import { Routes } from '@angular/router';
import { guestGuard } from '../../core/guards/guest.guard';

export const LOGIN_ROUTES: Routes = [{ path: '', canActivate: [guestGuard], loadComponent: () => import('./login.component').then((m) => m.LoginComponent) }];

export const REGISTER_ROUTES: Routes = [
  { path: '', canActivate: [guestGuard], loadComponent: () => import('./register.component').then((m) => m.RegisterComponent) }
];
