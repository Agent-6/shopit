import { Routes } from '@angular/router';
import { UsersPageComponent } from './pages/users/users-page.component';
import { UserDetailComponent } from './pages/users/user-detail.component';
import { RolesPageComponent } from './pages/roles/roles-page.component';
import { RoleDetailComponent } from './pages/roles/role-detail.component';
import { PermissionMatrixComponent } from './pages/permissions/permission-matrix.component';
import { TenantsPageComponent } from './pages/tenants/tenants-page.component';
import { TenantDetailComponent } from './pages/tenants/tenant-detail.component';
import { authGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { ShopItPermissions } from './core/auth/permissions';
import { AuthCallbackComponent } from './core/auth/auth-callback.component';
import { LayoutComponent } from './layout/layout.component';
import { ForbiddenPageComponent } from './pages/forbidden/forbidden-page.component';
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
      {
        path: APP_ROUTES.users.list.path,
        component: UsersPageComponent,
        canActivate: [permissionGuard(ShopItPermissions.Users.View)]
      },
      {
        path: APP_ROUTES.users.detail.path,
        component: UserDetailComponent,
        canActivate: [permissionGuard(ShopItPermissions.Users.View)]
      },
      {
        path: APP_ROUTES.roles.list.path,
        component: RolesPageComponent,
        canActivate: [permissionGuard(ShopItPermissions.Roles.View)]
      },
      {
        path: APP_ROUTES.roles.detail.path,
        component: RoleDetailComponent,
        canActivate: [permissionGuard(ShopItPermissions.Roles.View)]
      },
      {
        path: APP_ROUTES.permissions.matrix.path,
        component: PermissionMatrixComponent,
        canActivate: [permissionGuard(ShopItPermissions.Roles.ManagePermissions)]
      },
      {
        path: APP_ROUTES.tenants.list.path,
        component: TenantsPageComponent,
        canActivate: [permissionGuard(ShopItPermissions.Tenants.View)]
      },
      {
        path: APP_ROUTES.tenants.detail.path,
        component: TenantDetailComponent,
        canActivate: [permissionGuard(ShopItPermissions.Tenants.View)]
      },
      { path: 'forbidden', component: ForbiddenPageComponent }
    ]
  },
  { path: '**', redirectTo: APP_ROUTES.home.path }
];
