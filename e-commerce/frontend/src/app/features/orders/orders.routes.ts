import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const ORDERS_ROUTES: Routes = [
  { path: '', canActivate: [authGuard], loadComponent: () => import('./order-list.component').then((m) => m.OrderListComponent) },
  { path: ':id', canActivate: [authGuard], loadComponent: () => import('./order-detail.component').then((m) => m.OrderDetailComponent) }
];
