import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { SKIP_AUTH_REFRESH } from '../http/auth-retry.context';
import { AuthService } from '../services/auth.service';
import { getApiErrorMessage } from '../utils/api-error-message';

function isAuthFlow401Url(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/register') || url.includes('/auth/refresh');
}

/**
 * Em 401: tenta `refresh` uma vez e repete a requisição com novo Bearer (`SKIP_AUTH_REFRESH` evita loop).
 * 403: mensagem de permissão. Demais erros: snack com mensagem da API quando existir.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const snack = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        if (isAuthFlow401Url(req.url)) {
          return throwError(() => error);
        }
        if (req.context.get(SKIP_AUTH_REFRESH)) {
          auth.clearLocalSession();
          void router.navigate(['/login']);
          return throwError(() => error);
        }
        return auth.refreshToken().pipe(
          switchMap((token) =>
            next(
              req.clone({
                setHeaders: { Authorization: `Bearer ${token}` },
                context: req.context.set(SKIP_AUTH_REFRESH, true)
              })
            )
          ),
          catchError((refreshErr) => {
            auth.clearLocalSession();
            void router.navigate(['/login']);
            return throwError(() => refreshErr);
          })
        );
      }

      if (error.status === 403) {
        snack.open(getApiErrorMessage(error, 'Você não tem permissão para esta ação.'), 'Fechar', { duration: 4000 });
        return throwError(() => error);
      }

      snack.open(getApiErrorMessage(error), 'Fechar', { duration: 4000 });
      return throwError(() => error);
    })
  );
};
