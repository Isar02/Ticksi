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
