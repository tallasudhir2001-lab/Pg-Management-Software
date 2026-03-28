import { HttpInterceptorFn, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { Auth } from '../services/auth';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);
  const router = inject(Router);

  return next(attachToken(req, auth.getToken())).pipe(
    catchError(err => {
      // Only attempt refresh on 401, and not for auth endpoints themselves
      if (err.status !== 401 || isAuthUrl(req.url)) {
        return throwError(() => err);
      }

      return auth.refreshAccessToken().pipe(
        switchMap(res => next(attachToken(req, res.token))),
        catchError(refreshErr => {
          auth.logout();
          router.navigate(['/login']);
          return throwError(() => refreshErr);
        })
      );
    })
  );
};

function attachToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) return req;
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isAuthUrl(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/refresh') || url.includes('/auth/logout');
}
