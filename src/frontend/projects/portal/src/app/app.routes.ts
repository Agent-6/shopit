import { Routes } from '@angular/router';
import { UsersPageComponent } from './users/users-page.component';
import { UserDetailComponent } from './users/user-detail.component';

export const routes: Routes = [
  { path: '', redirectTo: 'users', pathMatch: 'full' },
  { path: 'users', component: UsersPageComponent },
  { path: 'users/:id', component: UserDetailComponent },
  { path: '**', redirectTo: 'users' }
];
