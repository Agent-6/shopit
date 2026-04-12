import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UsersService } from './users.service';
import { UserClaimRequest, UserPermissionRequest, UpdateUserRequest } from './users.model';
import { UserEditorComponent } from './user-editor.component';
import { Component, computed, inject, signal } from '@angular/core';
import { UiButtonComponent } from '../shared/components/ui-button.component';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [RouterLink, UserEditorComponent, UiButtonComponent],
  template: `
    <div class="space-y-6">
      <header class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
        <div class="flex flex-col gap-2">
          <div class="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
            <a class="font-semibold text-primary transition-colors hover:text-primary/80" routerLink="/users">← Back to users</a>
            <span>User details</span>
          </div>
          <h1 class="text-3xl font-semibold tracking-tight">{{ userName() }}</h1>
          <p class="text-sm text-muted-foreground">{{ selectedUser()?.email ?? 'Loading user details…' }}</p>
        </div>
      </header>

      @if (selectedUser()) {
        <section class="grid gap-6 lg:grid-cols-[1.5fr_1fr]">
          <div class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
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
          <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
            <h2 class="text-xl font-semibold tracking-tight">Permissions</h2>
            <p class="mt-2 text-sm text-muted-foreground">Update grant flags for the selected user.</p>

            <div class="mt-6 space-y-3">
              @for (permission of permissions(); track permission; let i = $index) {
                <div class="flex items-center justify-between gap-3 rounded-lg border border-border bg-background px-4 py-3">
                  <div>
                    <p class="font-medium text-sm">{{ permission.permissionName }}</p>
                  </div>
                  <label class="inline-flex items-center gap-3 text-sm font-medium leading-none cursor-pointer">
                    <span class="text-muted-foreground">Granted</span>
                    <input
                      type="checkbox"
                      class="peer h-4 w-4 shrink-0 rounded-sm border border-primary ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                      [checked]="permission.isGranted"
                      (change)="togglePermission(i, $any($event.target).checked)"
                    />
                  </label>
                </div>
              }
            </div>

            <div class="mt-6 pt-4 border-t border-border">
              <ui-button variant="default" (click)="savePermissions()">Save permissions</ui-button>
            </div>
          </section>

          <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
            <h2 class="text-xl font-semibold tracking-tight">Claims</h2>
            <p class="mt-2 text-sm text-muted-foreground">Add, edit, or remove claim values.</p>

            <div class="mt-6 space-y-4">
              @for (claim of claims(); track claim; let i = $index) {
                <div class="grid gap-3 md:grid-cols-[1fr_1fr_auto]">
                  <input
                    type="text"
                    class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    [value]="claim.claimType"
                    (input)="updateClaim(i, 'claimType', $any($event.target).value)"
                    placeholder="Claim type"
                  />
                  <input
                    type="text"
                    class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    [value]="claim.claimValue"
                    (input)="updateClaim(i, 'claimValue', $any($event.target).value)"
                    placeholder="Claim value"
                  />
                  <ui-button variant="destructive" size="icon" (click)="removeClaim(i)" title="Remove claim">X</ui-button>
                </div>
              }
            </div>

            <div class="mt-4 flex flex-wrap gap-3 pt-4 border-t border-border">
              <ui-button variant="secondary" (click)="addClaim()">Add claim</ui-button>
              <ui-button variant="default" (click)="saveClaims()">Save claims</ui-button>
            </div>
          </section>
        </div>
      </section>
      } @else {
        <div class="rounded-xl border bg-card p-6 text-muted-foreground shadow-sm text-center">Loading user details...</div>
      }
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
