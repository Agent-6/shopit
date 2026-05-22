import { RouterLink } from '@angular/router';
import { UsersService } from './users.service';
import { CreateUserRequest, UpdateUserRequest, User } from './users.model';
import { UserEditorComponent } from './user-editor.component';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { Component, computed, inject, signal } from '@angular/core';

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [RouterLink, UserEditorComponent, UiButtonComponent, UiIconComponent],
  template: `
    <section class="space-y-6">
      <header class="flex flex-col gap-4 bg-card text-card-foreground md:flex-row md:items-center md:justify-between">
        <div>
          <h1 class="text-2xl font-semibold tracking-tight">Users</h1>
          <p class="mt-2 text-sm text-muted-foreground">Browse, create, and maintain user accounts for the Identity API.</p>
        </div>
        <div class="self-start flex flex-row gap-4 justify-between">
          <ui-button variant="default" icon="plus" (click)="openCreate()">Create user</ui-button>
        </div>
      </header>

      <div class="rounded-md border bg-card text-card-foreground p-6 shadow-sm">
        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-12 items-end">
          <div class="lg:col-span-5 space-y-1.5">
            <label class="text-sm font-medium leading-none">Search input</label>
            <input
              type="search"
              placeholder="Search by username, email, or role"
              class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              [value]="service.filter()"
              (input)="service.filter.set($any($event.target).value)"
            />
          </div>

          <div class="lg:col-span-2 space-y-1.5">
            <label class="text-sm font-medium leading-none">Sort by</label>
            <select class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50" [value]="service.sortBy()" (change)="service.sortBy.set($any($event.target).value)">
              <option value="username">Username</option>
              <option value="email">Email</option>
              <option value="isActive">Active</option>
            </select>
          </div>

          <div class="lg:col-span-2 space-y-1.5">
            <label class="text-sm font-medium leading-none">Order</label>
            <select class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50" [value]="service.sortOrder()" (change)="service.sortOrder.set($any($event.target).value)">
              <option value="asc">Ascending</option>
              <option value="desc">Descending</option>
            </select>
          </div>

          <div class="lg:col-span-2 space-y-1.5">
            <label class="text-sm font-medium leading-none">Size</label>
            <select class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50" [value]="service.pageSize()" (change)="setPageSize($any($event.target).value)">
              <option [value]="5">5</option>
              <option [value]="10">10</option>
              <option [value]="20">20</option>
            </select>
          </div>

          <div class="lg:col-span-full flex justify-end w-fit">
            <ui-button variant="outline" icon="refresh-cw" (click)="service.loadUsers()" class="w-full">Refresh</ui-button>
          </div>
        </div>
      </div>

      <div class="overflow-y-auto rounded-md border bg-card text-card-foreground shadow-sm">
        <table class="w-full text-sm">
          <thead class="border-b bg-muted/50">
            <tr class="text-left font-medium text-muted-foreground">
              <th class="h-12 px-4 align-middle font-medium">Username</th>
              <th class="h-12 px-4 align-middle font-medium">Email</th>
              <th class="h-12 px-4 align-middle font-medium">Roles</th>
              <th class="h-12 px-4 align-middle font-medium">Active</th>
              <th class="h-12 px-4 align-middle font-medium text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="[&_tr:last-child]:border-0">
            @for (user of service.users(); track user.id) {
              <tr class="border-b transition-colors hover:bg-muted/50 data-[state=selected]:bg-muted">
                <td class="p-4 align-middle font-medium">{{ user.username }}</td>
                <td class="p-4 align-middle text-muted-foreground">{{ user.email }}</td>
                <td class="p-4 align-middle">
                  @if (user.roles?.length) {
                    <div class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2">
                      {{ user.roles?.join(', ') || '—' }}
                    </div>
                  }
                </td>
                <td class="p-4 align-middle">
                  @if (user.isActive) {
                    <span class="text-primary"><ui-icon name="check" class="h-4 w-4"></ui-icon></span>
                  }
                  @if (!user.isActive) {
                    <span class="text-muted-foreground"><ui-icon name="x" class="h-4 w-4"></ui-icon></span>
                  }
                </td>
                <td class="p-4 align-middle text-right flex justify-end gap-2">
                  <ui-button variant="outline" size="sm" routerLink="/users/{{ user.id }}">View</ui-button>
                  <ui-button variant="secondary" size="sm" (click)="editUser(user)">Edit</ui-button>
                  <ui-button variant="destructive" size="sm" (click)="deleteUser(user)">Delete</ui-button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <div class="flex items-center justify-between text-sm text-muted-foreground">
        <div>Page {{ service.page() }} of {{ service.pageCount() }}</div>
        <div class="flex items-center gap-2">
          <ui-button variant="outline" size="sm" (click)="previousPage()" [disabled]="service.page() <= 1">Previous</ui-button>
          <ui-button variant="outline" size="sm" (click)="nextPage()" [disabled]="service.page() >= service.pageCount()">Next</ui-button>
        </div>
      </div>

      @if (!service.loading() && service.users().length === 0) {
        <div class="rounded-xl border bg-card p-6 text-sm text-muted-foreground text-center">
          No users found. Click <span class="font-medium text-foreground">Create user</span> to add an account.
        </div>
      }

      @if (service.error()) {
        <div class="rounded-xl border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          <ui-icon name="alert-circle" class="inline-block mr-2 h-4 w-4"></ui-icon>
          {{ service.error() }}
        </div>
      }

      @if (editorOpen()) {
        <aside class="fixed inset-0 z-50 flex items-start justify-center pt-16 bg-background/80 backdrop-blur-sm sm:items-center sm:pt-0">
          <div class="w-full max-w-lg rounded-xl border bg-card p-6 shadow-lg">
          <app-user-editor
            [title]="editorTitle()"
            [submitLabel]="editorMode() === 'create' ? 'Create user' : 'Save changes'"
            [mode]="editorMode()"
            [model]="editingUser()"
            (submit)="saveUser($event)"
            (cancel)="closeEditor()"
          ></app-user-editor>
          </div>
        </aside>
      }
    </section>
  `
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
