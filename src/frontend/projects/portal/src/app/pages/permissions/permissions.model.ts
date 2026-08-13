export interface PermissionDefinition {
  name: string;
  displayName: string;
  description?: string | null;
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
  claims: MatrixClaim[];
}

export interface PermissionMatrix {
  groups: PermissionGroup[];
  roles: MatrixRole[];
}
