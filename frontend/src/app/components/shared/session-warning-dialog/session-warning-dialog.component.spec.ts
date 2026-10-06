import { signal } from '@angular/core';
import { ComponentFixture, TestBed, discardPeriodicTasks, fakeAsync, tick } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { AuthService } from '../../../services/auth.service';
import { SessionWarningDialogComponent } from './session-warning-dialog.component';

describe('SessionWarningDialogComponent', () => {
  let now: number;
  let fixture: ComponentFixture<SessionWarningDialogComponent>;
  let extension: Subject<void>;
  let auth: { sessionExpiresAt: ReturnType<typeof signal<number | null>>; extendSession: jasmine.Spy; logout: jasmine.Spy };
  let close: jasmine.Spy;
  let navigate: jasmine.Spy;

  beforeEach(() => {
    now = Date.parse('2026-10-06T10:28:00Z');
    spyOn(Date, 'now').and.callFake(() => now);
    auth = {
      sessionExpiresAt: signal<number | null>(now + 120_000),
      extendSession: jasmine.createSpy('extendSession').and.callFake(() => (extension = new Subject<void>())),
      logout: jasmine.createSpy('logout')
    };
    close = jasmine.createSpy('close');
    navigate = jasmine.createSpy('navigate');

    TestBed.configureTestingModule({
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { warningMs: 120_000 } },
        { provide: MatDialogRef, useValue: { close } },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: { navigate } }
      ]
    });
  });

  function open(): void {
    fixture = TestBed.createComponent(SessionWarningDialogComponent);
    fixture.detectChanges();
  }

  function advance(ms: number): void {
    now += ms;
    tick(ms);
    fixture.detectChanges();
  }

  function text(selector: string): string {
    return (fixture.nativeElement as HTMLElement).querySelector(selector)?.textContent?.trim() ?? '';
  }

  function ringOffset(): number {
    return parseFloat((fixture.nativeElement as HTMLElement).querySelector<SVGElement>('.session__progress')!.style.strokeDashoffset);
  }

  function button(kind: 'cancel' | 'confirm'): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector(`.ticket__button--${kind}`)!;
  }

  it('counts down every second while the ring empties and turns urgent in the last 30 seconds', fakeAsync(() => {
    open();
    expect(text('.session__time')).toBe('2:00');
    expect(ringOffset()).toBeCloseTo(0);
    expect(text('[aria-live]')).toBe('You will be signed out in less than two minutes.');

    advance(1000);
    expect(text('.session__time')).toBe('1:59');

    advance(59_000);
    expect(text('[aria-live]')).toBe('You will be signed out in less than a minute.');

    advance(30_000);
    expect(text('.session__time')).toBe('0:30');
    expect(ringOffset()).toBeCloseTo(2 * Math.PI * 32 * 0.75);
    expect(fixture.nativeElement.querySelector('.session__timer--urgent')).not.toBeNull();
    expect(text('[aria-live]')).toBe('You will be signed out in less than 30 seconds.');

    advance(40_000);
    expect(text('.session__time')).toBe('0:00');
    discardPeriodicTasks();
  }));

  it('follows a deadline moved by another tab', fakeAsync(() => {
    open();
    auth.sessionExpiresAt.set(now + 30 * 60_000);
    advance(1000);

    expect(text('.session__time')).toBe('29:59');
    discardPeriodicTasks();
  }));

  it('extends the session and closes once it is extended', fakeAsync(() => {
    open();
    button('confirm').click();
    fixture.detectChanges();

    expect(auth.extendSession).toHaveBeenCalledTimes(1);
    expect(button('confirm').disabled).toBeTrue();
    expect(button('cancel').disabled).toBeTrue();
    expect(close).not.toHaveBeenCalled();

    extension.next();
    expect(close).toHaveBeenCalledTimes(1);
    discardPeriodicTasks();
  }));

  it('keeps the dialog open with a message when the session cannot be extended', fakeAsync(() => {
    open();
    button('confirm').click();
    extension.error(new ApiError(0, 'network_error', 'Cannot reach the server.'));
    fixture.detectChanges();

    expect(text('.session__error')).toBe('Cannot reach the server.');
    expect(button('confirm').disabled).toBeFalse();
    expect(close).not.toHaveBeenCalled();
    discardPeriodicTasks();
  }));

  it('signs out and goes home', fakeAsync(() => {
    open();
    button('cancel').click();

    expect(auth.logout).toHaveBeenCalledTimes(1);
    expect(close).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith(['/']);
    discardPeriodicTasks();
  }));
});
