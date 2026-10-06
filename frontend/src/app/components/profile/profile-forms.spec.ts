import { authErrorText } from '../auth/auth-forms';
import { createDetailsForm, createPasswordForm, detailsOf, toPasswordChange, toProfileInput } from './profile-forms';

describe('profile forms', () => {
  it('applies the registration rules to the details with the API wording', () => {
    const form = createDetailsForm();
    form.setValue({ firstName: ' A ', lastName: '   ', phone: '12-34' });

    expect([
      authErrorText(form.controls.firstName, 'First name'),
      authErrorText(form.controls.lastName, 'Last name'),
      authErrorText(form.controls.phone, 'Phone')
    ]).toEqual(['First name must be at least 2 characters.', 'Last name is required.', 'Please enter a valid phone number.']);
  });

  it('starts from the saved details and sends them trimmed', () => {
    const form = createDetailsForm();
    form.reset(detailsOf({
      firstName: 'Lejla',
      lastName: 'Begić',
      email: 'lejla@ticksi.com',
      phone: '061 123 456',
      role: 'User',
      registrationDate: '2026-10-04T16:41:04Z'
    }));
    form.patchValue({ firstName: '  Amra ', phone: '+387 61 987 654 ' });

    expect(form.valid).toBeTrue();
    expect(toProfileInput(form)).toEqual({ firstName: 'Amra', lastName: 'Begić', phone: '+387 61 987 654' });
  });

  it('asks for the current password, a valid new one and the same confirmation', () => {
    const form = createPasswordForm();
    form.setValue({ currentPassword: ' ', newPassword: '12345', confirmPassword: '123456' });

    expect([
      authErrorText(form.controls.currentPassword, 'Current password'),
      authErrorText(form.controls.newPassword, 'New password'),
      authErrorText(form.controls.confirmPassword, 'Password confirmation')
    ]).toEqual(['Current password is required.', 'New password must be at least 6 characters.', 'Passwords do not match.']);
  });

  it('sends the passwords as typed', () => {
    const form = createPasswordForm();
    form.setValue({ currentPassword: ' Secret123', newPassword: 'Changed 456', confirmPassword: 'Changed 456' });

    expect(form.valid).toBeTrue();
    expect(toPasswordChange(form)).toEqual({ currentPassword: ' Secret123', newPassword: 'Changed 456' });
  });
});
