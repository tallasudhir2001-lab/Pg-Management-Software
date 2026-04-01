import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuditEvent, AuditCount } from '../models/audit-event.model';

export interface AuditListResponse {
  items: AuditEvent[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly baseUrl = `${environment.apiBaseUrl}/audit`;

  constructor(private http: HttpClient) {}

  getUnreviewedCount(): Observable<AuditCount> {
    return this.http.get<AuditCount>(`${this.baseUrl}/unreviewed-count`);
  }

  getAuditEvents(params: {
    page?: number;
    pageSize?: number;
    eventType?: string;
    entityType?: string;
    status?: string;
    sortDir?: string;
  }): Observable<AuditListResponse> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.eventType) httpParams = httpParams.set('eventType', params.eventType);
    if (params.entityType) httpParams = httpParams.set('entityType', params.entityType);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<AuditListResponse>(this.baseUrl, { params: httpParams });
  }

  markAsReviewed(id: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/review`, {});
  }

  markAllAsReviewed(): Observable<any> {
    return this.http.post(`${this.baseUrl}/review-all`, {});
  }
}
