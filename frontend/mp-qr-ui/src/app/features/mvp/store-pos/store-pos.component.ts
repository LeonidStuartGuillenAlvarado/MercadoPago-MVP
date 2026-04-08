import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { QRCodeComponent } from 'angularx-qrcode';
import { PaymentService }  from '../../../core/services/payment.service';
import { SignalRService }  from '../../../core/services/signalr.service';
import { environment }     from '../../../environments/environment';

interface Product {
  name:  string;
  price: number;
  qty:   number;
}

@Component({
  standalone: true,
  imports: [CommonModule, QRCodeComponent],
  templateUrl: './store-pos.component.html'
})
export class StorePosComponent implements OnInit, OnDestroy {

  private paymentService = inject(PaymentService);
  private signalR        = inject(SignalRService);
  private router         = inject(Router);

  // URL del QR estático impreso en caja — apunta al endpoint /api/store/scan
  readonly qrUrl = `${environment.apiUrl}/api/store/scan`;

  products = signal<Product[]>([
    { name: 'Producto 1', price: 100, qty: 0 },
    { name: 'Producto 2', price: 200, qty: 0 },
    { name: 'Producto 3', price: 300, qty: 0 },
    { name: 'Producto 4', price: 400, qty: 0 },
    { name: 'Producto 5', price: 500, qty: 0 },
  ]);

  qrEnabled = signal(false);
  loading   = signal(false);
  ref: string | null = null;

  // Polling de respaldo por si el webhook/SignalR no llega
  private pollingInterval?: ReturnType<typeof setInterval>;
  private readonly POLL_INTERVAL_MS = 4000;

  total() {
    return this.products().reduce((a, b) => a + b.qty * b.price, 0);
  }

  increase(p: Product) { p.qty++; this.products.update(v => [...v]); }
  decrease(p: Product) { if (p.qty > 0) p.qty--; this.products.update(v => [...v]); }

  ngOnInit() {
    this.signalR.startConnection((data) => {
      if (!this.ref || data.externalReference !== this.ref) return;

      if (data.status === 'approved') {
        this.reset();
        this.router.navigate(['/thank-you']);
      }
    });
  }

  ngOnDestroy() {
    this.stopPolling();
    this.signalR.stopConnection();
  }

  cobrar() {
    if (this.total() <= 0 || this.loading()) return;

    this.loading.set(true);

    this.paymentService.createPayment(this.total(), 'store').subscribe({
      next: (res) => {
        this.ref = res.externalReference;
        this.qrEnabled.set(true);
        this.loading.set(false);
        // Polling de respaldo por si el webhook/SignalR no llega
        this.startPolling(res.externalReference);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  cancelarCobro() {
    if (!this.ref) return;
    this.paymentService.cancelPayment(this.ref).subscribe({
      next: () => this.reset()
    });
  }

  private startPolling(ref: string): void {
    this.stopPolling();
    this.pollingInterval = setInterval(() => {
      this.paymentService.getStatus(ref).subscribe({
        next: (res) => {
          if (res.status === 'approved') {
            this.reset();
            this.stopPolling();
            this.router.navigate(['/thank-you']);
          } else if (['rejected', 'cancelled'].includes(res.status)) {
            this.reset();
            this.stopPolling();
          }
        }
      });
    }, this.POLL_INTERVAL_MS);
  }

  private stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = undefined;
    }
  }

  private reset() {
    this.products.update(ps => ps.map(p => ({ ...p, qty: 0 })));
    this.ref = null;
    this.qrEnabled.set(false);
    this.stopPolling();
  }
}
