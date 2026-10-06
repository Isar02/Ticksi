import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { loginUrl } from './return-url';

export const authGuard: CanMatchFn = () => {
  const router = inject(Router);
  if (inject(AuthService).isAuthenticated()) {
    return true;
  }

  const navigation = router.getCurrentNavigation();
  return loginUrl(router, navigation ? router.serializeUrl(navigation.extractedUrl) : router.url);
};
