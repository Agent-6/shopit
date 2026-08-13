import { RouterLink } from '@angular/router';
import { UsersService } from './users.service';
import { InviteUserRequest, UpdateUserRequest, User } from './users.model';
import { UserEditorComponent } from './user-editor.component';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { Component, computed, inject, signal } from '@angular/core';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';
import { PageHeaderComponent } from '../../core/components/page/page-header.component';
import { PageFiltersComponent } from '../../core/components/page/page-filters.component';
import {
  PageTableCellDirective,
  PageTableColumn,
  PageTableComponent,
} from '../../core/components/page/page-table.component';

@Component({
  selector: 'app-users-page',
  standalone: true,
  templateUrl: './users-page.component.html',
  imports: [
    RouterLink,
    UserEditorComponent,
    UiButtonComponent,
    UiIconComponent,
    PageHeaderComponent,
    PageFiltersComponent,
    PageTableComponent,
    PageTableCellDirective,
  ],
})
export class UsersPageComponent {
  protected readonly service = inject(UsersService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;
  protected readonly editorOpen = signal(false);
  protected readonly editingUser = signal<User | null>(null);
  protected readonly editorMode = computed(() => (this.editingUser() ? 'edit' : 'create'));
  protected readonly editorTitle = computed(() =>
    this.editingUser() ? `Edit ${this.editingUser()?.username}` : 'Invite a new user'
  );

  protected readonly columns: PageTableColumn[] = [
    { key: 'username', header: 'Username' },
    { key: 'email', header: 'Email' },
    { key: 'status', header: 'Status' },
    { key: 'roles', header: 'Roles' },
    { key: 'isActive', header: 'Active' },
    { key: 'actions', header: 'Actions', align: 'right' },
  ];

  constructor() {
    this.service.loadUsers();
  }

  protected trackById(_index: number, user: User): string {
    return user.id;
  }

  protected isLocked(user: User): boolean {
    return !!user.lockoutEnd && new Date(user.lockoutEnd).getTime() > Date.now();
  }

  protected openCreate(): void {
    this.editingUser.set(null);
    this.editorOpen.set(true);
  }

  protected editUser(user: User): void {
    this.editingUser.set(user);
    this.editorOpen.set(true);
  }

  protected closeEditor(): void {
    this.editorOpen.set(false);
  }

  protected async saveUser(payload: InviteUserRequest | UpdateUserRequest): Promise<void> {
    if (this.editingUser()) {
      const user = this.editingUser();
      if (user) {
        await this.service.updateUser(user.id, payload as UpdateUserRequest);
      }
    } else {
      await this.service.inviteUser(payload as InviteUserRequest);
    }
    this.closeEditor();
  }

  protected deleteUser(user: User): void {
    const confirmed = confirm(`Delete ${user.username}? This can be permanent.`);
    if (!confirmed) {
      return;
    }

    this.service.removeUser(user.id, true);
  }

  protected goToPage(page: number): void {
    this.service.page.set(Math.min(Math.max(1, page), this.service.pageCount()));
    this.service.loadUsers();
  }

  protected setPageSize(size: number): void {
    this.service.pageSize.set(size);
    this.service.loadUsers();
  }
}
