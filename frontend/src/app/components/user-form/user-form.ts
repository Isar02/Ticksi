import { FormControl, FormGroup, Validators } from '@angular/forms';
import { NewUserInput, USER_LIMITS, UserAccount, UserInput } from '../../models/user.model';
import { nameControl, newPasswordControl, phoneControl } from '../auth/auth-forms';
import { apiEmail, requiredText } from '../shared/form-rules';

export type UserForm = ReturnType<typeof createUserForm>;

export function createUserForm(withPassword: boolean) {
  const password = newPasswordControl();
  if (!withPassword) password.disable();

  return new FormGroup({
    firstName: nameControl(),
    lastName: nameControl(),
    email: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, apiEmail, Validators.maxLength(USER_LIMITS.email)]
    }),
    phone: phoneControl(),
    roleId: new FormControl('', { nonNullable: true, validators: Validators.required }),
    isActive: new FormControl(true, { nonNullable: true }),
    password
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
