import { fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import {
  EMAIL_CHECK_DELAY_MS,
  authErrorText,
  createLoginForm,
  createRegisterForm,
  serverFailure,
  toLoginRequest,
  toRegisterRequest
} from './auth-forms';

describe('auth forms', () => {
  const free = () => of(true);

  it('applies the registration API rules with its wording', () => {
    const form = createRegisterForm(free);
    form.setValue({
      firstName: ' A ',
      lastName: '   ',
      email: 'ana@',
      phone: '12-34',
      password: '      ',
      confirmPassword: 'other'
    });

    const messages = {
      firstName: authErrorText(form.controls.firstName, 'First name'),
      lastName: authErrorText(form.controls.lastName, 'Last name'),
      email: authErrorText(form.controls.email, 'Email'),
      phone: authErrorText(form.controls.phone, 'Phone'),
      password: authErrorText(form.controls.password, 'Password'),
      confirmPassword: authErrorText(form.controls.confirmPassword, 'Password confirmation')
    };

    expect(messages).toEqual({
      firstName: 'First name must be at least 2 characters.',
      lastName: 'Last name is required.',
      email: 'Invalid email format.',
      phone: 'Please enter a valid phone number.',
      password: 'Password is required.',
      confirmPassword: 'Passwords do not match.'
    });
  });

  it('asks for the password and an email in the API format on login, and sends the email trimmed', () => {
    const form = createLoginForm();
    form.setValue({ email: 'ana.ticksi.com', password: '  ' });

    expect(authErrorText(form.controls.email, 'Email')).toBe('Invalid email format.');
    expect(authErrorText(form.controls.password, 'Password')).toBe('Password is required.');

    form.setValue({ email: ' ana@ticksi.com ', password: ' Secret1 ' });
    expect(form.valid).toBeTrue();
    expect(toLoginRequest(form)).toEqual({ email: 'ana@ticksi.com', password: ' Secret1 ' });
  });

  it('accepts a complete account and sends it trimmed, without the confirmation', fakeAsync(() => {
    const form = createRegisterForm(free);
    form.setValue({
      firstName: ' Ana ',
      lastName: 'Kovac ',
      email: ' ana@ticksi.com ',
      phone: ' 061 123 456 ',
      password: 'Secret1!',
      confirmPassword: 'Secret1!'
    });
    tick(EMAIL_CHECK_DELAY_MS);

    expect(form.valid).toBeTrue();
    expect(toRegisterRequest(form)).toEqual({
      firstName: 'Ana',
      lastName: 'Kovac',
      email: 'ana@ticksi.com',
      phone: '061 123 456',
      password: 'Secret1!'
    });
  }));

  it('checks the email once typing pauses and marks a taken email', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of(false));
    const email = createRegisterForm(check).controls.email;

    email.setValue('an');
    email.setValue('ana@ticks');
    email.setValue(' ana@ticksi.com ');
    tick(EMAIL_CHECK_DELAY_MS - 1);
    expect(email.pending).toBeTrue();
    expect(check).not.toHaveBeenCalled();

    tick(1);
    expect(check).toHaveBeenCalledOnceWith('ana@ticksi.com');
    expect(authErrorText(email, 'Email')).toBe('An account with this email already exists.');
  }));

  it('does not call the API for an invalid email and lets a failed check pass to the server', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(throwError(() => new Error('offline')));
    const email = createRegisterForm(check).controls.email;

    email.setValue('ana@');
    tick(EMAIL_CHECK_DELAY_MS);
    expect(check).not.toHaveBeenCalled();

    email.setValue('ana@ticksi.com');
    tick(EMAIL_CHECK_DELAY_MS);
    expect(check).toHaveBeenCalledTimes(1);
    expect(email.valid).toBeTrue();
  }));

  it('puts API field errors on their controls and returns the rest for the page', () => {
    const form = createLoginForm();
    form.setValue({ email: 'ana@ticksi.com', password: 'Secret1' });

    const fields = new ApiError(400, 'validation_failed', 'x', { Email: ['Taken.'], other: ['Something else.'] });
    expect(serverFailure(form, fields)).toBe('Something else.');
    expect(authErrorText(form.controls.email, 'Email')).toBe('Taken.');

    expect(serverFailure(form, new ApiError(401, 'unauthorized', 'Invalid email or password.'))).toBe('Invalid email or password.');
    expect(serverFailure(form, new Error('boom'))).toBe('Something went wrong. Please try again.');
  });
});
