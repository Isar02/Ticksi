import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { NewUserInput, PHONE_PATTERN, USER_LIMITS, UserAccount, UserInput } from '../../models/user.model';
import { apiEmail, requiredText, trimmedMinLength } from '../shared/form-rules';

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
      validators: [requiredText, Validators.pattern(PHONE_PATTERN), Validators.maxLength(USER_LIMITS.phone)]
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
