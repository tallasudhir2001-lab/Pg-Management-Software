import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { TenantListDto } from '../models/tenant-list-dto';
import { PagedResults } from '../../../shared/models/page-results.model';
import { Observable } from 'rxjs';
//import { TenantListQuery } from '../models/tenant-list-query';

@Injectable({
  providedIn: 'root',
})
export class Tenantservice {
    private readonly baseUrl = `${environment.apiBaseUrl}/tenants`;
    
    constructor(private http: HttpClient) {}

    createTenant(payload: any) {
      return this.http.post(`${this.baseUrl}/create-tenant`, payload);
    }

    getTenants(params: {
  page: number;
  pageSize: number;
  search?: string;
  status?: string;
  roomId?: string;
  sortBy?: string;
  sortDir?: string;
}) {

    //using below if conditions to make sure we are not sending undefined as parameter which will give 0 results
    let httpParams = new HttpParams()
    .set('page', params.page)
    .set('pageSize', params.pageSize);

  if (params.search) {
    httpParams = httpParams.set('search', params.search);
  }

  if (params.status) {
    httpParams = httpParams.set('status', params.status);
  }

  if (params.roomId) {
    httpParams = httpParams.set('roomId', params.roomId);
  }

  if (params.sortBy) {
    httpParams = httpParams.set('sortBy', params.sortBy);
  }

  if (params.sortDir) {
    httpParams = httpParams.set('sortDir', params.sortDir);
  }

  return this.http.get<PagedResults<TenantListDto>>(
    this.baseUrl,
    { params: httpParams as any }
  );
}
deleteTenant(tenantId: string): Observable<void> {
  return this.http.delete<void>(`${this.baseUrl}/${tenantId}`);
}
}
