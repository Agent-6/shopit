import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UsersService } from './users.service';
import { CreateUserRequest, UpdateUserRequest, User } from './users.model';
import { UserEditorComponent } from './user-editor.component';

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [CommonModule, RouterLink, UserEditorComponent],
  template: `
    <section class="space-y-8">
      <header class="flex flex-col gap-4 rounded-3xl border border-slate-200 bg-white p-6 shadow-sm md:flex-row md:items-center md:justify-between">
        <div>
          <h1 class="text-3xl font-semibold text-slate-900">Users</h1>
          <p class="mt-2 text-sm text-slate-500">Browse, create, and maintain user accounts for the Identity API.</p>
        </div>
        <button type="button" class="inline-flex items-center justify-center rounded-full bg-sky-600 px-5 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-sky-700" (click)="openCreate()">
          Create user
        </button>
      </header>

      <div class="grid gap-4 rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-[minmax(240px,320px)_minmax(180px,240px)]">
          <input
            type="search"
            placeholder="Search by username, email, or role"
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
            [value]="service.filter()"
            (input)="service.filter.set($any($event.target).value)"
          />

          <label class="grid gap-2 text-sm text-slate-600">
            Sort by
            <select class="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 outline-none" [value]="service.sortBy()" (change)="service.sortBy.set($any($event.target).value)">
              <option value="username">Username</option>
              <option value="email">Email</option>
              <option value="isActive">Active</option>
            </select>
          </label>

          <label class="grid gap-2 text-sm text-slate-600">
            Order
            <select class="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 outline-none" [value]="service.sortOrder()" (change)="service.sortOrder.set($any($event.target).value)">
              <option value="asc">Asc</option>
              <option value="desc">Desc</option>
            </select>
          </label>

          <label class="grid gap-2 text-sm text-slate-600">
            Page size
            <select class="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 outline-none" [value]="service.pageSize()" (change)="setPageSize($any($event.target).value)">
              <option [value]="5">5</option>
              <option [value]="10">10</option>
              <option [value]="20">20</option>
            </select>
          </label>

          <button type="button" class="rounded-2xl border border-slate-300 bg-slate-100 px-4 py-3 text-sm font-semibold text-slate-700 transition hover:bg-slate-200" (click)="service.loadUsers()">
            Refresh
          </button>
        </div>
      </div>

      <div class="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <table class="min-w-full border-separate border-spacing-0 text-sm text-slate-700">
          <thead class="bg-slate-50 text-slate-500">
            <tr>
              <th class="px-6 py-4 text-left font-semibold">Username</th>
              <th class="px-6 py-4 text-left font-semibold">Email</th>
              <th class="px-6 py-4 text-left font-semibold">Roles</th>
              <th class="px-6 py-4 text-left font-semibold">Active</th>
              <th class="px-6 py-4 text-left font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-200 bg-white">
            @for (user of service.users(); track user.id) {
              <tr class="hover:bg-slate-50">
                <td class="px-6 py-4">{{ user.username }}</td>
                <td class="px-6 py-4">{{ user.email }}</td>
                <td class="px-6 py-4">{{ user.roles?.join(', ') || '—' }}</td>
                <td class="px-6 py-4">{{ user.isActive ? 'Yes' : 'No' }}</td>
                <td class="flex flex-wrap gap-2 px-6 py-4">
                  <a class="rounded-full px-3 py-2 text-sm font-medium text-sky-700 transition hover:bg-slate-100" [routerLink]="['/users', user.id]">View</a>
                  <button type="button" class="rounded-full bg-slate-100 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-200" (click)="editUser(user)">Edit</button>
                  <button type="button" class="rounded-full bg-rose-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-rose-700" (click)="deleteUser(user)">Delete</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <div class="flex flex-col gap-3 rounded-3xl border border-slate-200 bg-white p-6 text-sm text-slate-600 shadow-sm md:flex-row md:items-center md:justify-between">
        <span>Page {{ service.page() }} of {{ service.pageCount() }}</span>
        <div class="flex items-center gap-2">
          <button type="button" class="rounded-full border border-slate-300 bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700 disabled:cursor-not-allowed disabled:opacity-50" (click)="previousPage()" [disabled]="service.page() <= 1">Previous</button>
          <button type="button" class="rounded-full border border-slate-300 bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700 disabled:cursor-not-allowed disabled:opacity-50" (click)="nextPage()" [disabled]="service.page() >= service.pageCount()">Next</button>
        </div>
      </div>

      <div *ngIf="!service.loading() && service.users().length === 0" class="rounded-3xl border border-slate-200 bg-slate-50 p-6 text-sm text-slate-600">
        No users found. Click Create user to add the first account.
      </div>

      <div *ngIf="service.error()" class="rounded-3xl border border-rose-200 bg-rose-50 p-6 text-sm text-rose-700">
        {{ service.error() }}
      </div>

      <aside *ngIf="editorOpen()" class="sticky top-8 rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-100 md:max-w-md">
        <app-user-editor
          [title]="editorTitle()"
          [submitLabel]="editorMode() === 'create' ? 'Create user' : 'Save changes'"
          [mode]="editorMode()"
          [model]="editingUser()"
          (submit)="saveUser($event)"
          (cancel)="closeEditor()"
        ></app-user-editor>
      </aside>
    </section>

    <ng-template #loadingState>
      <div class="rounded-3xl border border-slate-200 bg-slate-50 p-6 text-slate-700 shadow-sm">Loading users…</div>
    </ng-template>
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
