import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../environments/environment';
import { authInterceptor } from '../core/interceptors/auth.interceptor';
import { errorInterceptor } from '../core/interceptors/error.interceptor';
import { ApiError } from '../core/models/api-error';
import { ToastService } from '../core/services/toast.service';
import { AuthResponse } from '../models/auth.models';
import { AuthService } from './auth.service';

describe('AuthService session expiry', () => {
  const sessionKey = 'ticksi_session';
  const start = Date.parse('2026-10-06T10:00:00Z');
  const original = {
    id: 'original-session',
    accessToken: 'original-access',
    refreshToken: 'original-refresh',
    refreshTokenExpiresAtUtc: new Date(start + 30 * 60_000).toISOString(),
    user: { email: 'user@ticksi.com', publicId: 'original-user', firstName: 'Lejla', role: 'User' }
  };
  let previousSession: string | null;
  let now: number;
  let auth: AuthService;
  let http: HttpTestingController;
  let client: HttpClient;
  let router: Router;
  let navigate: jasmine.Spy<Router['navigateByUrl']>;
  let toast: jasmine.SpyObj<ToastService>;

  beforeEach(() => {
    previousSession = localStorage.getItem(sessionKey);
    localStorage.setItem(sessionKey, JSON.stringify(original));
    now = start;
    spyOn(Date, 'now').and.callFake(() => now);
    spyOn(navigator.locks, 'request').and.callFake((
      _name: string,
      optionsOrCallback: LockOptions | LockGrantedCallback,
      callback?: LockGrantedCallback
    ) => Promise.resolve().then(() =>
      (typeof optionsOrCallback === 'function' ? optionsOrCallback : callback!)(null)));
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['info', 'error']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastService, useValue: toast }
      ]
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    router = TestBed.inject(Router);
    spyOnProperty(router, 'url', 'get').and.returnValue('/tickets?view=upcoming');
    navigate = spyOn(router, 'navigateByUrl').and.returnValue(Promise.resolve(true));
  });

  afterEach(() => {
    http.verify();
    if (previousSession === null) localStorage.removeItem(sessionKey);
    else localStorage.setItem(sessionKey, previousSession);
  });

  function expectSignedOut(): void {
    expect(auth.isAuthenticated()).toBeFalse();
    expect(auth.currentUser()).toBeNull();
    expect(auth.getToken()).toBeNull();
    expect(localStorage.getItem(sessionKey)).toBeNull();
    expect(toast.info).toHaveBeenCalledOnceWith('Your session has expired. Please sign in again.');
    expect(navigate).toHaveBeenCalledTimes(1);
    if (navigate.calls.count()) {
      const target = navigate.calls.mostRecent().args[0];
      expect(typeof target === 'string' ? target : router.serializeUrl(target))
        .toBe('/auth/login?returnUrl=%2Ftickets%3Fview%3Dupcoming');
    }
  }

  function advance(minutes: number): void {
    now += minutes * 60_000;
    tick(minutes * 60_000);
  }

  function refreshedSession(): AuthResponse {
    return {
      accessToken: `header.${btoa(JSON.stringify({ role: 'User' }))}.signature`,
      accessTokenExpiresAtUtc: new Date(now + 15 * 60_000).toISOString(),
      refreshToken: 'new-refresh',
      refreshTokenExpiresAtUtc: new Date(now + 30 * 60_000).toISOString(),
      email: 'new@ticksi.com',
      publicId: 'new-user',
      firstName: 'Amra'
    };
  }

  for (const minutes of [30, 31]) {
    it(`signs out an open tab on its first protected request after ${minutes} minutes`, fakeAsync(() => {
      advance(minutes);
      let failure: unknown;
      client.get(`${environment.apiUrl}/tickets/mine`).subscribe({ error: error => failure = error });
      http.expectOne(`${environment.apiUrl}/tickets/mine`)
        .flush({ code: 'unauthorized', message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
      flushMicrotasks();

      expectSignedOut();
      expect(failure).toEqual(jasmine.any(ApiError));
      http.expectNone(`${environment.apiUrl}/auth/refresh`);
    }));
  }

  it('shows the expiry message once when concurrent requests fail', fakeAsync(() => {
    advance(31);
    for (const path of ['tickets/mine', 'favorites']) {
      client.get(`${environment.apiUrl}/${path}`).subscribe({ error: () => {} });
      http.expectOne(`${environment.apiUrl}/${path}`)
        .flush({}, { status: 401, statusText: 'Unauthorized' });
    }
    flushMicrotasks();

    expectSignedOut();
    http.expectNone(`${environment.apiUrl}/auth/refresh`);
  }));

  it('uses the session refreshed by another tab before the lock is acquired', fakeAsync(() => {
    advance(31);
    let accessToken: string | undefined;
    auth.refreshSession(original.id).subscribe(token => accessToken = token);
    localStorage.setItem(sessionKey, JSON.stringify({
      ...original,
      accessToken: 'other-tab-access',
      refreshToken: 'other-tab-refresh',
      refreshTokenExpiresAtUtc: new Date(now + 30 * 60_000).toISOString()
    }));
    flushMicrotasks();

    expect(accessToken).toBe('other-tab-access');
    expect(auth.getToken()).toBe('other-tab-access');
    expect(auth.isAuthenticated()).toBeTrue();
    expect(toast.info).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
    http.expectNone(`${environment.apiUrl}/auth/refresh`);
  }));

  it('signs out when a refresh 401 arrives after the local expiry', fakeAsync(() => {
    advance(29);
    auth.refreshSession(original.id).subscribe({ error: () => {} });
    flushMicrotasks();
    const request = http.expectOne(`${environment.apiUrl}/auth/refresh`);
    advance(2);
    request.flush({ code: 'unauthorized', message: 'Session expired' }, { status: 401, statusText: 'Unauthorized' });
    flushMicrotasks();

    expectSignedOut();
  }));

  it('accepts a successful refresh started before the local expiry', fakeAsync(() => {
    advance(29);
    let accessToken: string | undefined;
    let failure: unknown;
    auth.refreshSession(original.id).subscribe({ next: token => accessToken = token, error: error => failure = error });
    flushMicrotasks();
    const request = http.expectOne(`${environment.apiUrl}/auth/refresh`);
    advance(2);
    const response = refreshedSession();
    request.flush(response);
    flushMicrotasks();

    expect(failure).toBeUndefined();
    expect(accessToken).toBe(response.accessToken);
    expect(auth.isAuthenticated()).toBeTrue();
    expect(auth.sessionId()).toBe(original.id);
    expect(JSON.parse(localStorage.getItem(sessionKey)!).refreshToken).toBe(response.refreshToken);
    expect(toast.info).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
    http.expectNone(`${environment.apiUrl}/auth/logout`);
  }));

  it('preserves a new sign-in when an older refresh is refused', fakeAsync(() => {
    advance(29);
    auth.refreshSession(original.id).subscribe({ error: () => {} });
    flushMicrotasks();
    const refresh = http.expectOne(`${environment.apiUrl}/auth/refresh`);
    auth.login({ email: 'new@ticksi.com', password: 'password' }).subscribe();
    const response = refreshedSession();
    http.expectOne(`${environment.apiUrl}/auth/login`).flush(response);
    const newSessionId = auth.sessionId();
    advance(2);
    refresh.flush({ code: 'unauthorized', message: 'Session expired' }, { status: 401, statusText: 'Unauthorized' });
    flushMicrotasks();

    expect(auth.isAuthenticated()).toBeTrue();
    expect(auth.sessionId()).toBe(newSessionId);
    expect(newSessionId).not.toBe(original.id);
    expect(auth.currentUser()?.publicId).toBe('new-user');
    expect(JSON.parse(localStorage.getItem(sessionKey)!).refreshToken).toBe(response.refreshToken);
    expect(toast.info).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  }));
});
