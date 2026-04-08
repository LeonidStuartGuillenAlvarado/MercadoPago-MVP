import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreatePaymentResponse {
  externalReference: string;
  qrCode:            string;
  status:            string;
}

export interface PaymentStatusResponse {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {

  private readonly baseUrl = `${environment.apiUrl}/api/payments`;

  constructor(private http: HttpClient) {}

  createPayment(amount: number, mode: 'web' | 'store'): Observable<CreatePaymentResponse> {
    return this.http.post<CreatePaymentResponse>(this.baseUrl, { amount, mode });
  }

  getStatus(externalReference: string): Observable<PaymentStatusResponse> {
    return this.http.get<PaymentStatusResponse>(`${this.baseUrl}/${externalReference}/status`);
  }

  cancelPayment(externalReference: string): Observable<{ status: string }> {
    return this.http.post<{ status: string }>(
      `${this.baseUrl}/${externalReference}/cancel`, {}
    );
  }
}
