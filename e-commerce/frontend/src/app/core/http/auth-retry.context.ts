import { HttpContextToken } from '@angular/common/http';

/** Marca a segunda tentativa após refresh para evitar loop infinito de 401 → refresh. */
export const SKIP_AUTH_REFRESH = new HttpContextToken<boolean>(() => false);
