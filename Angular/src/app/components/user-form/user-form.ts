import { AbstractControl, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { NewUserInput, UserAccount, UserInput } from '../../models/user.model';

export const USER_LIMITS = { nameMin: 2, name: 100, email: 256, phone: 20, passwordMin: 6 } as const;

const PHONE = /^\+?[0-9\s-]{9,}$/;

export type UserForm = ReturnType<typeof createUserForm>;

export function createUserForm(withPassword: boolean) {
  const name = () =>
    new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, trimmedMinLength(USER_LIMITS.nameMin), Validators.maxLength(USER_LIMITS.name)]
    });

  return new FormGroup({
    firstName: name(),
    lastName: name(),
    email: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, apiEmail, Validators.maxLength(USER_LIMITS.email)]
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.pattern(PHONE), Validators.maxLength(USER_LIMITS.phone)]
    }),
    roleId: new FormControl('', { nonNullable: true, validators: Validators.required }),
    isActive: new FormControl(true, { nonNullable: true }),
    password: new FormControl(
      { value: '', disabled: !withPassword },
      { nonNullable: true, validators: [requiredText, Validators.minLength(USER_LIMITS.passwordMin)] }
    )
  });
}

export function fillFromUser(form: UserForm, user: UserAccount): void {
  form.patchValue({
    firstName: user.firstName,
    lastName: user.lastName,
    email: user.email,
    phone: user.phone,
    roleId: user.roleId,
    isActive: user.isActive
  });
}

export function toUserInput(form: UserForm): UserInput {
  const value = form.getRawValue();
  return {
    firstName: value.firstName.trim(),
    lastName: value.lastName.trim(),
    email: value.email.trim(),
    phone: value.phone.trim(),
    roleId: value.roleId,
    isActive: value.isActive
  };
}

export function toNewUserInput(form: UserForm): NewUserInput {
  return { ...toUserInput(form), password: form.getRawValue().password };
}

// Puts each API field error on its control and returns the messages that fit no field.
export function applyServerErrors(form: UserForm, fieldErrors: Readonly<Record<string, string[]>>): string[] {
  const unplaced: string[] = [];

  for (const [field, messages] of Object.entries(fieldErrors)) {
    const key = field.replace(/^\$\./, '').toLowerCase();
    const control = Object.entries(form.controls).find(([name]) => name.toLowerCase() === key)?.[1];

    if (control && control.enabled) {
      control.setErrors({ ...control.errors, server: messages[0] });
      control.markAsTouched();
    } else {
      unplaced.push(...messages);
    }
  }

  return unplaced;
}

export function errorText(control: AbstractControl, label: string): string {
  const errors = control.errors ?? {};

  if (errors['server']) return errors['server'];
  if (errors['required']) return `${label} is required.`;
  if (errors['trimmedMinLength']) return `${label} must be at least ${errors['trimmedMinLength'].requiredLength} characters.`;
  if (errors['minlength']) return `${label} must be at least ${errors['minlength'].requiredLength} characters.`;
  if (errors['maxlength']) return `${label} can have at most ${errors['maxlength'].requiredLength} characters.`;
  if (errors['email']) return 'Enter a valid email address.';
  if (errors['pattern']) return 'Enter a valid phone number.';
  return '';
}

// FluentValidation's NotEmpty rejects whitespace-only strings; Angular's required does not.
function requiredText(control: AbstractControl<string>): ValidationErrors | null {
  return control.value?.trim() ? null : { required: true };
}

// Same check as the API's default FluentValidation EmailAddress validator, on the value sent to it.
function apiEmail(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value?.trim() ?? '';
  if (!value) return null;

  const at = value.indexOf('@');
  return at > 0 && at < value.length - 1 && at === value.lastIndexOf('@') ? null : { email: true };
}

function trimmedMinLength(requiredLength: number): ValidatorFn {
  return (control: AbstractControl<string>): ValidationErrors | null => {
    const value = control.value ?? '';
    if (!value) return null;
    if (!value.trim()) return { required: true };
    return value.trim().length < requiredLength ? { trimmedMinLength: { requiredLength } } : null;
  };
}
