import { RouterLink } from '@angular/router';
import { UsersService } from './users.service';
import { CreateUserRequest, UpdateUserRequest, User } from './users.model';
import { UserEditorComponent } from './user-editor.component';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { Component, computed, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../core/components/page/page-header.component';

@Component({
  selector: 'app-users-page',
  standalone: true,
  templateUrl: './users-page.component.html',
  imports: [RouterLink, UserEditorComponent, UiButtonComponent, UiIconComponent, PageHeaderComponent],
})
export class UsersPageComponent {
  protected readonly service = inject(UsersService);
  protected readonly editorOpen = signal(false);
  protected readonly editingUser = signal<User | null>(null);
  protected readonly editorMode = computed(() => (this.editingUser() ? 'edit' : 'create'));
  protected readonly editorTitle = computed(() => (this.editingUser() ? `Edit ${this.editingUser()?.username}` : 'Create a new user'));

  constructor() {
    this.service.loadUsers();
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

  protected async saveUser(payload: CreateUserRequest | UpdateUserRequest): Promise<void> {
    if (this.editingUser()) {
      const user = this.editingUser();
      if (user) {
        await this.service.updateUser(user.id, payload as UpdateUserRequest);
      }
    } else {
      await this.service.createUser(payload as CreateUserRequest);
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

  protected previousPage(): void {
    this.service.page.update((current) => Math.max(1, current - 1));
    this.service.loadUsers();
  }

  protected nextPage(): void {
    this.service.page.update((current) => Math.min(this.service.pageCount(), current + 1));
    this.service.loadUsers();
  }

  protected setPageSize(value: string): void {
    this.service.pageSize.set(Number(value));
    this.service.loadUsers();
  }
}
