export interface UserClaimRequest {
  claimType: string;
  claimValue: string;
}

export interface UserPermissionRequest {
  permissionName: string;
  isGranted: boolean;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  roles?: string[] | null;
  claims?: UserClaimRequest[] | null;
  emailConfirmed?: boolean | null;
  phoneNumberConfirmed?: boolean | null;
  isActive?: boolean | null;
}

export interface InviteUserRequest {
  email: string;
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  roles?: string[] | null;
  claims?: UserClaimRequest[] | null;
}

export interface UpdateUserRequest {
  username?: string | null;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  phoneNumberConfirmed?: boolean | null;
  isActive?: boolean | null;
  roles?: string[] | null;
  claims?: UserClaimRequest[] | null;
  emailConfirmed?: boolean | null;
}

export interface UpdateUserRolesRequest {
  roleNames: string[];
}

export interface LockUserRequest {
  lockoutEnd?: string | null;
}

export interface UpdateUserPasswordRequest {
  newPassword: string;
}

export interface DeleteUserResponse {
  id: string;
  isDeleted: boolean;
  deletedType: string;
}

export interface UpdateUserPermissionsRequest {
  permissions: UserPermissionRequest[];
}

export interface UpdateUserClaimsRequest {
  claims: UserClaimRequest[];
  removedClaims?: string[] | null;
}

export interface User {
  id: string;
  username: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  roles?: string[];
  claims?: UserClaimRequest[];
  emailConfirmed?: boolean;
  phoneNumberConfirmed?: boolean;
  twoFactorEnabled?: boolean;
  isActive: boolean;
  /** Active | Inactive | Suspended | PendingActivation */
  status?: string;
  lockoutEnabled?: boolean;
  lockoutEnd?: string | null;
  createdAt?: string;
  lastModifiedAt?: string;
}
