import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Auth } from './auth';
import { PermissionService } from './permission.service';

@Injectable({ providedIn: 'root' })
export class BranchViewService {
  private readonly storageKey = 'branchViewActive';
  private readonly apiUrl = `${environment.apiBaseUrl}/branch`;

  private _isActive$ = new BehaviorSubject<boolean>(this.loadState());
  private _canToggle$ = new BehaviorSubject<boolean>(false);
  private _userPgs$ = new BehaviorSubject<{ pgId: string; name: string }[]>([]);

  isActive$ = this._isActive$.asObservable();
  canToggle$ = this._canToggle$.asObservable();
  userPgs$ = this._userPgs$.asObservable();

  constructor(
    private http: HttpClient,
    private auth: Auth,
    private permissionService: PermissionService
  ) {}

  get isActive(): boolean {
    return this._isActive$.value;
  }

  /** Returns the current PG id from the JWT */
  get currentPgId(): string | null {
    return this.auth.getPgId();
  }

  /** Returns true if the given PG id matches the user's current login PG */
  isCurrentPg(pgId: string): boolean {
    return pgId === this.currentPgId;
  }

  /**
   * Check if the user can toggle branch view.
   * Called once on layout init. Requires 2+ PG assignments.
   */
  checkCanToggle(): void {
    const branchId = this.auth.getBranchId();
    const hasPermission = this.permissionService.hasAccess('Branch.ToggleBranchView');

    if (!branchId || !hasPermission) {
      this._canToggle$.next(false);
      // If toggle isn't available, ensure branch view is off
      if (this._isActive$.value) {
        this._isActive$.next(false);
        localStorage.removeItem(this.storageKey);
      }
      return;
    }

    // Load user's PGs — only show toggle if user has 2+ PGs
    this.http.get<{ pgId: string; name: string }[]>(`${this.apiUrl}/user-pgs`).pipe(
      tap(pgs => {
        this._userPgs$.next(pgs);
        const canToggle = pgs.length >= 2;
        this._canToggle$.next(canToggle);
        // If user only has 1 PG, force toggle off
        if (!canToggle && this._isActive$.value) {
          this._isActive$.next(false);
          localStorage.removeItem(this.storageKey);
        }
      }),
      catchError(() => {
        this._canToggle$.next(false);
        return of([]);
      })
    ).subscribe();
  }

  toggle(): void {
    const newState = !this._isActive$.value;
    this._isActive$.next(newState);
    localStorage.setItem(this.storageKey, JSON.stringify(newState));
  }

  private loadState(): boolean {
    try {
      return JSON.parse(localStorage.getItem(this.storageKey) ?? 'false');
    } catch {
      return false;
    }
  }

  /**
   * Reset state on logout
   */
  reset(): void {
    this._isActive$.next(false);
    this._canToggle$.next(false);
    this._userPgs$.next([]);
    localStorage.removeItem(this.storageKey);
  }
}
