import { signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { SESSION_WARNING_MS, SessionTimeoutService } from './session-timeout.service';

describe('SessionTimeoutService', () => {
  const minute = 60_000;
  let now: number;
  let expiresAt: ReturnType<typeof signal<number | null>>;
  let expireIfDue: jasmine.Spy;
  let opened: { close: jasmine.Spy; closed: Subject<void> }[];

  beforeEach(() => {
    now = Date.parse('2026-10-06T10:00:00Z');
    spyOn(Date, 'now').and.callFake(() => now);
    expiresAt = signal<number | null>(null);
    expireIfDue = jasmine.createSpy('expireIfDue');
    opened = [];

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { sessionExpiresAt: expiresAt, expireIfDue } },
        {
          provide: MatDialog,
          useValue: {
            open: () => {
              const closed = new Subject<void>();
              const dialog = { close: jasmine.createSpy('close').and.callFake(() => closed.next()), closed };
              opened.push(dialog);
              return { close: dialog.close, afterClosed: () => closed };
            }
          }
        }
      ]
    });
  });

  // Started inside each fakeAsync test, so its timers and listeners run on the fake clock.
  function start(): void {
    TestBed.inject(SessionTimeoutService).start();
  }

  function sessionEndsIn(minutes: number): void {
    expiresAt.set(now + minutes * minute);
    TestBed.flushEffects();
  }

  function advance(ms: number): void {
    now += ms;
    tick(ms);
  }

  it('warns two minutes before the deadline and ends the session when it is reached', fakeAsync(() => {
    start();
    sessionEndsIn(30);

    advance(28 * minute - 1);
    expect(opened.length).toBe(0);

    advance(1);
    expect(opened.length).toBe(1);
    expect(expireIfDue).not.toHaveBeenCalled();

    advance(SESSION_WARNING_MS);
    expect(expireIfDue).toHaveBeenCalledTimes(1);
  }));

  it('closes the warning when the session is extended and warns again before the new deadline', fakeAsync(() => {
    start();
    sessionEndsIn(30);
    advance(29 * minute);
    expect(opened.length).toBe(1);

    sessionEndsIn(30);
    expect(opened[0].close).toHaveBeenCalled();

    advance(SESSION_WARNING_MS);
    expect(expireIfDue).not.toHaveBeenCalled();
    expect(opened.length).toBe(1);

    advance(26 * minute);
    expect(opened.length).toBe(2);
  }));

  it('closes the warning and stops counting when the user signs out', fakeAsync(() => {
    start();
    sessionEndsIn(1);
    expect(opened.length).toBe(1);

    expiresAt.set(null);
    TestBed.flushEffects();
    advance(5 * minute);

    expect(opened[0].close).toHaveBeenCalled();
    expect(expireIfDue).not.toHaveBeenCalled();
  }));

  it('opens the warning only once while the deadline stays inside the last two minutes', fakeAsync(() => {
    start();
    sessionEndsIn(1.5);
    sessionEndsIn(1);

    expect(opened.length).toBe(1);
    advance(minute);
    expect(expireIfDue).toHaveBeenCalledTimes(1);
  }));

  it('checks the deadline again when a hidden tab becomes visible', fakeAsync(() => {
    start();
    sessionEndsIn(30);
    spyOn(Date, 'now').and.returnValue(now + 31 * minute);

    Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'visible' });
    try {
      document.dispatchEvent(new Event('visibilitychange'));
    } finally {
      delete (document as { visibilityState?: unknown }).visibilityState;
    }
    tick(0);

    expect(expireIfDue).toHaveBeenCalledTimes(1);
    expect(opened.length).toBe(0);
  }));
});
