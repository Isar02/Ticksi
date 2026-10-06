import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

export const guestGuard: CanMatchFn = () =>
  !inject(AuthService).isAuthenticated() || inject(Router).createUrlTree(['/']);
