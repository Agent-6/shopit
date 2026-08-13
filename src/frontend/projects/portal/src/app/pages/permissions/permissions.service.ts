import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermissionMatrix } from './permissions.model';

export interface RoleClaimPayload {
  claimType: string;
  claimValue: string;
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

  /** Saves a role's full claim list (permission claims + preserved custom claims). */
  async saveRoleClaims(roleId: string, claims: RoleClaimPayload[]): Promise<boolean> {
    this.error.set(null);

    try {
      await lastValueFrom(
        this.http.put<void>(`${this.baseUrl}/roles/${roleId}/claims`, { claims })
      );
      return true;
    } catch (error) {
      this.error.set('Unable to save role permissions.');
      return false;
    }
  }
}
