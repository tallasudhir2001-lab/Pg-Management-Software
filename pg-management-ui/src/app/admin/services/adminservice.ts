import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class Adminservice {
  private apiUrl = `${environment.apiBaseUrl}/admin`;

  constructor(private http: HttpClient) {}

  registerPg(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register-pg`, data);
  }

  getPgs(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/pgs`);
  }

  getBranches(): Observable<BranchDto[]> {
    return this.http.get<BranchDto[]>(`${this.apiUrl}/branches`);
  }

  updatePgSubscription(pgId: string, data: { isEmailSubscriptionEnabled: boolean; isWhatsappSubscriptionEnabled: boolean }): Observable<any> {
    return this.http.put(`${this.apiUrl}/pgs/${pgId}/subscription`, data);
  }

  updatePgDetails(pgId: string, data: { name: string; address: string; contactNumber: string; branchName?: string; ownerEmail?: string }): Observable<any> {
    return this.http.put(`${this.apiUrl}/pgs/${pgId}/details`, data);
  }
}

export interface BranchDto {
  id: string;
  name: string;
  pgCount: number;
  pGs: { pgId: string; name: string }[];
}
