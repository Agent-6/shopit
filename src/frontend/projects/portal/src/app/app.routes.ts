import { Routes } from '@angular/router';
import { UsersPageComponent } from './users/users-page.component';
import { UserDetailComponent } from './users/user-detail.component';
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
      { path: APP_ROUTES.users.detail.path, component: UserDetailComponent }
    ]
  },
  { path: '**', redirectTo: APP_ROUTES.home.path }
];
