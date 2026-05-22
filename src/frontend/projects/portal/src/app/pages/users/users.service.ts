import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal, computed } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import {
  CreateUserRequest,
  DeleteUserResponse,
  PagedResult,
  UpdateUserClaimsRequest,
  UpdateUserPermissionsRequest,
  UpdateUserRequest,
  User,
  UserClaimRequest,
  UserPermissionRequest
} from './users.model';
import { environment } from '../../../environments/environment';

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
        this.http.get<User[] | PagedResult<User>>(`${this.baseUrl}/users`, {
          params: this.queryParams()
        })
      );

      if (Array.isArray(result)) {
        this.users.set(result);
        this.totalCount.set(result.length);
      } else {
        this.users.set(result.users ?? []);
        this.totalCount.set(result.totalCount ?? result.users?.length ?? 0);
      }
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
}
