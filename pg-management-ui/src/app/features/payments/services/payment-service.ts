import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { PaymentMode } from '../models/payment-mode.model';
import { PaymentType } from '../models/Payment type.model';
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
  private readonly paymentModesUrl = `${environment.apiBaseUrl}/payment-modes`;
  private readonly paymentTypesUrl = `${environment.apiBaseUrl}/payment-types`;

  private paymentModes$!: Observable<PaymentMode[]>;
  // ✅ NEW: cached observable — fetched at most once per app session
  private paymentTypes$!: Observable<PaymentType[]>;

  constructor(private http: HttpClient) {}

  getPaymentContext(tenantId: string): Observable<PaymentContext> {
    return this.http.get<PaymentContext>(`${this.baseUrl}/context/${tenantId}`);
  }

  createPayment(payload: CreatePaymentRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/create-payment`, payload);
  }

  getPaymentModes(): Observable<PaymentMode[]> {
    if (!this.paymentModes$) {
      this.paymentModes$ = this.http
        .get<PaymentMode[]>(`${this.paymentModesUrl}`)
        .pipe(shareReplay(1));
    }
    return this.paymentModes$;
  }

  // ✅ NEW: same lazy-cache pattern as getPaymentModes()
  getPaymentTypes(): Observable<PaymentType[]> {
    if (!this.paymentTypes$) {
      this.paymentTypes$ = this.http
        .get<PaymentType[]>(`${this.paymentTypesUrl}`)
        .pipe(shareReplay(1));
    }
    return this.paymentTypes$;
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
    userId?: string;
    types?: string[];
    sortBy?: string;
    sortDir?: 'asc' | 'desc';
    fromDate?: string;
    toDate?: string;
  }): Observable<PagedResults<PaymentHistoryDto>> {
    let params = new HttpParams()
      .set('page', options.page.toString())
      .set('pageSize', options.pageSize.toString());

    if (options.search)   params = params.set('search',   options.search);
    if (options.mode)     params = params.set('mode',     options.mode);
    if (options.tenantId) params = params.set('tenantId', options.tenantId);
    if (options.userId)   params = params.set('userId',   options.userId);
    if (options.sortBy)   params = params.set('sortBy',   options.sortBy);
    if (options.sortDir)  params = params.set('sortDir',  options.sortDir);
    if (options.fromDate) params = params.set('fromDate',  options.fromDate);
    if (options.toDate)   params = params.set('toDate',    options.toDate);

    if (options.types && options.types.length > 0) {
      params = params.set('types', options.types.join(','));
    }

    return this.http.get<PagedResults<PaymentHistoryDto>>(
      `${this.baseUrl}/history`,
      { params }
    );
  }

  deletePayment(paymentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${paymentId}`);
  }

  getPayment(paymentId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${paymentId}`);
  }

  updatePayment(paymentId: string, payload: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/${paymentId}`, payload);
  }

  sendReceipt(paymentId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${paymentId}/send-receipt`, {});
  }

  sendReceiptWhatsApp(paymentId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${paymentId}/send-receipt-whatsapp`, {});
  }

  calculateRent(tenantId: string, paidFrom: string, paidUpto: string): Observable<{ amount: number; stayType: string }> {
    const params = new HttpParams()
      .set('paidFrom', paidFrom)
      .set('paidUpto', paidUpto);
    return this.http.get<{ amount: number; stayType: string }>(
      `${this.baseUrl}/calculate-rent/${tenantId}`,
      { params }
    );
  }
}