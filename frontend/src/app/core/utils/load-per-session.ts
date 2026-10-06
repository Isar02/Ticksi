import { computed, inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable, of, startWith, switchMap } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { loginUrl } from '../guards/return-url';
import { ApiError } from '../models/api-error';

// Loads again on every new sign-in and every reload, with null while loading; the session id stays the same when tokens rotate.
export function loadPerSession<T>(returnUrl: string, reload$: Observable<void>, load: () => Observable<T>): Observable<T | null> {
  const auth = inject(AuthService);
  const router = inject(Router);

  return toObservable(computed(() => auth.sessionId())).pipe(
    switchMap(sessionId => {
      if (sessionId === null) {
        // Signing out here already navigates away; only a sign-out from another tab is sent to the login page.
        if (!router.getCurrentNavigation()) void router.navigateByUrl(loginUrl(router, returnUrl));
        return of(null);
      }

      return reload$.pipe(
        startWith(undefined),
        switchMap(() => load().pipe(startWith(null)))
      );
    })
  );
}

export function failureMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
