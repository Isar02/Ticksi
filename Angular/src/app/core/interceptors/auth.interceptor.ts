import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

const authUrl = `${environment.apiUrl}/auth/`;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(environment.apiUrl) || request.url.startsWith(authUrl)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const sessionId = auth.sessionId();

  return next(withAccessToken(request, auth.getToken())).pipe(
    catchError((error: unknown) => {
      if (!sessionId || !(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refreshSession(sessionId).pipe(
        catchError(() => throwError(() => error)),
        switchMap(accessToken => next(withAccessToken(request, accessToken)))
      );
    })
  );
};

function withAccessToken(request: HttpRequest<unknown>, accessToken: string | null): HttpRequest<unknown> {
  return accessToken ? request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }) : request;
}
