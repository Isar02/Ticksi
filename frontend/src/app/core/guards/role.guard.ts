import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Role } from '../models/role';

export function roleGuard(roles: readonly Role[]): CanMatchFn {
  return () => inject(AuthService).hasAnyRole(roles) || inject(Router).createUrlTree(['/']);
}
