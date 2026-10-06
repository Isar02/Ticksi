import { Directive, input } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router, RouterLink, convertToParamMap } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { AuthService } from '../../../services/auth.service';
import { EMAIL_CHECK_DELAY_MS } from '../auth-forms';
import { RegisterComponent } from './register.component';

@Directive({ selector: '[routerLink]', standalone: true })
class RouterLinkStub {
  readonly routerLink = input<unknown>();
}

describe('RegisterComponent', () => {
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let fixture: ComponentFixture<RegisterComponent>;
  let page: HTMLElement;

  function type(name: string, value: string, blur = true): void {
    const field = page.querySelector<HTMLInputElement>(`input[formcontrolname="${name}"]`)!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    if (blur) field.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
  }

  function fill(email = 'lana@ticksi.com'): void {
    type('firstName', ' Lana ');
    type('lastName', 'Kovac');
    type('email', email);
    type('phone', '+387 61 222 333');
    type('password', 'Secret1!');
    type('confirmPassword', 'Secret1!');
  }

  function submit(): void {
    page.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    fixture.detectChanges();
  }

  function texts(selector: string): string[] {
    return [...page.querySelectorAll(selector)].map(element => element.textContent!.trim());
  }

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['isEmailAvailable', 'register']);
    auth.isEmailAvailable.and.returnValue(of(true));
    auth.register.and.returnValue(of(undefined));
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ returnUrl: '/events' }) } } }
      ]
    }).overrideComponent(RegisterComponent, { remove: { imports: [RouterLink] }, add: { imports: [RouterLinkStub] } });

    fixture = TestBed.createComponent(RegisterComponent);
    fixture.detectChanges();
    page = fixture.nativeElement as HTMLElement;
  });

  it('shows a taken email on the email field while typing', fakeAsync(() => {
    auth.isEmailAvailable.and.returnValue(of(false));

    type('email', 'admin@ticksi.com', false);
    expect(texts('mat-hint')).toContain('Checking whether this email is free…');

    tick(EMAIL_CHECK_DELAY_MS);
    fixture.detectChanges();

    expect(auth.isEmailAvailable).toHaveBeenCalledOnceWith('admin@ticksi.com');
    expect(texts('mat-error')).toEqual(['An account with this email already exists.']);
  }));

  for (const error of [new Error('offline'), new ApiError(500, 'server_error', 'Unavailable.')]) {
    it(`does not claim the email is free when its check fails: ${error.message}`, fakeAsync(() => {
      auth.isEmailAvailable.and.returnValue(throwError(() => error));

      type('email', 'admin@ticksi.com', false);
      tick(EMAIL_CHECK_DELAY_MS);
      fixture.detectChanges();

      expect(page.querySelector('.email-free')).toBeNull();
      expect(texts('mat-hint')).toContain('Email availability could not be checked. You can still try creating an account.');
      expect(texts('mat-hint')).not.toContain('This email is free.');
    }));
  }

  it('shows a green check only for the current email after a successful answer', fakeAsync(() => {
    const answer = new Subject<boolean>();
    auth.isEmailAvailable.and.returnValue(answer);
    type('email', 'lana@ticksi.com', false);
    tick(EMAIL_CHECK_DELAY_MS);
    answer.next(true);
    answer.complete();
    fixture.detectChanges();
    expect(page.querySelector('.email-free')).not.toBeNull();

    auth.isEmailAvailable.and.returnValue(throwError(() => new Error('offline')));
    type('email', 'other@ticksi.com', false);
    expect(page.querySelector('.email-free')).toBeNull();
    tick(EMAIL_CHECK_DELAY_MS);
    fixture.detectChanges();
    expect(page.querySelector('.email-free')).toBeNull();
    expect(texts('mat-hint')).not.toContain('This email is free.');
  }));

  it('still sends a valid account if the pending email lookup fails', fakeAsync(() => {
    const answer = new Subject<boolean>();
    auth.isEmailAvailable.and.returnValue(answer);
    fill();
    tick(EMAIL_CHECK_DELAY_MS);
    submit();
    expect(auth.register).not.toHaveBeenCalled();

    answer.error(new Error('offline'));

    expect(auth.register).toHaveBeenCalledTimes(1);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/events');
  }));

  it('waits for the email check before sending, then returns to the page the visitor came from', fakeAsync(() => {
    const answer = new Subject<boolean>();
    auth.isEmailAvailable.and.returnValue(answer);
    fill();
    tick(EMAIL_CHECK_DELAY_MS);

    submit();
    expect(auth.register).not.toHaveBeenCalled();

    answer.next(true);
    answer.complete();

    expect(auth.register).toHaveBeenCalledOnceWith({
      firstName: 'Lana',
      lastName: 'Kovac',
      email: 'lana@ticksi.com',
      phone: '+387 61 222 333',
      password: 'Secret1!'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/events');
  }));

  it('does not send when the pending check finds the email taken', fakeAsync(() => {
    const answer = new Subject<boolean>();
    auth.isEmailAvailable.and.returnValue(answer);
    fill('admin@ticksi.com');
    tick(EMAIL_CHECK_DELAY_MS);

    submit();
    answer.next(false);
    answer.complete();
    fixture.detectChanges();

    expect(auth.register).not.toHaveBeenCalled();
    expect(texts('mat-error')).toEqual(['An account with this email already exists.']);
    expect(page.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBeFalse();
  }));

  it('shows the API field errors on their fields and the rest above the form', fakeAsync(() => {
    auth.register.and.returnValue(
      throwError(() => new ApiError(400, 'validation_failed', 'x', { email: ['An account with this email already exists.'], role: ['No role.'] }))
    );
    fill();
    tick(EMAIL_CHECK_DELAY_MS);

    submit();
    fixture.detectChanges();

    expect(texts('mat-error')).toEqual(['An account with this email already exists.']);
    expect(texts('.alert span')).toEqual(['No role.']);
    expect(page.querySelector<HTMLInputElement>('input[formcontrolname="email"]')!.disabled).toBeFalse();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    tick(EMAIL_CHECK_DELAY_MS);
    fixture.detectChanges();
    expect(texts('mat-error')).toEqual(['An account with this email already exists.']);
    expect(page.querySelector('.email-free')).toBeNull();

    type('email', 'other@ticksi.com');
    tick(EMAIL_CHECK_DELAY_MS);
    fixture.detectChanges();
    expect(texts('mat-error')).toEqual([]);
    expect(page.querySelector('.email-free')).not.toBeNull();
  }));

  it('shows every browser error on submit and sends nothing', () => {
    submit();

    expect(texts('mat-error')).toEqual([
      'First name is required.',
      'Last name is required.',
      'Email is required.',
      'Phone is required.',
      'Password is required.',
      'Password confirmation is required.'
    ]);
    expect(auth.isEmailAvailable).not.toHaveBeenCalled();
    expect(auth.register).not.toHaveBeenCalled();
  });

  it('rechecks the confirmation when the password changes', () => {
    type('password', 'Secret1!');
    type('confirmPassword', 'Secret1!');
    expect(texts('mat-error')).toEqual([]);

    type('password', 'Secret2!');

    expect(texts('mat-error')).toEqual(['Passwords do not match.']);
  });
});
