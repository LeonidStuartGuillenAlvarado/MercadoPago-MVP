import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

import { PaymentService } from '../../core/services/payment.service';
import { PaymentStore } from './payment.store';
import { SignalRService } from '../../core/services/signalr.service';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment.component.html',
})
export class PaymentComponent implements OnInit, OnDestroy {
  private paymentService = inject(PaymentService);
  private signalR = inject(SignalRService);
  store = inject(PaymentStore);

  ngOnInit() {
    this.signalR.startConnection((data) => {
      const currentRef = this.store.externalReference();

      if (data.externalReference === currentRef) {
        this.store.setStatus(data.status);
      }
    });
  }

  updateAmount(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.store.amount.set(Number(value));
  }

  createPayment() {
    if (this.store.amount() <= 0) return;

    this.store.setLoading(true);

    this.paymentService.createPayment(this.store.amount()).subscribe({
      next: (res) => {
        this.store.setPayment(res);
        this.store.setLoading(false);
      },
      error: () => {
        this.store.setLoading(false);
      },
    });
  }

  cancelPayment() {
    const ref = this.store.externalReference();
    if (!ref) return;

    this.paymentService.cancelPayment(ref).subscribe();
  }

  ngOnDestroy() {
    this.signalR.stopConnection();
  }
}
