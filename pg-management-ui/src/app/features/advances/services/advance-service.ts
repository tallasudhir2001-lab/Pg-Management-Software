import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Advance } from '../models/advance.model';
import { SettleAdvanceDto } from '../models/settle-advance.dto';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdvanceService {

  private readonly baseUrl = `${environment.apiBaseUrl}/advances`;
  
  constructor(private http: HttpClient) {}

  //  Get advances for tenant (if needed separately later)
  getAdvances(tenantId: string): Observable<Advance[]> {
    return this.http.get<Advance[]>(`${this.baseUrl}/tenant/${tenantId}`);
  }

  //  Settle advance
  settleAdvance(advanceId: string, dto: SettleAdvanceDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/${advanceId}/settle`, dto);
  }

  createAdvance(dto: {
    tenantId: string;
    amount: number;
    paymentModeCode: string;
  }): Observable<any> {
    return this.http.post(`${this.baseUrl}`, dto);
  }
}