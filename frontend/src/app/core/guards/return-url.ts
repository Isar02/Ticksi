import { ActivatedRoute, Router, UrlTree } from '@angular/router';

const RETURN_URL = 'returnUrl';

export function loginUrl(router: Router, returnUrl: string): UrlTree {
  return router.createUrlTree(['/auth/login'], { queryParams: { [RETURN_URL]: returnUrl } });
}

export function readReturnUrl(route: ActivatedRoute): string {
  const returnUrl = route.snapshot.queryParamMap.get(RETURN_URL);
  return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/';
}
