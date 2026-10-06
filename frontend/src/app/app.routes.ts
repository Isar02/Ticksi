import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { roleGuard } from './core/guards/role.guard';
import { MANAGER_ROLES } from './core/models/role';

export const routes: Routes = [
  {
    path: 'auth',
    canMatch: [guestGuard],
    loadChildren: () => import('./routes/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'admin',
    canMatch: [authGuard, roleGuard(MANAGER_ROLES)],
    loadChildren: () => import('./routes/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'organizer',
    canMatch: [authGuard, roleGuard(MANAGER_ROLES)],
    loadChildren: () => import('./routes/organizer.routes').then(m => m.ORGANIZER_ROUTES)
  },
  {
    path: 'dashboard',
    canMatch: [authGuard],
    loadComponent: () => import('./components/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'profile',
    canMatch: [authGuard],
    loadComponent: () => import('./components/profile/profile.component').then(m => m.ProfileComponent)
  },
  {
    path: 'favorites',
    canMatch: [authGuard],
    loadComponent: () => import('./components/favorites/favorites.component').then(m => m.FavoritesComponent)
  },
  {
    path: 'tickets',
    canMatch: [authGuard],
    loadComponent: () => import('./components/tickets/my-tickets/my-tickets.component').then(m => m.MyTicketsComponent)
  },
  {
    path: 'orders/:id',
    canMatch: [authGuard],
    loadComponent: () => import('./components/orders/order-details/order-details.component').then(m => m.OrderDetailsComponent)
  },
  {
    path: '',
    loadChildren: () => import('./routes/public.routes').then(m => m.PUBLIC_ROUTES)
  },
  { path: '**', redirectTo: '' }
];
