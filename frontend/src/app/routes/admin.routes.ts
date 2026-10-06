import { Routes } from '@angular/router';
import { CategoriesComponent } from '../components/categories/categories.component';
import { AdminUsersComponent } from '../components/admin-users/admin-users.component';
import { UserFormComponent } from '../components/user-form/user-form.component';
import { roleGuard } from '../core/guards/role.guard';
import { Role } from '../core/models/role';

const adminOnly = roleGuard([Role.Admin]);

export const ADMIN_ROUTES: Routes = [
  { path: 'categories', component: CategoriesComponent },
  { path: 'users', canMatch: [adminOnly], component: AdminUsersComponent },
  { path: 'users/new', canMatch: [adminOnly], component: UserFormComponent },
  { path: 'users/:id/edit', canMatch: [adminOnly], component: UserFormComponent }
];
