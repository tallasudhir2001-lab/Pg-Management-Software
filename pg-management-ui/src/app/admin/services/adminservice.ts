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
}
