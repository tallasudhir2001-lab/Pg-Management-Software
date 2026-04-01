import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DashboardSummary } from '../models/dashboard-summary.model';
import { DashboardAlerts } from '../models/dashboard-alerts.model';
import { CollectionSummary } from '../models/collection-summary.model';
import { RecentPayment } from '../models/recent-payment.model';
import { AuditCount } from '../../audit/models/audit-event.model';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DashboardService {

  private readonly baseUrl = `${environment.apiBaseUrl}/dashboard`;

  constructor(private http: HttpClient) {}

  getSummary(range?: DateRange): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/summary`, {
      params: range ? { from: range.from.toISOString(), to: range.to.toISOString() } : {}
    });
  }

  getAlerts(): Observable<DashboardAlerts> {
    return this.http.get<DashboardAlerts>(`${this.baseUrl}/alerts`);
  }

  getCollectionSummary(range?: DateRange): Observable<CollectionSummary> {
    return this.http.get<CollectionSummary>(`${this.baseUrl}/collection-summary`, {
      params: range ? { from: range.from.toISOString(), to: range.to.toISOString() } : {}
    });
  }

  getRecentPayments(limit: number = 8, range?: DateRange): Observable<RecentPayment[]> {
    const params: any = { limit };
    if (range) {
      params.from = range.from.toISOString();
      params.to = range.to.toISOString();
    }
    return this.http.get<RecentPayment[]>(`${this.baseUrl}/recent-payments`, { params });
  }

  getExpensesSummary(range?: DateRange): Observable<{ totalExpenses: number }> {
    return this.http.get<{ totalExpenses: number }>(`${this.baseUrl}/expenses-summary`, {
      params: range ? { from: range.from.toISOString(), to: range.to.toISOString() } : {}
    });
  }

  getUnreviewedAuditCount(): Observable<AuditCount> {
    return this.http.get<AuditCount>(`${environment.apiBaseUrl}/audit/unreviewed-count`);
  }

}

export interface DateRange {
  from: Date;
  to: Date;
}
