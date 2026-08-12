import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal, computed } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import {
  CreateUserRequest,
  DeleteUserResponse,
  LockUserRequest,
  UpdateUserClaimsRequest,
  UpdateUserPasswordRequest,
  UpdateUserPermissionsRequest,
  UpdateUserRequest,
  UpdateUserRolesRequest,
  User,
  UserClaimRequest,
  UserPermissionRequest
} from './users.model';
import { environment } from '../../../environments/environment';
import { PagedResponse } from '../../core/models/pagination';
import { Role } from '../roles/role.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/identity`;

  readonly users = signal<User[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedUser = signal<User | null>(null);
  readonly permissions = signal<UserPermissionRequest[]>([]);
  readonly claims = signal<UserClaimRequest[]>([]);
  readonly userRoles = signal<string[]>([]);
  readonly availableRoles = signal<Role[]>([]);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly filter = signal('');
  readonly sortBy = signal('username');
  readonly sortOrder = signal<'asc' | 'desc'>('asc');
  readonly totalCount = signal(0);

  readonly queryParams = computed(() => {
    let params = new HttpParams()
      .set('page', String(this.page()))
      .set('pageSize', String(this.pageSize()))
      .set('sortBy', this.sortBy())
      .set('sortOrder', this.sortOrder());

    if (this.filter().trim()) {
      params = params.set('filter', this.filter().trim());
    }

    return params;
  });

  readonly pageCount = computed(() => {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  });

  async loadUsers(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const result = await lastValueFrom(
        this.http.get<PagedResponse<User>>(`${this.baseUrl}/users`, {
          params: this.queryParams()
        })
      );

      this.users.set(result.items ?? []);
      this.totalCount.set(result.totalCount ?? 0);
    } catch (error) {
      this.error.set('Unable to load users. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  async loadUser(userId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const user = await lastValueFrom(this.http.get<User>(`${this.baseUrl}/users/${userId}`));
      this.selectedUser.set(user);
    } catch (error) {
      this.error.set('Unable to load user details.');
    } finally {
      this.loading.set(false);
    }
  }

  async createUser(payload: CreateUserRequest): Promise<User | null> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const created = await lastValueFrom(this.http.post<User>(`${this.baseUrl}/users`, payload));
      await this.loadUsers();
      return created;
    } catch (error) {
      this.error.set('Unable to create user. Please validate your input.');
      return null;
    } finally {
      this.loading.set(false);
    }
  }

  async updateUser(userId: string, payload: UpdateUserRequest): Promise<User | null> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const updated = await lastValueFrom(this.http.put<User>(`${this.baseUrl}/users/${userId}`, payload));
      await this.loadUsers();
      this.selectedUser.set(updated);
      return updated;
    } catch (error) {
      this.error.set('Unable to save changes. Please try again.');
      return null;
    } finally {
      this.loading.set(false);
    }
  }

  async removeUser(userId: string, permanent = false): Promise<DeleteUserResponse | null> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const params = new HttpParams().set('permanent', String(permanent));
      const response = await lastValueFrom(
        this.http.delete<DeleteUserResponse>(`${this.baseUrl}/users/${userId}`, { params })
      );
      await this.loadUsers();
      return response;
    } catch (error) {
      this.error.set('Unable to delete the user.');
      return null;
    } finally {
      this.loading.set(false);
    }
  }

  // ------------------------------------------------------------------
  // Permissions
  // ------------------------------------------------------------------

  async loadPermissions(userId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const response = await lastValueFrom(
        this.http.get<UserPermissionRequest[] | { permissions: UserPermissionRequest[] }>(
          `${this.baseUrl}/users/${userId}/permissions`
        )
      );
      this.permissions.set(Array.isArray(response) ? response : response.permissions ?? []);
    } catch (error) {
      this.error.set('Unable to load permissions.');
    } finally {
      this.loading.set(false);
    }
  }

  async savePermissions(userId: string, permissions: UserPermissionRequest[]): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.put<void>(`${this.baseUrl}/users/${userId}/permissions`, {
          permissions
        } as UpdateUserPermissionsRequest)
      );
      this.permissions.set(permissions);
    } catch (error) {
      this.error.set('Unable to update permissions.');
    } finally {
      this.loading.set(false);
    }
  }

  // ------------------------------------------------------------------
  // Claims
  // ------------------------------------------------------------------

  async loadClaims(userId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const response = await lastValueFrom(
        this.http.get<UserClaimRequest[] | { claims: UserClaimRequest[] }>(
          `${this.baseUrl}/users/${userId}/claims`
        )
      );
      this.claims.set(Array.isArray(response) ? response : response.claims ?? []);
    } catch (error) {
      this.error.set('Unable to load claims.');
    } finally {
      this.loading.set(false);
    }
  }

  async saveClaims(userId: string, claims: UserClaimRequest[], removedClaims: string[] = []): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.put<void>(`${this.baseUrl}/users/${userId}/claims`, {
          claims,
          removedClaims: removedClaims.length ? removedClaims : null
        } as UpdateUserClaimsRequest)
      );
      this.claims.set(claims);
    } catch (error) {
      this.error.set('Unable to update claims.');
    } finally {
      this.loading.set(false);
    }
  }

  async removeClaim(userId: string, claimType: string, claimValue: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const url = `${this.baseUrl}/users/${userId}/claims/${encodeURIComponent(claimType)}/${encodeURIComponent(claimValue)}`;
      await lastValueFrom(this.http.delete<void>(url));
      await this.loadClaims(userId);
    } catch (error) {
      this.error.set('Unable to remove the claim.');
    } finally {
      this.loading.set(false);
    }
  }

  // ------------------------------------------------------------------
  // Roles
  // ------------------------------------------------------------------

  async loadAvailableRoles(): Promise<void> {
    try {
      const result = await lastValueFrom(
        this.http.get<PagedResponse<Role>>(`${this.baseUrl}/roles`, {
          params: new HttpParams().set('page', '1').set('pageSize', '100')
        })
      );
      this.availableRoles.set(result.items ?? []);
    } catch (error) {
      this.error.set('Unable to load roles.');
    }
  }

  async loadUserRoles(userId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const response = await lastValueFrom(
        this.http.get<{ roles: string[] }>(`${this.baseUrl}/users/${userId}/roles`)
      );
      this.userRoles.set(response.roles ?? []);
    } catch (error) {
      this.error.set('Unable to load user roles.');
    } finally {
      this.loading.set(false);
    }
  }

  async setUserRoles(userId: string, roleNames: string[]): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.put<void>(`${this.baseUrl}/users/${userId}/roles`, { roleNames } as UpdateUserRolesRequest)
      );
      this.userRoles.set(roleNames);
    } catch (error) {
      this.error.set('Unable to update roles.');
    } finally {
      this.loading.set(false);
    }
  }

  // ------------------------------------------------------------------
  // Security & status
  // ------------------------------------------------------------------

  async lockUser(userId: string, lockoutEnd?: string | null): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.post<void>(`${this.baseUrl}/users/${userId}/lock`, { lockoutEnd } as LockUserRequest)
      );
      await this.loadUser(userId);
    } catch (error) {
      this.error.set('Unable to lock the account.');
    } finally {
      this.loading.set(false);
    }
  }

  async unlockUser(userId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(this.http.post<void>(`${this.baseUrl}/users/${userId}/unlock`, null));
      await this.loadUser(userId);
    } catch (error) {
      this.error.set('Unable to unlock the account.');
    } finally {
      this.loading.set(false);
    }
  }

  async activateUser(userId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(this.http.post<void>(`${this.baseUrl}/users/${userId}/activate`, null));
      await this.loadUser(userId);
    } catch (error) {
      this.error.set('Unable to activate the account.');
    } finally {
      this.loading.set(false);
    }
  }

  async deactivateUser(userId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(this.http.post<void>(`${this.baseUrl}/users/${userId}/deactivate`, null));
      await this.loadUser(userId);
    } catch (error) {
      this.error.set('Unable to deactivate the account.');
    } finally {
      this.loading.set(false);
    }
  }

  async updateUserPassword(userId: string, newPassword: string): Promise<boolean> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.put<void>(`${this.baseUrl}/users/${userId}/password`, { newPassword } as UpdateUserPasswordRequest)
      );
      return true;
    } catch (error) {
      this.error.set('Unable to update the password. Make sure it meets the password policy.');
      return false;
    } finally {
      this.loading.set(false);
    }
  }
}
