import { AbstractControl, AsyncValidatorFn, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Observable, catchError, map, of, switchMap, timer } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { LoginRequest, RegisterRequest } from '../../models/auth.models';
import { PHONE_PATTERN, USER_LIMITS } from '../../models/user.model';
import { applyServerErrors, apiEmail, requiredText, trimmedMinLength, trimmedPattern } from '../shared/form-rules';

export const EMAIL_CHECK_DELAY_MS = 400;
export const EMAIL_TAKEN = 'An account with this email already exists.';

export type EmailCheck = (email: string) => Observable<boolean>;
export type LoginForm = ReturnType<typeof createLoginForm>;
export type RegisterForm = ReturnType<typeof createRegisterForm>;

export function createLoginForm() {
  return new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [requiredText, apiEmail] }),
    password: new FormControl('', { nonNullable: true, validators: requiredText })
  });
}

export function nameControl() {
  return new FormControl('', {
    nonNullable: true,
    validators: [requiredText, trimmedMinLength(USER_LIMITS.nameMin), Validators.maxLength(USER_LIMITS.name)]
  });
}

export function phoneControl() {
  return new FormControl('', {
    nonNullable: true,
    validators: [requiredText, trimmedPattern(PHONE_PATTERN), Validators.maxLength(USER_LIMITS.phone)]
  });
}

export function newPasswordControl() {
  return new FormControl('', { nonNullable: true, validators: [requiredText, Validators.minLength(USER_LIMITS.passwordMin)] });
}

export function createRegisterForm(isEmailAvailable: EmailCheck) {
  return new FormGroup({
    firstName: nameControl(),
    lastName: nameControl(),
    email: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, apiEmail, Validators.maxLength(USER_LIMITS.email)],
      asyncValidators: emailAvailable(isEmailAvailable)
    }),
    phone: phoneControl(),
    password: newPasswordControl(),
    confirmPassword: new FormControl('', { nonNullable: true, validators: [requiredText, matches('password')] })
  });
}

// Angular cancels the previous check on every keystroke, so the timer also debounces the requests.
export function emailAvailable(isEmailAvailable: EmailCheck): AsyncValidatorFn {
  return (control: AbstractControl<string>) =>
    timer(EMAIL_CHECK_DELAY_MS).pipe(
      switchMap(() => isEmailAvailable(control.value.trim())),
      catchError(() => of(null)),
      map((available): ValidationErrors | null => {
        const errors: ValidationErrors = {};
        if (available === false) errors['emailTaken'] = true;
        if (control.errors?.['server']) errors['server'] = control.errors['server'];
        return Object.keys(errors).length > 0 ? errors : null;
      })
    );
}

export function toLoginRequest(form: LoginForm): LoginRequest {
  const value = form.getRawValue();
  return { email: value.email.trim(), password: value.password };
}

export function toRegisterRequest(form: RegisterForm): RegisterRequest {
  const value = form.getRawValue();
  return {
    firstName: value.firstName.trim(),
    lastName: value.lastName.trim(),
    email: value.email.trim(),
    phone: value.phone.trim(),
    password: value.password
  };
}

export function serverFailure(form: FormGroup, error: unknown): string | null {
  if (!(error instanceof ApiError)) {
    return 'Something went wrong. Please try again.';
  }

  if (Object.keys(error.fieldErrors).length === 0) {
    return error.message;
  }

  const unplaced = applyServerErrors(form, error.fieldErrors);
  return unplaced.length > 0 ? unplaced.join(' ') : null;
}

export function authErrorText(control: AbstractControl, label: string): string {
  const errors = control.errors ?? {};

  if (errors['server']) return errors['server'];
  if (errors['required']) return `${label} is required.`;
  if (errors['trimmedMinLength']) return `${label} must be at least ${errors['trimmedMinLength'].requiredLength} characters.`;
  if (errors['minlength']) return `${label} must be at least ${errors['minlength'].requiredLength} characters.`;
  if (errors['maxlength']) return `${label} cannot exceed ${errors['maxlength'].requiredLength} characters.`;
  if (errors['email']) return 'Invalid email format.';
  if (errors['emailTaken']) return EMAIL_TAKEN;
  if (errors['pattern']) return 'Please enter a valid phone number.';
  if (errors['passwordMismatch']) return 'Passwords do not match.';
  return '';
}

export function matches(passwordControl: string): ValidatorFn {
  return (control: AbstractControl<string>): ValidationErrors | null => {
    const password = control.parent?.get(passwordControl)?.value;
    return control.value && password !== undefined && control.value !== password ? { passwordMismatch: true } : null;
  };
}
