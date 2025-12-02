import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const adminGuard: CanActivateFn = () => {

  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.getToken();

  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  const role = auth.getUserRole();

  if (!role) {
    router.navigate(['/login']);
    return false;
  }

  if (role !== 'Admin' && role !== 'Organizer') {
    router.navigate(['/']);
    return false;
  }

  return true;
};
