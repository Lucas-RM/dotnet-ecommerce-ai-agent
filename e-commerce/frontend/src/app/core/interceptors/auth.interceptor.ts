import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Anexa `Authorization` para a API e `withCredentials` (cookie de refresh em rotas cruzadas).
 * Não envia Bearer em login, registro ou refresh.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const base = environment.apiUrl;
  const agentBase = environment.agentApiUrl;

  if (req.url.startsWith(agentBase)) {
    const token = auth.getAccessToken();
    let nextReq = req;
    if (token) {
      nextReq = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
    return next(nextReq);
  }

  if (!req.url.startsWith(base)) {
    return next(req);
  }

  let nextReq = req.clone({ withCredentials: true });
  const skipBearer =
    nextReq.url.includes('/auth/login') || nextReq.url.includes('/auth/register') || nextReq.url.includes('/auth/refresh');

  if (!skipBearer) {
    const token = auth.getAccessToken();
    if (token) {
      nextReq = nextReq.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
  }

  return next(nextReq);
};
