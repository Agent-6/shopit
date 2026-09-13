import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
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
import { RoleEditorComponent } from './role-editor.component';
import { CreateRoleRequest, Role, UpdateRoleRequest } from './role.model';
import { RolesService } from './roles.service';

@Component({
  selector: 'app-roles-page',
  standalone: true,
  templateUrl: './roles-page.component.html',
  imports: [
    DatePipe,
    RouterLink,
    RoleEditorComponent,
    UiButtonComponent,
    UiIconComponent,
    PageHeaderComponent,
    PageFiltersComponent,
    PageTableComponent,
    PageTableCellDirective,
  ],
})
export class RolesPageComponent {
  protected readonly service = inject(RolesService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;
  protected readonly editorOpen = signal(false);
  protected readonly editingRole = signal<Role | null>(null);
  protected readonly editorMode = computed(() => (this.editingRole() ? 'edit' : 'create'));
  protected readonly editorTitle = computed(() =>
    this.editingRole() ? `Edit ${this.editingRole()?.name}` : 'Create a new role'
  );
  protected readonly pageCount = computed(() => Math.max(1, this.service.totalPages()));

  protected readonly columns: PageTableColumn[] = [
    { key: 'name', header: 'Name' },
    { key: 'side', header: 'Side' },
    { key: 'description', header: 'Description' },
    { key: 'createdAt', header: 'Created' },
    { key: 'actions', header: 'Actions', align: 'right' },
  ];

  protected sideBadgeClass(side: string | undefined): string {
    switch (side) {
      case 'Host':
        return 'bg-indigo-100 text-indigo-700';
      case 'Tenant':
        return 'bg-emerald-100 text-emerald-700';
      default:
        return 'bg-muted text-muted-foreground';
    }
  }

  protected sideLabel(side: string | undefined): string {
    switch (side) {
      case 'Host':
        return 'Host only';
      case 'Tenant':
        return 'Tenant only';
      default:
        return 'Both';
    }
  }

  constructor() {
    this.service.loadRoles();
  }

  protected trackById(_index: number, role: Role): string {
    return role.id;
  }

  protected openCreate(): void {
    this.editingRole.set(null);
    this.editorOpen.set(true);
  }

  protected editRole(role: Role): void {
    this.editingRole.set(role);
    this.editorOpen.set(true);
  }

  protected closeEditor(): void {
    this.editorOpen.set(false);
  }

  protected async saveRole(payload: CreateRoleRequest | UpdateRoleRequest): Promise<void> {
    if (this.editingRole()) {
      const role = this.editingRole();
      if (role) {
        await this.service.updateRole(role.id, payload as UpdateRoleRequest);
      }
    } else {
      await this.service.createRole(payload as CreateRoleRequest);
    }

    this.closeEditor();
  }

  protected deleteRole(role: Role): void {
    const confirmed = confirm(`Delete role "${role.name}"? Users assigned this role will lose its permissions.`);
    if (!confirmed) {
      return;
    }

    this.service.deleteRole(role.id);
  }

  protected goToPage(page: number): void {
    this.service.page.set(Math.min(Math.max(1, page), this.pageCount()));
    this.service.loadRoles();
  }

  protected setPageSize(size: number): void {
    this.service.pageSize.set(size);
    this.service.loadRoles();
  }
}
