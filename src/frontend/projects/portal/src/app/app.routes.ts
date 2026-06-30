import { Routes } from '@angular/router';
import { UsersPageComponent } from './pages/users/users-page.component';
import { UserDetailComponent } from './pages/users/user-detail.component';
import { TenantsPageComponent } from './pages/tenants/tenants-page.component';
import { TenantDetailComponent } from './pages/tenants/tenant-detail.component';
import { authGuard } from './core/auth/auth.guard';
import { AuthCallbackComponent } from './core/auth/auth-callback.component';
import { LayoutComponent } from './layout/layout.component';
import { APP_ROUTES } from './routes';

export const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: APP_ROUTES.home.path, redirectTo: 'users', pathMatch: 'full' },
      { path: APP_ROUTES.users.list.path, component: UsersPageComponent },
      { path: APP_ROUTES.users.detail.path, component: UserDetailComponent },
      { path: APP_ROUTES.tenants.list.path, component: TenantsPageComponent },
      { path: APP_ROUTES.tenants.detail.path, component: TenantDetailComponent }
    ]
  },
  { path: '**', redirectTo: APP_ROUTES.home.path }
];
