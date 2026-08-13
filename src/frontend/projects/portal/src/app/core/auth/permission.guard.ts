import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from './permission.service';

/**
 * Returns a route guard that allows navigation only when the current user holds the
 * given permission, redirecting to /forbidden otherwise.
 */
export function permissionGuard(permission: string): CanActivateFn {
  return async () => {
    const permissionService = inject(PermissionService);
    const router = inject(Router);

    await permissionService.load();

    if (permissionService.has(permission)) {
      return true;
    }

    return router.createUrlTree(['/forbidden']);
  };
}
