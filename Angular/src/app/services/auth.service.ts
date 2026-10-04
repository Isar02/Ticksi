import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, firstValueFrom, from, map, shareReplay, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiError } from '../core/models/api-error';
import { AuthResponse, LoginRequest, RegisterRequest, UserInfo } from '../models/auth.models';

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

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly authUrl = `${environment.apiUrl}/auth`;
  private readonly session = signal<StoredSession | null>(readStoredSession());
  private refreshInFlight: RefreshInFlight | null = null;

  readonly currentUser = computed(() => this.session()?.user ?? null);

  constructor() {
    // Another tab may rotate, replace or end the session; this tab must not keep using the old one.
    window.addEventListener('storage', event => {
      if (event.key === SESSION_KEY || event.key === null) {
        this.session.set(readStoredSession());
      }
    });
  }

  login(credentials: LoginRequest): Observable<void> {
    return this.http.post<AuthResponse>(`${this.authUrl}/login`, credentials)
      .pipe(map(response => this.startSession(response, crypto.randomUUID())));
  }

  register(data: RegisterRequest): Observable<void> {
    return this.http.post<AuthResponse>(`${this.authUrl}/register`, data)
      .pipe(map(response => this.startSession(response, crypto.randomUUID())));
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

  // Runs under a lock shared by all tabs, so only one of them sends a given refresh token.
  private async refreshOnce(original: StoredSession): Promise<string> {
    const stored = readStoredSession();
    if (!stored || stored.id !== original.id) {
      throw sessionChanged();
    }

    if (stored.refreshToken !== original.refreshToken) {
      this.session.set(stored);
      return stored.accessToken;
    }

    let response: AuthResponse;
    try {
      response = await firstValueFrom(
        this.http.post<AuthResponse>(`${this.authUrl}/refresh`, { refreshToken: original.refreshToken })
      );
    } catch (error) {
      if (error instanceof ApiError && error.status === 401 && isStored(original)) {
        this.clearSession();
        this.router.navigate(['/login']);
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

  private revokeOnServer(refreshToken: string): void {
    this.http.post(`${this.authUrl}/logout`, { refreshToken }).subscribe({ error: () => {} });
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
}

function sessionChanged(): Error {
  return new Error('The session changed before it could be refreshed.');
}

function isStored(session: StoredSession): boolean {
  const stored = readStoredSession();
  return stored?.id === session.id && stored.refreshToken === session.refreshToken;
}

function readStoredSession(): StoredSession | null {
  const session = parseSession(localStorage.getItem(SESSION_KEY));
  if (session?.id && Date.parse(session.refreshTokenExpiresAtUtc) > Date.now()) {
    return session;
  }

  localStorage.removeItem(SESSION_KEY);
  return null;
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
