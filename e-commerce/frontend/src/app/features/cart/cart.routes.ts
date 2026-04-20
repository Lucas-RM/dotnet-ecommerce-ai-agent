import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const CART_ROUTES: Routes = [
  { path: '', canActivate: [authGuard], loadComponent: () => import('./cart.component').then((m) => m.CartComponent) }
];
