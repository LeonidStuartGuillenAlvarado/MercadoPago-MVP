import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/payments`;

  createPayment(amount: number) {
    return this.http.post<any>(this.baseUrl, { amount });
  }

  // getStatus(externalReference: string) {
  //   return this.http.get<any>(`${this.baseUrl}/${externalReference}/status`);
  // }
  getStatus(externalReference: string) {
    return this.http.get<{ status: string }>(`${this.baseUrl}/${externalReference}/status`);
  }

  // cancelPayment(externalReference: string) {
  //   return this.http.post<any>(`${this.baseUrl}/${externalReference}/cancel`, {});
  // }
  cancelPayment(externalReference: string) {
    return this.http.post<{ status: string }>(`${this.baseUrl}/${externalReference}/cancel`, {});
  }
}
