export const USER_LIMITS = { nameMin: 2, name: 100, email: 256, phone: 20, passwordMin: 6 } as const;

export const PHONE_PATTERN = /^\+?[0-9\s-]{9,}$/;

export interface UserAccount {
  publicId: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  roleId: string;
  roleName: string;
  isActive: boolean;
  registrationDate: string;
}

export interface RoleOption {
  publicId: string;
  name: string;
}

export interface UserInput {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  roleId: string;
  isActive: boolean;
}

export interface NewUserInput extends UserInput {
  password: string;
}

export const ROLE_DESCRIPTIONS: Readonly<Partial<Record<string, string>>> = {
  Admin: 'Manages every account, event and category.',
  Organizer: 'Creates events and manages their own.',
  User: 'Browses events and buys tickets, without managing anything.'
};

export const USER_STATUSES = ['active', 'inactive'] as const;
export type UserStatus = (typeof USER_STATUSES)[number];

export const USER_SORTS = ['name', 'email', 'role', 'registered'] as const;
export type UserSort = (typeof USER_SORTS)[number];

export interface UsersQuery {
  search?: string;
  roleId?: string;
  status?: UserStatus;
  registeredFrom?: string;
  registeredTo?: string;
  sortBy?: UserSort;
  sortDescending?: boolean;
  page: number;
  pageSize: number;
}
