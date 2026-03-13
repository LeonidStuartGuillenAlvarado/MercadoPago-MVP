import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { PaymentService } from '../../../core/services/payment.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { QRCodeComponent } from 'angularx-qrcode';


type PaymentStatus = 'idle' | 'pending' | 'approved' | 'rejected' | 'cancelled';
@Component({
  standalone: true,
  imports: [CommonModule, QRCodeComponent],
  templateUrl: './web-checkout.component.html',
})
export class WebCheckoutComponent implements OnInit {
  private paymentService = inject(PaymentService);
  private signalR = inject(SignalRService);
  private router = inject(Router);
  
  //se agrega un logger para mostrar el estado de la transacción en tiempo real
  private pendingLogger: any;



  // 🔵 Signals
  total = signal(0);
  qrCode = signal<string | null>(null);
  externalReference = signal<string | null>(null);
  status = signal<PaymentStatus>('idle');
  statusDetail = signal<string | null>(null);
  loading = signal(false);

  // 🔵 Computed helpers
  showQr = computed(() => !!this.qrCode() && this.status() !== 'approved');

  private normalizeStatus(value: string): PaymentStatus {
    const allowed: PaymentStatus[] = ['idle', 'pending', 'approved', 'rejected', 'cancelled'];

    return allowed.includes(value as PaymentStatus) ? (value as PaymentStatus) : 'idle';
  }

  //detalles de status
  getStatusDetailText(detail: string | null) {
    switch(detail) {
      case 'cc_rejected_insufficient_amount':
        return 'Fondos insuficientes';
      case 'cc_rejected_bad_filled_security_code':
        return 'Código de seguridad incorrecto';
      case 'cc_rejected_bad_filled_date':
        return 'Fecha de vencimiento incorrecta';
      case 'cc_rejected_call_for_authorize':
        return 'Debe autorizar el pago con el banco';
      case 'cc_rejected_other_reason':
        return 'Error general de la tarjeta';
      default:
        return detail;
    }
  }

  private contador = 0;
  startPendingLogger() {

    this.pendingLogger = setInterval(() => {

      if (this.status() === 'pending') {
        this.contador++;
        console.log("status:", this.status()," ", this.contador);
      }

    }, 1000); // cada 1 segundo
  }

  ngOnInit() {
    
    const cart = JSON.parse(localStorage.getItem('cart') || '[]');
    const totalAmount = cart.reduce((a: number, b: any) => a + b.qty * b.price, 0);
    this.total.set(totalAmount);
    

    this.signalR.startConnection((data) => {
      if (data.externalReference === this.externalReference()) {
        this.status.set(this.normalizeStatus(data.status));
        this.statusDetail.set(data.statusDetail ?? null);
      
        if (this.pendingLogger) {
          clearInterval(this.pendingLogger);
        }
        
        if (data.status === 'approved') {
          setTimeout(() => {
            this.router.navigate(['/thank-you']);
          }, 1500);
        }
      }
    });
  }

  pay() {
    if (this.total() <= 0) return;

    this.loading.set(true);
    this.status.set('pending');

    this.paymentService.createPayment(this.total(), 'web').subscribe({
      next: (res) => {
        this.externalReference.set(res.externalReference ?? null);
        this.qrCode.set(res.qrCode ?? null);
        this.status.set(this.normalizeStatus(res.status ?? 'pending'));

        this.startPendingLogger();

        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.status.set('idle');
      },
    });
  }
}
