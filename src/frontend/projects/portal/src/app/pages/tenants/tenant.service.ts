import { HttpClient } from "@angular/common/http";
import { inject, Injectable, signal } from "@angular/core";
import { environment } from "../../../environments/environment";
import { Tenant } from "./tenant.model";
import { PagedResponse } from "../../core/models/pagination";

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenancy/tenants`;

  readonly tenants = signal<Tenant[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedTenant = signal<Tenant | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(10)
  readonly totalCount = signal(0);
  readonly totalPages = signal(1);

  loadTenants(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<PagedResponse<Tenant>>(this.baseUrl, {params: { pageNumber: this.page(), pageSize: this.pageSize() } })
      .subscribe({
        next: (result) => {
          this.tenants.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
        },
        error: (err) => {
          this.error.set(err.message);
        },
        complete: () => {
          this.loading.set(false);
        }
      });
  }

  loadTenant(tenantId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<Tenant>(`${this.baseUrl}/${tenantId}`)
      .subscribe({
        next: (tenant) => {
          this.selectedTenant.set(tenant);
        },
        error: (err) => {
          this.error.set(err.message);
        },
        complete: () => {
          this.loading.set(false);
        }
      });
  }

  createTenant(name: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.post<Tenant>(this.baseUrl, { name })
      .subscribe({
        next: () => {
          this.loadTenants();
        },
        error: (err) => {
          this.error.set(err.message);
        },
        complete: () => {
          this.loading.set(false);
        }
      });
  }

  updateTenant(tenantId: string, name: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.put(`${this.baseUrl}/${tenantId}`, { name })
      .subscribe({
        next: () => {
          this.loadTenants();
        },
        error: (err) => {
          this.error.set(err.message);
        },
        complete: () => {
          this.loading.set(false);
        }
      });
  }

  activateTenant(tenantId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.put(`${this.baseUrl}/${tenantId}/activate`, {})
      .subscribe({
        next: () => {
          this.loadTenants();
        },
        error: (err) => {
          this.error.set(err.message);
        },
        complete: () => {
          this.loading.set(false);
        }
      });
  }

  deactivateTenant(tenantId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.put(`${this.baseUrl}/${tenantId}/deactivate`, {})
      .subscribe({
        next: () => {
          this.loadTenants();
        },
        error: (err) => {
          this.error.set(err.message);
        },
        complete: () => {
          this.loading.set(false);
        }
      });
  }
}
