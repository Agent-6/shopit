export interface RoleClaim {
  type: string;
  value: string;
}

export interface CreateRoleRequest {
  name: string;
  description?: string | null;
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
}

export interface RoleDetail extends Role {
  claims: RoleClaim[];
}

