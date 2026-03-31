import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ReportRecipient {
  userId: string;
  name: string;
  email: string;
  phoneNumber: string;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly base = `${environment.apiBaseUrl}/reports`;
  private readonly paymentsBase = `${environment.apiBaseUrl}/payments`;

  constructor(private http: HttpClient) {}

  generateReport(endpoint: string, params: HttpParams): Observable<Blob> {
    return this.http.get(`${this.base}/${endpoint}`, {
      params,
      responseType: 'blob'
    });
  }

  getReportData<T>(endpoint: string, params: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.base}/${endpoint}`, { params });
  }

  getReceipt(paymentId: string): Observable<Blob> {
    return this.http.get(`${this.paymentsBase}/${paymentId}/receipt`, {
      responseType: 'blob'
    });
  }

  sendReceipt(paymentId: string, recipientEmail: string): Observable<any> {
    return this.http.post(`${this.paymentsBase}/${paymentId}/send-receipt`, { recipientEmail });
  }

  sendReport(reportType: string, recipientEmail: string, filters: Record<string, string> = {}): Observable<any> {
    return this.http.post(`${this.base}/send`, { reportType, recipientEmail, filters });
  }

  sendReportWhatsApp(reportType: string, phoneNumber: string, filters: Record<string, string> = {}): Observable<any> {
    return this.http.post(`${this.base}/send-whatsapp`, { reportType, phoneNumber, filters });
  }

  getAvailableRecipients(): Observable<ReportRecipient[]> {
    return this.http.get<ReportRecipient[]>(`${this.base}/available-recipients`);
  }
}
