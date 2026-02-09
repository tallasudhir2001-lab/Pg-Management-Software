import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DashboardSummary } from '../models/dashboard-summary.model';
import { RevenueTrend } from '../models/revenue-trend.model';
import { RecentPayment } from '../models/recent-payment.model';
import { Occupancy } from '../models/occupancy.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {

  private readonly baseUrl = `${environment.apiBaseUrl}/dashboard`;

  constructor(private http: HttpClient) {}

  // -----------------------------
  // SUMMARY
  // -----------------------------
  getSummary(range?:DateRange): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(
      `${this.baseUrl}/summary`,
      {
      params: range
        ? {
            from: range.from.toISOString(),
            to: range.to.toISOString()
          }
        : {}
    }
    );
  }

  // -----------------------------
  // REVENUE TREND
  // -----------------------------
  getRevenueTrend(range?:DateRange): Observable<RevenueTrend[]> {
    return this.http.get<RevenueTrend[]>(
      `${this.baseUrl}/revenue-trend`,
      {
      params: range
        ? {
            from: range.from.toISOString(),
            to: range.to.toISOString()
          }
        : {}
    }
    );
  }

  // -----------------------------
  // RECENT PAYMENTS
  // -----------------------------
  getRecentPayments(limit: number = 5, range?: DateRange) {
  const params: any = { limit };

  if (range) {
    params.from = range.from.toISOString();
    params.to = range.to.toISOString();
  }

  return this.http.get<RecentPayment[]>(
    `${this.baseUrl}/recent-payments`,
    { params }
  );
}


  // -----------------------------
  // OCCUPANCY
  // -----------------------------
  getOccupancy(): Observable<Occupancy> {
    return this.http.get<Occupancy>(
      `${this.baseUrl}/occupancy`
    );
  }
  getExpensesSummary(range?: DateRange) {
  return this.http.get<{ totalExpenses: number }>(
    `${this.baseUrl}/expenses-summary`,
    {
      params: range
        ? {
            from: range.from.toISOString(),
            to: range.to.toISOString()
          }
        : {}
    }
  );
}

}
export interface DateRange {
  from: Date;
  to: Date;
}

