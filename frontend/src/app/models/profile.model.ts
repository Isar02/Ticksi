export interface Profile {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  role: string;
  registrationDate: string;
}

export interface ProfileInput {
  firstName: string;
  lastName: string;
  phone: string;
}

export interface PasswordChange {
  currentPassword: string;
  newPassword: string;
}
