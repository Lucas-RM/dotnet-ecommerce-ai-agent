import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/** Exige usuário autenticado com papel Admin. */
export const adminGuard: CanActivateFn = (): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.ensureAuthenticated()) {
    return router.createUrlTree(['/login']);
  }
  if (auth.hasRole('Admin')) {
    return true;
  }
  return router.createUrlTree(['/catalog']);
};
