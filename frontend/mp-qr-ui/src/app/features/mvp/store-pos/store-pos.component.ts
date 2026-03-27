import { QRCodeComponent } from 'angularx-qrcode';
import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { PaymentService } from '../../../core/services/payment.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  imports: [CommonModule, QRCodeComponent],
  templateUrl: './store-pos.component.html',
})
export class StorePosComponent implements OnInit, OnDestroy {
  private paymentService = inject(PaymentService);
  private signalR = inject(SignalRService);
  private router = inject(Router);


  qrUrl = 'https://postpalpebral-karrie-overhostilely.ngrok-free.dev/api/store/scan';
  qrEnabled = signal(false);
  message = '';

  products = signal([
    { name: 'Producto 1', price: 1000, qty: 0 },
    { name: 'Producto 2', price: 2000, qty: 0 },
    { name: 'Producto 3', price: 3000, qty: 0 },
    { name: 'Producto 4', price: 4000, qty: 0 },
    { name: 'Producto 5', price: 5000, qty: 0 },
  ]);

  ref: string | null = null;
  loading = false;

  ngOnInit() {
    // 🔵 Conexión única a SignalR
    this.signalR.startConnection((data) => {
      if (!this.ref) return;

      if (data.externalReference === this.ref && data.status === 'approved') {
        this.resetProducts();

        this.router.navigate(['/thank-you']);
      }
    });
  }

  ngOnDestroy() {
    this.signalR.stopConnection();
  }

  total() {
    return this.products().reduce((a, b) => a + b.qty * b.price, 0);
  }

  increase(p: any) {
    p.qty++;
    this.products.update((v) => [...v]);
  }

  decrease(p: any) {
    if (p.qty > 0) p.qty--;
    this.products.update((v) => [...v]);
  }

  cobrar() {
    if (this.total() <= 0 || this.loading) return;

    this.loading = true;

    this.paymentService.createPayment(this.total(), 'store').subscribe({
      next: (res) => {
        this.ref = res.externalReference;

        this.qrEnabled.set(true);
        this.message = 'QR habilitado. El cliente puede escanear ahora.';

        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  private resetProducts() {
    this.products.update((products) => products.map((p) => ({ ...p, qty: 0 })));
    this.ref = null;
  }
}
