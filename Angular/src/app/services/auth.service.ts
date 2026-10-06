import { HttpClient, HttpContextToken } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, firstValueFrom, from, map, shareReplay, throwError, timeout } from 'rxjs';
import { environment } from '../../environments/environment';
import { loginUrl } from '../core/guards/return-url';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { ApiError } from '../core/models/api-error';
import { Role } from '../core/models/role';
import { ToastService } from '../core/services/toast.service';
import { AuthResponse, LoginRequest, RegisterRequest, UserInfo } from '../models/auth.models';
import { PasswordChange } from '../models/profile.model';

// The id is given at sign-in and kept through token rotation, so a new sign-in is never mistaken for a rotation.
interface StoredSession {
  id: string;
  accessToken: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserInfo;
}

interface RefreshInFlight {
  sessionId: string;
  accessToken$: Observable<string>;
}

const SESSION_KEY = 'ticksi_session';
const REFRESH_LOCK = 'ticksi_session_refresh';
const REFRESH_REQUEST_TIMEOUT_MS = 15_000;
const SESSION_EXPIRED_MESSAGE = 'Your session has expired. Please sign in again.';

// For a request sent with a token chosen here, while the refresh lock is held.
export const OWN_ACCESS_TOKEN = new HttpContextToken<boolean>(() => false);

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly authUrl = `${environment.apiUrl}/auth`;
  private readonly session = signal<StoredSession | null>(readStoredSession());
  private refreshInFlight: RefreshInFlight | null = null;

  readonly currentUser = computed(() => this.session()?.user ?? null);
  readonly sessionExpiresAt = computed(() => {
    const session = this.session();
    return session ? Date.parse(session.refreshTokenExpiresAtUtc) : null;
  });

  constructor() {
    // Another tab may rotate, replace or end the session; this tab must not keep using the old one.
    window.addEventListener('storage', event => {
      if (event.key === SESSION_KEY || event.key === null) {
        this.session.set(readStoredSession());
      }
    });
  }

  login(credentials: LoginRequest): Observable<void> {
    return this.http.post<AuthResponse>(`${this.authUrl}/login`, credentials, { context: withoutErrorToast() })
      .pipe(map(response => this.startSession(response, crypto.randomUUID())));
  }

  register(data: RegisterRequest): Observable<void> {
    return this.http.post<AuthResponse>(`${this.authUrl}/register`, data, { context: withoutErrorToast() })
      .pipe(map(response => this.startSession(response, crypto.randomUUID())));
  }

  isEmailAvailable(email: string): Observable<boolean> {
    return this.http
      .get<{ available: boolean }>(`${this.authUrl}/email-availability`, { params: { email }, context: withoutErrorToast() })
      .pipe(map(response => response.available));
  }

  logout(): void {
    const refreshToken = this.session()?.refreshToken;
    this.clearSession();

    if (refreshToken) {
      this.revokeOnServer(refreshToken);
    }
  }

  refreshSession(sessionId: string): Observable<string> {
    const current = this.session();
    if (current?.id !== sessionId) {
      return throwError(() => sessionChanged());
    }

    if (this.refreshInFlight?.sessionId !== sessionId) {
      const accessToken$: Observable<string> = from(
        navigator.locks.request(REFRESH_LOCK, () => this.refreshOnce(current))
      ).pipe(
        finalize(() => {
          if (this.refreshInFlight?.accessToken$ === accessToken$) {
            this.refreshInFlight = null;
          }
        }),
        shareReplay(1)
      );
      this.refreshInFlight = { sessionId, accessToken$ };
    }

    return this.refreshInFlight.accessToken$;
  }

  // The change revokes every refresh token, so no tab may send one until the new session is stored.
  changePassword(change: PasswordChange): Observable<void> {
    const sessionId = this.sessionId();
    return sessionId
      ? from(navigator.locks.request(REFRESH_LOCK, () => this.changePasswordOnce(sessionId, change)))
      : throwError(() => sessionChanged());
  }

  // Under the refresh lock, so a refresh in another tab cannot write the old name back.
  renameUser(firstName: string): void {
    const sessionId = this.sessionId();
    void navigator.locks.request(REFRESH_LOCK, () => {
      const stored = parseSession(localStorage.getItem(SESSION_KEY));
      if (!sessionId || stored?.id !== sessionId) {
        return;
      }

      const renamed: StoredSession = { ...stored, user: { ...stored.user, firstName } };
      localStorage.setItem(SESSION_KEY, JSON.stringify(renamed));
      this.session.set(renamed);
    });
  }

  extendSession(): Observable<void> {
    const sessionId = this.sessionId();
    return sessionId ? this.refreshSession(sessionId).pipe(map(() => undefined)) : throwError(() => sessionChanged());
  }

  // Wait for an in-flight refresh, including in another tab, before deciding whether the session expired.
  expireIfDue(): Promise<void> {
    return navigator.locks.request(REFRESH_LOCK, () => {
      if (!this.session()) {
        return;
      }

      const stored = parseSession(localStorage.getItem(SESSION_KEY));
      if (stored?.id && isLive(stored)) {
        this.session.set(stored);
        return;
      }

      this.expireSession();
    });
  }

  sessionId(): string | null {
    return this.session()?.id ?? null;
  }

  isAuthenticated(): boolean {
    return this.session() !== null;
  }

  getToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  getUserRole(): string | null {
    return this.currentUser()?.role ?? null;
  }

  hasAnyRole(roles: readonly Role[]): boolean {
    return roles.some(role => role === this.getUserRole());
  }

  // Runs under a lock shared by all tabs, so only one of them sends a given refresh token.
  private async refreshOnce(original: StoredSession): Promise<string> {
    const stored = parseSession(localStorage.getItem(SESSION_KEY));
    if (!stored || stored.id !== original.id) {
      throw sessionChanged();
    }

    if (!isLive(stored)) {
      this.expireSession();
      throw new ApiError(401, 'unauthorized', SESSION_EXPIRED_MESSAGE);
    }

    if (stored.refreshToken !== original.refreshToken) {
      this.session.set(stored);
      return stored.accessToken;
    }

    let response: AuthResponse;
    try {
      response = await firstValueFrom(
        this.http.post<AuthResponse>(
          `${this.authUrl}/refresh`,
          { refreshToken: original.refreshToken },
          { context: withoutErrorToast() }
        ).pipe(timeout(REFRESH_REQUEST_TIMEOUT_MS))
      );
    } catch (error) {
      if (error instanceof ApiError && error.status === 401 && isStored(original)) {
        this.expireSession();
      }
      throw error;
    }

    if (!isStored(original)) {
      this.revokeOnServer(response.refreshToken);
      throw sessionChanged();
    }

    this.startSession(response, original.id);
    return response.accessToken;
  }

  private async changePasswordOnce(sessionId: string, change: PasswordChange): Promise<void> {
    const stored = parseSession(localStorage.getItem(SESSION_KEY));
    if (!stored || stored.id !== sessionId) {
      throw sessionChanged();
    }

    let response: AuthResponse;
    try {
      response = await this.sendPasswordChange(stored.accessToken, change);
    } catch (error) {
      if (!(error instanceof ApiError && error.status === 401)) {
        throw error;
      }
      response = await this.sendPasswordChange(await this.refreshOnce(stored), change);
    }

    // Another tab may have signed out or in before this tab heard of it, so storage decides too.
    if (this.sessionId() !== sessionId || parseSession(localStorage.getItem(SESSION_KEY))?.id !== sessionId) {
      this.revokeOnServer(response.refreshToken);
      throw sessionChanged();
    }

    this.startSession(response, sessionId);
  }

  private sendPasswordChange(accessToken: string, change: PasswordChange): Promise<AuthResponse> {
    return firstValueFrom(
      this.http.put<AuthResponse>(`${environment.apiUrl}/profile/password`, change, {
        headers: { Authorization: `Bearer ${accessToken}` },
        context: withoutErrorToast().set(OWN_ACCESS_TOKEN, true)
      }).pipe(timeout(REFRESH_REQUEST_TIMEOUT_MS))
    );
  }

  private revokeOnServer(refreshToken: string): void {
    this.http.post(`${this.authUrl}/logout`, { refreshToken }, { context: withoutErrorToast() }).subscribe({ error: () => {} });
  }

  private startSession(response: AuthResponse, id: string): void {
    const session: StoredSession = {
      id,
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc,
      user: {
        email: response.email,
        publicId: response.publicId,
        firstName: response.firstName,
        role: readRole(response.accessToken)
      }
    };

    localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  private clearSession(): void {
    localStorage.removeItem(SESSION_KEY);
    this.session.set(null);
  }

  private expireSession(): void {
    this.clearSession();
    this.toast.info(SESSION_EXPIRED_MESSAGE);
    this.router.navigateByUrl(loginUrl(this.router, this.router.url));
  }
}

function sessionChanged(): Error {
  return new Error('The session changed before it could be refreshed.');
}

function isStored(session: StoredSession): boolean {
  const stored = parseSession(localStorage.getItem(SESSION_KEY));
  return stored?.id === session.id && stored.refreshToken === session.refreshToken;
}

function readStoredSession(): StoredSession | null {
  const session = parseSession(localStorage.getItem(SESSION_KEY));
  if (session?.id && isLive(session)) {
    return session;
  }

  localStorage.removeItem(SESSION_KEY);
  return null;
}

function isLive(session: StoredSession): boolean {
  return Date.parse(session.refreshTokenExpiresAtUtc) > Date.now();
}

function parseSession(json: string | null): StoredSession | null {
  try {
    return json ? (JSON.parse(json) as StoredSession) : null;
  } catch {
    return null;
  }
}

function readRole(accessToken: string): string | null {
  try {
    const payload = accessToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(payload)).role ?? null;
  } catch {
    return null;
  }
}
