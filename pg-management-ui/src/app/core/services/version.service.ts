import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AppVersion {
  version: string;
  buildDate: string;
}

@Injectable({ providedIn: 'root' })
export class VersionService {
  private readonly url = `${environment.apiBaseUrl}/version`;
  private version$?: Observable<AppVersion>;

  constructor(private http: HttpClient) {}

  getVersion(): Observable<AppVersion> {
    if (!this.version$) {
      this.version$ = this.http.get<AppVersion>(this.url).pipe(shareReplay(1));
    }
    return this.version$;
  }
}
