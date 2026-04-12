import { Routes } from '@angular/router';
import { UsersPageComponent } from './users/users-page.component';
import { UserDetailComponent } from './users/user-detail.component';
import { authGuard } from './auth.guard';
import { AuthCallbackComponent } from './auth-callback.component';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { 
    path: '', 
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'users', pathMatch: 'full' },
      { path: 'users', component: UsersPageComponent },
      { path: 'users/:id', component: UserDetailComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];
