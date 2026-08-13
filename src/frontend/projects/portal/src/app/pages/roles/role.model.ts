export interface RoleClaimRequest {
  claimType: string;
  claimValue: string;
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
  claims: RoleClaimRequest[];
}

export interface UpdateRoleClaimsRequest {
  claims: RoleClaimRequest[];
}
