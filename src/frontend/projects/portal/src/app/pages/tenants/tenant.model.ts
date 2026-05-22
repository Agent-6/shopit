export interface Tenant {
  id: string;
  name: string;
  isActive: boolean;
  createdOn: Date;
  lastModifiedOn: Date;
}

export interface CreateTenantRequest {
  name: string;
}

export interface UpdateTenantRequest {
  id: string;
  name: string;
}
