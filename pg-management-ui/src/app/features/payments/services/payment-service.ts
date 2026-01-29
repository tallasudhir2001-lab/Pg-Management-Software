import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { PaymentMode } from '../models/payment-mode.model';
import { PaymentContext } from '../models/payment-context.model';
import { CreatePaymentRequest } from '../models/create-payment.model';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { TenantPaymentHistory } from '../models/tenant-payment-history.model';
import { PagedResults } from '../../../shared/models/page-results.model';
import { PaymentHistoryDto } from '../models/paymets-history-dto';

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
  getPaymentHistory(options: {
    page: number;
    pageSize: number;
    search?: string;
    mode?: string;
    tenantId?: string;
    userId?:string;
    sortBy?: string;
    sortDir?: 'asc' | 'desc';
  }): Observable<PagedResults<PaymentHistoryDto>> {
    let params = new HttpParams()
      .set('page', options.page.toString())
      .set('pageSize', options.pageSize.toString());

    if (options.search) {
      params = params.set('search', options.search);
    }

    if (options.mode) {
      params = params.set('mode', options.mode);
    }

    if (options.tenantId) {
      params = params.set('tenantId', options.tenantId);
    }

    if (options.sortBy) {
      params = params.set('sortBy', options.sortBy);
    }

    if (options.sortDir) {
      params = params.set('sortDir', options.sortDir);
    }

    return this.http.get<PagedResults<PaymentHistoryDto>>(
      `${this.baseUrl}/history`,
      { params }
    );
  }
  deletePayment(paymentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${paymentId}`);
  }
}
