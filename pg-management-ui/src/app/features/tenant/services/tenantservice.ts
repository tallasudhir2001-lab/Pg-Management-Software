import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { TenantListDto } from '../models/tenant-list-dto';
import { PagedResults } from '../../../shared/models/page-results.model';
import { TenantListQuery } from '../models/tenant-list-query';

@Injectable({
  providedIn: 'root',
})
export class Tenantservice {
    private readonly baseUrl = `${environment.apiBaseUrl}/tenants`;
    
    constructor(private http: HttpClient) {}

    createTenant(payload: any) {
      return this.http.post(`${this.baseUrl}/create-tenant`, payload);
    }

    getTenants(query: TenantListQuery) {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search)
      params = params.set('search', query.search);

    if (query.status)
      params = params.set('status', query.status);

    if (query.roomId)
      params = params.set('roomId', query.roomId);

    if (query.sortBy)
      params = params.set('sortBy', query.sortBy);

    if (query.sortDir)
      params = params.set('sortDir', query.sortDir);

    return this.http.get<PagedResults<TenantListDto>>(
      this.baseUrl,
      { params }
    );
  }
}
