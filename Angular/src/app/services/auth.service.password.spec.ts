import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed, fakeAsync, flushMicrotasks } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../environments/environment';
import { authInterceptor } from '../core/interceptors/auth.interceptor';
import { errorInterceptor } from '../core/interceptors/error.interceptor';
import { ApiError } from '../core/models/api-error';
import { ToastService } from '../core/services/toast.service';
import { AuthResponse } from '../models/auth.models';
import { AuthService } from './auth.service';

describe('AuthService profile changes', () => {
  const sessionKey = 'ticksi_session';
  const passwordUrl = `${environment.apiUrl}/profile/password`;
  const refreshUrl = `${environment.apiUrl}/auth/refresh`;
  const change = { currentPassword: 'Secret123', newPassword: 'Changed456' };
  const original = {
    id: 'original-session',
    accessToken: 'original-access',
    refreshToken: 'original-refresh',
    refreshTokenExpiresAtUtc: new Date(Date.now() + 30 * 60_000).toISOString(),
    user: { email: 'user@ticksi.com', publicId: 'user-id', firstName: 'Lejla', role: 'User' }
  };
  let previousSession: string | null;
  let auth: AuthService;
  let http: HttpTestingController;
  let locks: jasmine.Spy;

  beforeEach(() => {
    previousSession = localStorage.getItem(sessionKey);
    localStorage.setItem(sessionKey, JSON.stringify(original));
    let lockQueue = Promise.resolve();
    locks = spyOn(navigator.locks, 'request').and.callFake((
      _name: string,
      optionsOrCallback: LockOptions | LockGrantedCallback,
      callback?: LockGrantedCallback
    ) => {
      const result = lockQueue.then(() =>
        (typeof optionsOrCallback === 'function' ? optionsOrCallback : callback!)(null));
      lockQueue = result.then(() => undefined, () => undefined);
      return result;
    });
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['info', 'error']) }
      ]
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    spyOn(TestBed.inject(Router), 'navigateByUrl').and.returnValue(Promise.resolve(true));
  });

  afterEach(() => {
    http.verify();
    if (previousSession === null) localStorage.removeItem(sessionKey);
    else localStorage.setItem(sessionKey, previousSession);
  });

  function session(refreshToken: string): AuthResponse {
    return {
      accessToken: `header.${btoa(JSON.stringify({ role: 'User' }))}.${refreshToken}`,
      accessTokenExpiresAtUtc: new Date(Date.now() + 15 * 60_000).toISOString(),
      refreshToken,
      refreshTokenExpiresAtUtc: new Date(Date.now() + 30 * 60_000).toISOString(),
      email: 'user@ticksi.com',
      publicId: 'user-id',
      firstName: 'Lejla'
    };
  }

  function stored() {
    return JSON.parse(localStorage.getItem(sessionKey)!);
  }

  it('changes the password under the refresh lock and keeps this session with the new tokens', fakeAsync(() => {
    let done = false;
    auth.changePassword(change).subscribe(() => (done = true));
    flushMicrotasks();

    const request = http.expectOne(passwordUrl);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(change);
    expect(request.request.headers.get('Authorization')).toBe('Bearer original-access');
    expect(locks).toHaveBeenCalledWith('ticksi_session_refresh', jasmine.any(Function));
    request.flush(session('changed-refresh'));
    flushMicrotasks();

    expect(done).toBeTrue();
    expect(auth.sessionId()).toBe(original.id);
    expect(stored().refreshToken).toBe('changed-refresh');
    expect(auth.getToken()).toContain('changed-refresh');
  }));

  it('refreshes an expired access token inside the lock and sends the change again', fakeAsync(() => {
    let done = false;
    auth.changePassword(change).subscribe(() => (done = true));
    flushMicrotasks();

    http.expectOne(passwordUrl).flush({ code: 'unauthorized', message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    flushMicrotasks();
    const refresh = http.expectOne(refreshUrl);
    expect(refresh.request.body).toEqual({ refreshToken: 'original-refresh' });
    refresh.flush(session('rotated-refresh'));
    flushMicrotasks();

    const retry = http.expectOne(passwordUrl);
    expect(retry.request.headers.get('Authorization')).toBe(`Bearer ${session('rotated-refresh').accessToken}`);
    retry.flush(session('changed-refresh'));
    flushMicrotasks();

    expect(done).toBeTrue();
    expect(stored().refreshToken).toBe('changed-refresh');
  }));

  it('leaves the session alone when the current password is wrong', fakeAsync(() => {
    let failure: unknown;
    auth.changePassword(change).subscribe({ error: error => (failure = error) });
    flushMicrotasks();

    http.expectOne(passwordUrl).flush(
      { code: 'validation_failed', message: 'The current password is incorrect.', errors: { currentPassword: ['The current password is incorrect.'] } },
      { status: 400, statusText: 'Bad Request' }
    );
    flushMicrotasks();

    expect((failure as ApiError).fieldErrors).toEqual({ currentPassword: ['The current password is incorrect.'] });
    expect(stored()).toEqual(original);
    http.expectNone(refreshUrl);
  }));

  it('revokes the new session when this tab signed out while the change was on its way', fakeAsync(() => {
    let failure: unknown;
    auth.changePassword(change).subscribe({ error: error => (failure = error) });
    flushMicrotasks();
    const request = http.expectOne(passwordUrl);

    auth.logout();
    http.expectOne(`${environment.apiUrl}/auth/logout`).flush(null);
    request.flush(session('changed-refresh'));
    flushMicrotasks();

    expect(failure).toEqual(jasmine.any(Error));
    expect(localStorage.getItem(sessionKey)).toBeNull();
    expect(http.expectOne(`${environment.apiUrl}/auth/logout`).request.body).toEqual({ refreshToken: 'changed-refresh' });
  }));

  for (const [situation, otherTab] of [
    ['signed out', null],
    ['signed in to another account', JSON.stringify({ ...original, id: 'other-sign-in', refreshToken: 'other-refresh' })]
  ] as const) {
    it(`keeps what another tab stored when it ${situation} before this tab heard of it`, fakeAsync(() => {
      let failure: unknown;
      auth.changePassword(change).subscribe({ error: error => (failure = error) });
      flushMicrotasks();
      const request = http.expectOne(passwordUrl);

      if (otherTab === null) localStorage.removeItem(sessionKey);
      else localStorage.setItem(sessionKey, otherTab);
      request.flush(session('changed-refresh'));
      flushMicrotasks();

      expect(failure).toEqual(jasmine.any(Error));
      expect(localStorage.getItem(sessionKey)).toBe(otherTab);
      expect(http.expectOne(`${environment.apiUrl}/auth/logout`).request.body).toEqual({ refreshToken: 'changed-refresh' });
    }));
  }

  it('renames the signed-in user in this tab and in storage', fakeAsync(() => {
    auth.renameUser('Amra');
    flushMicrotasks();

    expect(auth.currentUser()!.firstName).toBe('Amra');
    expect(stored().user.firstName).toBe('Amra');
    expect(stored().refreshToken).toBe(original.refreshToken);
  }));

  it('does not give the new name to an account signed in while the rename waited for the lock', fakeAsync(() => {
    let release!: () => void;
    navigator.locks.request('ticksi_session_refresh', () => new Promise<void>(resolve => (release = resolve)));
    flushMicrotasks();
    const other = { ...original, id: 'other-sign-in', user: { ...original.user, publicId: 'other-user', firstName: 'Emir' } };

    auth.renameUser('Amra');
    localStorage.setItem(sessionKey, JSON.stringify(other));
    window.dispatchEvent(new StorageEvent('storage', { key: sessionKey }));
    release();
    flushMicrotasks();

    expect(auth.currentUser()!.firstName).toBe('Emir');
    expect(stored().user.firstName).toBe('Emir');
  }));

  it('does not rename a session that another tab replaced', fakeAsync(() => {
    localStorage.setItem(sessionKey, JSON.stringify({ ...original, id: 'other-sign-in' }));

    auth.renameUser('Amra');
    flushMicrotasks();

    expect(stored().user.firstName).toBe('Lejla');
    expect(auth.currentUser()!.firstName).toBe('Lejla');
  }));
});
