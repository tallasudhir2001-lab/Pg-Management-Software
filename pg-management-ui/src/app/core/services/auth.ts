import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError, shareReplay } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PermissionService } from './permission.service';

@Injectable({ providedIn: 'root' })
export class Auth {
  private apiUrl = `${environment.apiBaseUrl}/auth`;
  private refreshObservable: Observable<any> | null = null;

  constructor(private http: HttpClient, private permissionService: PermissionService) {}

  login(data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, data);
  }

  selectPg(pgId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/select-pg`, { pgId });
  }

  saveToken(token: string, refreshToken?: string): void {
    localStorage.setItem('token', token);
    if (refreshToken) localStorage.setItem('refreshToken', refreshToken);
    this.permissionService.loadFromToken(token);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  refreshAccessToken(): Observable<{ token: string; refreshToken: string }> {
    // Deduplicate: if a refresh is already in flight, reuse its observable
    if (this.refreshObservable) return this.refreshObservable;

    const refreshToken = this.getRefreshToken();
    if (!refreshToken) return throwError(() => new Error('No refresh token available'));

    this.refreshObservable = this.http
      .post<{ token: string; refreshToken: string }>(`${this.apiUrl}/refresh`, { refreshToken })
      .pipe(
        tap(res => {
          this.saveToken(res.token, res.refreshToken);
          this.refreshObservable = null;
        }),
        catchError(err => {
          this.refreshObservable = null;
          return throwError(() => err);
        }),
        shareReplay(1)
      );

    return this.refreshObservable;
  }

  private decodeTokenPayload(): Record<string, any> | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return null;
    }
  }

  getRole(): string | null {
    return this.decodeTokenPayload()?.['role'] ?? null;
  }

  getBranchId(): string | null {
    return this.decodeTokenPayload()?.['branchId'] ?? null;
  }

  getPgId(): string | null {
    return this.decodeTokenPayload()?.['pgId'] ?? null;
  }

  isOwner(): boolean {
    return this.getRole() === 'Owner';
  }

  isAdmin(): boolean {
    return this.decodeTokenPayload()?.['auth_level'] === 'admin';
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      // Fire-and-forget — revoke on server but don't block UI
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe();
    }
    this.permissionService.clear();
    localStorage.clear();
  }
}
