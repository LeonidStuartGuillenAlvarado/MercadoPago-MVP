import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class PaymentStore {

  amount = signal<number>(0);
  status = signal<string | null>(null);
  qrCode = signal<string | null>(null);
  externalReference = signal<string | null>(null);
  loading = signal<boolean>(false);

  setPayment(data: any) {
    this.externalReference.set(data.externalReference);
    this.qrCode.set(data.qrCode);
    this.status.set(data.status);
  }

  setStatus(status: string) {
    this.status.set(status);
  }

  setLoading(value: boolean) {
    this.loading.set(value);
  }

  clear() {
    this.status.set(null);
    this.qrCode.set(null);
    this.externalReference.set(null);
  }
}
