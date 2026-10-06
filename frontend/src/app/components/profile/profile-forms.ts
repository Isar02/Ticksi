import { FormControl, FormGroup } from '@angular/forms';
import { PasswordChange, Profile, ProfileInput } from '../../models/profile.model';
import { requiredText } from '../shared/form-rules';
import { matches, nameControl, newPasswordControl, phoneControl } from '../auth/auth-forms';

export type DetailsForm = ReturnType<typeof createDetailsForm>;
export type PasswordForm = ReturnType<typeof createPasswordForm>;

export function createDetailsForm() {
  return new FormGroup({
    firstName: nameControl(),
    lastName: nameControl(),
    phone: phoneControl()
  });
}

export function createPasswordForm() {
  return new FormGroup({
    currentPassword: new FormControl('', { nonNullable: true, validators: requiredText }),
    newPassword: newPasswordControl(),
    confirmPassword: new FormControl('', { nonNullable: true, validators: [requiredText, matches('newPassword')] })
  });
}

export function detailsOf(profile: Profile): ProfileInput {
  return { firstName: profile.firstName, lastName: profile.lastName, phone: profile.phone };
}

export function toProfileInput(form: DetailsForm): ProfileInput {
  const value = form.getRawValue();
  return { firstName: value.firstName.trim(), lastName: value.lastName.trim(), phone: value.phone.trim() };
}

export function toPasswordChange(form: PasswordForm): PasswordChange {
  const value = form.getRawValue();
  return { currentPassword: value.currentPassword, newPassword: value.newPassword };
}
