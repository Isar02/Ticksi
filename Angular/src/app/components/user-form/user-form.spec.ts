import { UserAccount } from '../../models/user.model';
import { applyServerErrors } from '../shared/form-rules';
import { authErrorText as errorText } from '../auth/auth-forms';
import { createUserForm, fillFromUser, toNewUserInput, toUserInput } from './user-form';

describe('user form', () => {
  const account: UserAccount = {
    publicId: 'user-amar',
    firstName: 'Amar',
    lastName: 'Hadzic',
    email: 'amar@ticksi.com',
    phone: '+387 62 555 444',
    roleId: 'role-organizer',
    roleName: 'Organizer',
    isActive: false,
    registrationDate: '2026-09-01T10:00:00'
  };

  it('applies the API rules to every field', () => {
    const form = createUserForm(true);
    form.setValue({ firstName: '  A ', lastName: '   ', email: 'not-an-email', phone: '12ab', roleId: '', isActive: true, password: '12345' });

    const messages = {
      firstName: errorText(form.controls.firstName, 'First name'),
      lastName: errorText(form.controls.lastName, 'Last name'),
      email: errorText(form.controls.email, 'Email'),
      phone: errorText(form.controls.phone, 'Phone'),
      roleId: errorText(form.controls.roleId, 'Role'),
      password: errorText(form.controls.password, 'Password')
    };

    expect(messages).toEqual({
      firstName: 'First name must be at least 2 characters.',
      lastName: 'Last name is required.',
      email: 'Invalid email format.',
      phone: 'Please enter a valid phone number.',
      roleId: 'Role is required.',
      password: 'Password must be at least 6 characters.'
    });
  });

  it('accepts a complete account and sends it trimmed, with the password only on create', () => {
    const form = createUserForm(true);
    form.setValue({
      firstName: ' Amar ',
      lastName: 'Hadzic ',
      email: ' amar@ticksi.com',
      phone: '+387 62 555 444',
      roleId: 'role-user',
      isActive: true,
      password: 'Secret1!'
    });

    expect(form.valid).toBeTrue();
    expect(toNewUserInput(form)).toEqual({
      firstName: 'Amar',
      lastName: 'Hadzic',
      email: 'amar@ticksi.com',
      phone: '+387 62 555 444',
      roleId: 'role-user',
      isActive: true,
      password: 'Secret1!'
    });
    expect(Object.keys(toUserInput(form))).not.toContain('password');
  });

  it('rejects whitespace-only email, phone and password with the API required messages', () => {
    for (const [field, label, value] of [
      ['email', 'Email', '   '],
      ['phone', 'Phone', '         '],
      ['password', 'Password', '      ']
    ] as const) {
      const form = createUserForm(true);
      fillFromUser(form, account);
      form.controls.password.setValue('Secret1!');
      form.controls[field].setValue(value);

      expect(form.invalid).withContext(field).toBeTrue();
      expect(errorText(form.controls[field], label)).toBe(`${label} is required.`);
    }
  });

  it('uses the API email rule instead of rejecting API-accepted addresses', () => {
    const form = createUserForm(false);
    fillFromUser(form, account);

    for (const email of ['a..b@example.com', '"a b"@example.com', 'a@b', ' a..b@example.com ']) {
      form.controls.email.setValue(email);
      expect(form.valid).withContext(email).toBeTrue();
    }

    for (const email of ['not-an-email', '@example.com', 'local@', 'a@b@c']) {
      form.controls.email.setValue(email);
      expect(errorText(form.controls.email, 'Email')).withContext(email).toBe('Invalid email format.');
    }
  });

  it('preserves spaces around a nonblank password when creating an account', () => {
    const form = createUserForm(true);
    fillFromUser(form, account);
    form.controls.password.setValue(' Secret1! ');

    expect(form.valid).toBeTrue();
    expect(toNewUserInput(form).password).toBe(' Secret1! ');
  });

  it('leaves the password out of the edit form and fills the account', () => {
    const form = createUserForm(false);
    fillFromUser(form, account);

    expect(form.controls.password.disabled).toBeTrue();
    expect(form.valid).toBeTrue();
    expect(toUserInput(form)).toEqual(jasmine.objectContaining({ roleId: 'role-organizer', isActive: false }));
  });

  it('puts API errors on their fields and returns the ones that fit none', () => {
    const form = createUserForm(false);
    fillFromUser(form, account);

    const unplaced = applyServerErrors(form, {
      email: ['An account with this email already exists.'],
      RoleId: ['Role does not exist.'],
      password: ['Password is required.'],
      general: ['Something else.']
    });

    expect(errorText(form.controls.email, 'Email')).toBe('An account with this email already exists.');
    expect(errorText(form.controls.roleId, 'Role')).toBe('Role does not exist.');
    expect(form.controls.email.touched).toBeTrue();
    expect(unplaced).toEqual(['Password is required.', 'Something else.']);

    form.controls.email.setValue('new@ticksi.com');
    expect(form.controls.email.valid).toBeTrue();
  });
});
