import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService, OWN_ACCESS_TOKEN } from '../../services/auth.service';
import { ApiError } from '../models/api-error';

const authUrl = `${environment.apiUrl}/auth/`;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(environment.apiUrl) || request.url.startsWith(authUrl) || request.context.get(OWN_ACCESS_TOKEN)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const sessionId = auth.sessionId();

  return next(withAccessToken(request, auth.getToken())).pipe(
    catchError((error: unknown) => {
      if (!sessionId || !(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      // A refresh that could not be completed (no connection, server error) is the request's real failure.
      return auth.refreshSession(sessionId).pipe(
        catchError((refreshError: unknown) =>
          throwError(() => (refreshError instanceof ApiError && refreshError.status !== 401 ? refreshError : error))
        ),
        switchMap(accessToken => next(withAccessToken(request, accessToken)))
      );
    })
  );
};

function withAccessToken(request: HttpRequest<unknown>, accessToken: string | null): HttpRequest<unknown> {
  return accessToken ? request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }) : request;
}
