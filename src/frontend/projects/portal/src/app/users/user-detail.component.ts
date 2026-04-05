import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UsersService } from './users.service';
import { UserClaimRequest, UserPermissionRequest, UpdateUserRequest } from './users.model';
import { UserEditorComponent } from './user-editor.component';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, UserEditorComponent],
  template: `
    <div class="space-y-8">
      <header class="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <div class="flex flex-col gap-2">
          <div class="flex flex-wrap items-center gap-3 text-sm text-slate-500">
            <a class="font-semibold text-sky-700 transition hover:text-sky-900" routerLink="/users">← Back to users</a>
            <span>User details</span>
          </div>
          <h1 class="text-3xl font-semibold text-slate-900">{{ userName() }}</h1>
          <p class="text-sm text-slate-500">{{ selectedUser()?.email ?? 'Loading user details…' }}</p>
        </div>
      </header>

      <section *ngIf="selectedUser(); else loadingState" class="grid gap-6 lg:grid-cols-[1.5fr_1fr]">
        <div class="space-y-6 rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <app-user-editor
            [title]="'Edit ' + (selectedUser()?.username ?? 'user')"
            [submitLabel]="'Save profile'"
            [mode]="'edit'"
            [model]="selectedUser()"
            (submit)="handleProfileSave($event)"
            (cancel)="router.navigate(['/users'])"
          ></app-user-editor>
        </div>

        <div class="space-y-6">
          <section class="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 class="text-xl font-semibold text-slate-900">Permissions</h2>
            <p class="mt-2 text-sm text-slate-500">Update grant flags for the selected user.</p>

            <div class="mt-6 space-y-3">
              <div *ngFor="let permission of permissions(); let i = index" class="flex items-center justify-between gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <div>
                  <p class="font-medium text-slate-900">{{ permission.permissionName }}</p>
                </div>
                <label class="inline-flex items-center gap-3 text-sm text-slate-500">
                  <span>Granted</span>
                  <input
                    type="checkbox"
                    class="h-5 w-5 rounded border-slate-300 text-sky-600 focus:ring-sky-500"
                    [checked]="permission.isGranted"
                    (change)="togglePermission(i, $any($event.target).checked)"
                  />
                </label>
              </div>
            </div>

            <button type="button" class="mt-6 inline-flex items-center justify-center rounded-full bg-sky-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-sky-700" (click)="savePermissions()">Save permissions</button>
          </section>

          <section class="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 class="text-xl font-semibold text-slate-900">Claims</h2>
            <p class="mt-2 text-sm text-slate-500">Add, edit, or remove claim values.</p>

            <div class="mt-6 space-y-4">
              <div *ngFor="let claim of claims(); let i = index" class="grid gap-3 md:grid-cols-[1fr_1fr_auto]">
                <input
                  type="text"
                  class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
                  [value]="claim.claimType"
                  (input)="updateClaim(i, 'claimType', $any($event.target).value)"
                  placeholder="Claim type"
                />
                <input
                  type="text"
                  class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
                  [value]="claim.claimValue"
                  (input)="updateClaim(i, 'claimValue', $any($event.target).value)"
                  placeholder="Claim value"
                />
                <button type="button" class="rounded-full bg-rose-100 px-4 py-3 text-sm font-semibold text-rose-700 transition hover:bg-rose-200" (click)="removeClaim(i)">Remove</button>
              </div>
            </div>

            <div class="mt-4 flex flex-wrap gap-3">
              <button type="button" class="rounded-full bg-slate-100 px-4 py-3 text-sm font-semibold text-slate-700 transition hover:bg-slate-200" (click)="addClaim()">Add claim</button>
              <button type="button" class="rounded-full bg-sky-600 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-700" (click)="saveClaims()">Save claims</button>
            </div>
          </section>
        </div>
      </section>

      <ng-template #loadingState>
        <div class="rounded-3xl border border-slate-200 bg-white p-6 text-slate-600 shadow-sm">Loading user details...</div>
      </ng-template>
    </div>
  `
})
export class UserDetailComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly service = inject(UsersService);

  protected readonly selectedTab = signal<'profile' | 'permissions' | 'claims'>('profile');
  protected readonly permissions = signal<UserPermissionRequest[]>([]);
  protected readonly claims = signal<UserClaimRequest[]>([]);

  protected readonly selectedUser = this.service.selectedUser;
  protected readonly userName = computed(() => this.selectedUser()?.username ?? 'User details');

  constructor() {
    const userId = this.route.snapshot.params['id'] as string;
    if (userId) {
      this.loadUser(userId);
    }
  }

  protected async loadUser(userId: string): Promise<void> {
    await this.service.loadUser(userId);
    await this.service.loadPermissions(userId);
    await this.service.loadClaims(userId);
    this.permissions.set(this.service.permissions());
    this.claims.set(this.service.claims());
  }

  protected async handleProfileSave(value: UpdateUserRequest): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.updateUser(user.id, value);
  }

  protected togglePermission(index: number, isGranted: boolean): void {
    this.permissions.update((current) => {
      const next = [...current];
      next[index] = { ...next[index], isGranted };
      return next;
    });
  }

  protected async savePermissions(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.savePermissions(user.id, this.permissions());
  }

  protected addClaim(): void {
    this.claims.update((current) => [...current, { claimType: '', claimValue: '' }]);
  }

  protected updateClaim(index: number, field: keyof UserClaimRequest, value: string): void {
    this.claims.update((current) => {
      const next = [...current];
      next[index] = { ...next[index], [field]: value };
      return next;
    });
  }

  protected removeClaim(index: number): void {
    this.claims.update((current) => current.filter((_, i) => i !== index));
  }

  protected async saveClaims(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.saveClaims(user.id, this.claims(), []);
  }
}
