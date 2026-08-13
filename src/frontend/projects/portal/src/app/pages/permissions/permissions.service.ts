import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermissionMatrix } from './permissions.model';

export interface RolePermissionUpdate {
  permissionName: string;
  isGranted: boolean;
}

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/identity`;

  readonly matrix = signal<PermissionMatrix | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async loadMatrix(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      this.matrix.set(await lastValueFrom(this.http.get<PermissionMatrix>(`${this.baseUrl}/permissions/matrix`)));
    } catch (error) {
      this.error.set('Unable to load the permission matrix.');
    } finally {
      this.loading.set(false);
    }
  }

  /** Grants/revokes catalog permissions on a role (custom claims are preserved server-side). */
  async saveRolePermissions(roleId: string, permissions: RolePermissionUpdate[]): Promise<boolean> {
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.put<void>(`${this.baseUrl}/roles/${roleId}/permissions`, { permissions })
      );
      return true;
    } catch (error) {
      this.error.set('Unable to save role permissions.');
      return false;
    }
  }
}
