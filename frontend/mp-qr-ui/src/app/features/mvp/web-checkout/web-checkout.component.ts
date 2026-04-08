import { Component, inject, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { QRCodeComponent } from 'angularx-qrcode';
import { PaymentService }  from '../../../core/services/payment.service';
import { SignalRService }  from '../../../core/services/signalr.service';
import {
  PaymentStatus,
  normalizeStatus,
  isFinalStatus,
  statusDetailLabel
} from '../../../core/utils/payment-status.utils';

@Component({
  standalone: true,
  imports: [CommonModule, QRCodeComponent],
  templateUrl: './web-checkout.component.html'
})
export class WebCheckoutComponent implements OnInit, OnDestroy {

  private paymentService = inject(PaymentService);
  private signalR        = inject(SignalRService);
  private router         = inject(Router);

  // ── Estado del componente ────────────────────────────────────────────────
  total             = signal(0);
  qrCode            = signal<string | null>(null);
  externalReference = signal<string | null>(null);
  status            = signal<PaymentStatus>('idle');
  statusDetail      = signal<string | null>(null);
  loading           = signal(false);

  // Muestra QR solo si hay código y el pago no está aprobado aún
  showQr = computed(() => !!this.qrCode() && this.status() !== 'approved');

  // Evita procesar el mismo evento SignalR dos veces
  private lastProcessedStatus: string | null = null;
  private lastProcessedDetail: string | null = null;

  // Timeout de expiración: si a los 10 minutos no hay respuesta, informar al usuario
  private expirationTimer?: ReturnType<typeof setTimeout>;

  // Polling de respaldo: consulta el estado cada N segundos por si el webhook/SignalR falla
  private pollingInterval?: ReturnType<typeof setInterval>;
  private readonly POLL_INTERVAL_MS = 4000; // cada 4 segundos

  // ── Helpers expuestos al template ────────────────────────────────────────
  readonly statusDetailLabel = statusDetailLabel;

  // ── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit() {
    const cart        = JSON.parse(localStorage.getItem('cart') || '[]');
    const totalAmount = cart.reduce((a: number, b: any) => a + b.qty * b.price, 0);
    this.total.set(totalAmount);

    // SignalR: se inicia la conexión solo para escuchar actualizaciones
    // La suscripción real al externalReference se filtra dentro del handler
    this.signalR.startConnection((data) => {
      if (data.externalReference !== this.externalReference()) return;

      // Idempotencia en el frontend
      if (
        this.lastProcessedStatus === data.status &&
        this.lastProcessedDetail === data.statusDetail
      ) return;

      this.lastProcessedStatus = data.status;
      this.lastProcessedDetail = data.statusDetail ?? null;

      this.status.set(normalizeStatus(data.status));
      this.statusDetail.set(data.statusDetail ?? null);

      if (data.status === 'approved') {
        this.clearExpiration();
        setTimeout(() => this.router.navigate(['/thank-you']), 1500);
      }
    });
  }

  ngOnDestroy() {
    this.clearExpiration();
    this.stopPolling();
    this.signalR.stopConnection();
  }

  // ── Generar pago ─────────────────────────────────────────────────────────
  pay() {
    if (this.total() <= 0 || this.loading()) return;

    this.loading.set(true);
    this.status.set('pending');
    this.statusDetail.set(null);

    this.paymentService.createPayment(this.total(), 'web').subscribe({
      next: (res) => {
        this.externalReference.set(res.externalReference ?? null);
        this.qrCode.set(res.qrCode ?? null);
        this.status.set(normalizeStatus(res.status ?? 'pending'));
        this.loading.set(false);

        // El QR expira en 10 min en el backend → notificar al usuario si no pagó
        this.scheduleExpiration(10 * 60 * 1000);

        // Polling de respaldo: por si el webhook/SignalR no llega
        this.startPolling(res.externalReference);
      },
      error: () => {
        this.loading.set(false);
        this.status.set('idle');
      }
    });
  }

  // ── Polling de respaldo ───────────────────────────────────────────────────
  private startPolling(ref: string): void {
    this.stopPolling();
    this.pollingInterval = setInterval(() => {
      if (isFinalStatus(this.status())) {
        this.stopPolling();
        return;
      }
      this.paymentService.getStatus(ref).subscribe({
        next: (res) => {
          const normalized = normalizeStatus(res.status);
          if (normalized !== this.status()) {
            this.status.set(normalized);
            if (normalized === 'approved') {
              this.clearExpiration();
              this.stopPolling();
              setTimeout(() => this.router.navigate(['/thank-you']), 1500);
            } else if (isFinalStatus(normalized)) {
              this.stopPolling();
            }
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

  // ── Cancelar pago ────────────────────────────────────────────────────────
  cancel() {
    const ref = this.externalReference();
    if (!ref) return;

    this.paymentService.cancelPayment(ref).subscribe({
      next: () => {
        this.status.set('cancelled');
        this.qrCode.set(null);
        this.clearExpiration();
        this.stopPolling();
      }
    });
  }

  // ── Volver a intentar ────────────────────────────────────────────────────
  retry() {
    this.qrCode.set(null);
    this.externalReference.set(null);
    this.status.set('idle');
    this.statusDetail.set(null);
    this.lastProcessedStatus = null;
    this.lastProcessedDetail = null;
    this.clearExpiration();
    this.stopPolling();
  }

  // ── Expiración ────────────────────────────────────────────────────────────
  private scheduleExpiration(ms: number) {
    this.clearExpiration();
    this.expirationTimer = setTimeout(() => {
      if (this.status() === 'pending') {
        this.status.set('cancelled');
        this.qrCode.set(null);
        this.statusDetail.set('QR expirado. Generá uno nuevo para continuar.');
      }
    }, ms);
  }

  private clearExpiration() {
    if (this.expirationTimer) {
      clearTimeout(this.expirationTimer);
      this.expirationTimer = undefined;
    }
  }
}
