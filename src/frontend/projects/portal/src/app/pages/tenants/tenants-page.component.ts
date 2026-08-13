import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../core/components/page/page-header.component';
import { PageFiltersComponent } from '../../core/components/page/page-filters.component';
import {
  PageTableCellDirective,
  PageTableColumn,
  PageTableComponent,
} from '../../core/components/page/page-table.component';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';
import { TenantEditorComponent } from './tenant-editor.component';
import { CreateTenantRequest, Tenant, UpdateTenantRequest } from './tenant.model';
import { TenantService } from './tenant.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-tenants-page',
  standalone: true,
  templateUrl: './tenants-page.component.html',
  imports: [
    DatePipe,
    RouterLink,
    TenantEditorComponent,
    UiButtonComponent,
    UiIconComponent,
    PageHeaderComponent,
    PageFiltersComponent,
    PageTableComponent,
    PageTableCellDirective,
  ],
})
export class TenantsPageComponent {
  protected readonly service = inject(TenantService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;
  protected readonly editorOpen = signal(false);
  protected readonly editingTenant = signal<Tenant | null>(null);
  protected readonly editorMode = computed(() => (this.editingTenant() ? 'edit' : 'create'));
  protected readonly editorTitle = computed(() =>
    this.editingTenant() ? `Edit ${this.editingTenant()?.name}` : 'Create a new tenant'
  );
  protected readonly pageCount = computed(() => Math.max(1, this.service.totalPages()));

  protected readonly columns: PageTableColumn[] = [
    { key: 'name', header: 'Name' },
    { key: 'isActive', header: 'Status' },
    { key: 'createdOn', header: 'Created' },
    { key: 'lastModifiedOn', header: 'Updated' },
    { key: 'actions', header: 'Actions', align: 'right' },
  ];

  constructor() {
    this.service.loadTenants();
  }

  protected trackById(_index: number, tenant: Tenant): string {
    return tenant.id;
  }

  protected openCreate(): void {
    this.editingTenant.set(null);
    this.editorOpen.set(true);
  }

  protected editTenant(tenant: Tenant): void {
    this.editingTenant.set(tenant);
    this.editorOpen.set(true);
  }

  protected closeEditor(): void {
    this.editorOpen.set(false);
  }

  protected async saveTenant(payload: CreateTenantRequest | UpdateTenantRequest): Promise<void> {
    if (this.editingTenant()) {
      const tenant = this.editingTenant();
      if (tenant) {
        this.service.updateTenant(tenant.id, payload.name);
      }
    } else {
      this.service.createTenant(payload.name);
    }

    this.closeEditor();
  }

  protected activateTenant(tenant: Tenant): void {
    this.service.activateTenant(tenant.id);
  }

  protected deactivateTenant(tenant: Tenant): void {
    this.service.deactivateTenant(tenant.id);
  }

  protected goToPage(page: number): void {
    this.service.page.set(Math.min(Math.max(1, page), this.pageCount()));
    this.service.loadTenants();
  }

  protected setPageSize(size: number): void {
    this.service.pageSize.set(size);
    this.service.loadTenants();
  }
}
