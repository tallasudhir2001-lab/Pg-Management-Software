import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { PaymentMode } from '../models/payment-mode.model';
import { PaymentContext } from '../models/payment-context.model';
import { CreatePaymentRequest } from '../models/create-payment.model';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { TenantPaymentHistory } from '../models/tenant-payment-history.model';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private readonly baseUrl = `${environment.apiBaseUrl}/payments`;
  private readonly paymentModesUrl=`${environment.apiBaseUrl}/payment-modes`;

  private paymentModes$!: Observable<PaymentMode[]>;
  constructor(private http: HttpClient) { }

  getPaymentContext(tenantId: string): Observable<PaymentContext> {
    return this.http.get<PaymentContext>(
      `${this.baseUrl}/context/${tenantId}`
    );
  }
  createPayment(payload: CreatePaymentRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/create-payment`, payload);
  }
  getPaymentModes(): Observable<PaymentMode[]> {
    if (!this.paymentModes$) {
      this.paymentModes$ = this.http
        .get<PaymentMode[]>(`${this.paymentModesUrl}`)
        .pipe(
          shareReplay(1) // cache last value
        );
    }
    return this.paymentModes$;
  }
  getTenantPaymentHistory(tenantId: string) {
    return this.http.get<TenantPaymentHistory[]>(
      `${this.baseUrl}/tenant/${tenantId}`
    );
  }
}
