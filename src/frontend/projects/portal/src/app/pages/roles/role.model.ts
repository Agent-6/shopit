export type RoleMultiTenancySide = 'Both' | 'Host' | 'Tenant';

export interface RoleClaim {
  type: string;
  value: string;
}

export interface CreateRoleRequest {
  name: string;
  description?: string | null;
  multiTenancySide?: RoleMultiTenancySide;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string | null;
}

export interface Role {
  id: string;
  name: string;
  description?: string | null;
  createdAt: Date;
  multiTenancySide?: RoleMultiTenancySide;
}

export interface RoleDetail extends Role {
  claims: RoleClaim[];
}

