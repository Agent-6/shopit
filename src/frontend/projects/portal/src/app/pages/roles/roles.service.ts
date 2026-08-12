import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal, computed } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { CreateRoleRequest, Role, RoleClaimRequest, RoleDetail, UpdateRoleRequest } from './role.model';
import { environment } from '../../../environments/environment';
import { PagedResponse } from '../../core/models/pagination';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/identity/roles`;

  readonly roles = signal<Role[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedRole = signal<RoleDetail | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly filter = signal('');
  readonly totalCount = signal(0);
  readonly totalPages = signal(1);

  readonly queryParams = computed(() => {
    let params = new HttpParams()
      .set('page', String(this.page()))
      .set('pageSize', String(this.pageSize()));

    if (this.filter().trim()) {
      params = params.set('filter', this.filter().trim());
    }

    return params;
  });

  async loadRoles(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const result = await lastValueFrom(
        this.http.get<PagedResponse<Role>>(`${this.baseUrl}`, { params: this.queryParams() })
      );

      this.roles.set(result.items ?? []);
      this.totalCount.set(result.totalCount ?? 0);
      this.totalPages.set(result.totalPages ?? 1);
    } catch (error) {
      this.error.set('Unable to load roles. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  async loadRole(roleId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const role = await lastValueFrom(this.http.get<RoleDetail>(`${this.baseUrl}/${roleId}`));
      this.selectedRole.set(role);
    } catch (error) {
      this.error.set('Unable to load role details.');
    } finally {
      this.loading.set(false);
    }
  }

  async createRole(payload: CreateRoleRequest): Promise<Role | null> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const created = await lastValueFrom(this.http.post<Role>(`${this.baseUrl}`, payload));
      await this.loadRoles();
      return created;
    } catch (error) {
      this.error.set('Unable to create the role. Please validate your input.');
      return null;
    } finally {
      this.loading.set(false);
    }
  }

  async updateRole(roleId: string, payload: UpdateRoleRequest): Promise<Role | null> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const updated = await lastValueFrom(this.http.put<Role>(`${this.baseUrl}/${roleId}`, payload));
      await this.loadRoles();
      return updated;
    } catch (error) {
      this.error.set('Unable to save role changes.');
      return null;
    } finally {
      this.loading.set(false);
    }
  }

  async deleteRole(roleId: string): Promise<boolean> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(this.http.delete(`${this.baseUrl}/${roleId}`));
      await this.loadRoles();
      return true;
    } catch (error) {
      this.error.set('Unable to delete the role.');
      return false;
    } finally {
      this.loading.set(false);
    }
  }

  async saveRoleClaims(roleId: string, claims: RoleClaimRequest[]): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.put<void>(`${this.baseUrl}/${roleId}/claims`, { claims } as { claims: RoleClaimRequest[] })
      );
    } catch (error) {
      this.error.set('Unable to update role claims.');
    } finally {
      this.loading.set(false);
    }
  }
}
