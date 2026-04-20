import { Routes } from '@angular/router';
import { adminGuard } from '../../core/guards/admin.guard';
import { authGuard } from '../../core/guards/auth.guard';

export const ADMIN_ROUTES: Routes = [
  { path: '', canActivate: [authGuard, adminGuard], loadComponent: () => import('./admin.component').then((m) => m.AdminComponent) }
];
