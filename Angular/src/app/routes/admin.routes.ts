import { Routes } from '@angular/router';
import { CategoriesComponent } from '../components/categories/categories.component';
import { AdminUsersComponent } from '../components/admin-users/admin-users.component';
import { roleGuard } from '../core/guards/role.guard';
import { Role } from '../core/models/role';

export const ADMIN_ROUTES: Routes = [
  { path: 'categories', component: CategoriesComponent },
  { path: 'users', canMatch: [roleGuard([Role.Admin])], component: AdminUsersComponent }
];
