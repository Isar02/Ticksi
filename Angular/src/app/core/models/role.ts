export const Role = {
  Admin: 'Admin',
  Organizer: 'Organizer',
  User: 'User'
} as const;

export type Role = (typeof Role)[keyof typeof Role];

// The API lets the same roles manage categories and events.
export const MANAGER_ROLES: readonly Role[] = [Role.Admin, Role.Organizer];
