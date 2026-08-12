/**
 * Canonical permission names, mirroring `ShopItIdentityPermissions` on the backend.
 * Permission names are stored as claim types on roles and users.
 */
export const ShopItPermissions = {
  Users: {
    View: 'user.view',
    Create: 'user.create',
    Update: 'user.update',
    Delete: 'user.delete',
    ManageRoles: 'user.manage-roles',
    ManagePermissions: 'user.manage-permissions',
    ManageClaims: 'user.manage-claims',
    LockUnlock: 'user.lock-unlock',
    ResetPassword: 'user.reset-password',
  },
  Roles: {
    View: 'role.view',
    Create: 'role.create',
    Update: 'role.update',
    Delete: 'role.delete',
    ManagePermissions: 'role.manage-permissions',
  },
  Tenants: {
    View: 'tenant.view',
    Create: 'tenant.create',
    Update: 'tenant.update',
    Delete: 'tenant.delete',
    ActivateDeactivate: 'tenant.activate-deactivate',
  },
} as const;
