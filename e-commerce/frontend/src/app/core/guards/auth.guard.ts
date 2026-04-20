import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/** Exige access token em memória (restaurado pelo APP_INITIALIZER quando há cookie de refresh). */
export const authGuard: CanActivateFn = (): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.ensureAuthenticated()) {
    return true;
  }
  return router.createUrlTree(['/login']);
};
