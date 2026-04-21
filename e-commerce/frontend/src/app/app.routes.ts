import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'catalog' },
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.LOGIN_ROUTES)
  },
  {
    path: 'register',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.REGISTER_ROUTES)
  },
  {
    path: 'catalog',
    loadChildren: () => import('./features/catalog/catalog.routes').then((m) => m.CATALOG_ROUTES)
  },
  {
    path: 'cart',
    loadChildren: () => import('./features/cart/cart.routes').then((m) => m.CART_ROUTES)
  },
  {
    path: 'checkout',
    loadChildren: () => import('./features/checkout/checkout.routes').then((m) => m.CHECKOUT_ROUTES)
  },
  {
    path: 'orders',
    loadChildren: () => import('./features/orders/orders.routes').then((m) => m.ORDERS_ROUTES)
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES)
  },
  {
    path: 'profile',
    loadChildren: () => import('./features/profile/profile.routes').then((m) => m.PROFILE_ROUTES)
  },
  {
    path: 'chat',
    canActivate: [authGuard],
    loadComponent: () => import('./features/agent-chat/agent-chat.component').then((m) => m.AgentChatComponent)
  }
];
