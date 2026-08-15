export type PermissionMultiTenancySide = 'Both' | 'Host' | 'Tenant';

export interface PermissionDefinition {
  name: string;
  displayName: string;
  description?: string | null;
  multiTenancySide?: PermissionMultiTenancySide;
}

export interface PermissionGroup {
  name: string;
  displayName: string;
  permissions: PermissionDefinition[];
}

export interface MatrixClaim {
  type: string;
  value: string;
}

export interface MatrixRole {
  id: string;
  name: string;
  tenantId: string;
  multiTenancySide?: PermissionMultiTenancySide;
  claims: MatrixClaim[];
}

export interface PermissionMatrix {
  groups: PermissionGroup[];
  roles: MatrixRole[];
}
