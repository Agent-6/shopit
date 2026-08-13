import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UsersService } from './users.service';
import { UserClaimRequest, UserPermissionRequest, UpdateUserRequest } from './users.model';
import { UserEditorComponent } from './user-editor.component';
import { Component, computed, inject, signal } from '@angular/core';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [RouterLink, UserEditorComponent, UiButtonComponent, UiIconComponent, DatePipe],
  template: `
    <div class="space-y-6">
      <header class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
        <div class="flex flex-col gap-2">
          <div class="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
            <a class="font-semibold text-primary transition-colors hover:text-primary/80" routerLink="/users">← Back to users</a>
            <span>User details</span>
          </div>
          <div class="flex flex-wrap items-center gap-3">
            <h1 class="text-3xl font-semibold tracking-tight">{{ userName() }}</h1>

            @if (selectedUser()) {
              @if (selectedUser()?.status === 'PendingActivation') {
                <span class="inline-flex items-center gap-1 rounded-full border border-amber-500/40 px-2.5 py-0.5 text-xs font-semibold text-amber-600 dark:text-amber-500">
                  <ui-icon name="clock" class="h-3 w-3"></ui-icon>
                  Pending activation
                </span>
              } @else {
                <span class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold"
                      [class.border-green-500/40]="selectedUser()?.isActive"
                      [class.text-green-600]="selectedUser()?.isActive"
                      [class.dark:text-green-500]="selectedUser()?.isActive"
                      [class.border-destructive/40]="!selectedUser()?.isActive"
                      [class.text-destructive]="!selectedUser()?.isActive">
                  {{ selectedUser()?.isActive ? 'Active' : 'Inactive' }}
                </span>
              }

              @if (isLocked()) {
                <span class="inline-flex items-center rounded-full border border-amber-500/40 px-2.5 py-0.5 text-xs font-semibold text-amber-600 dark:text-amber-500">
                  Locked until {{ selectedUser()?.lockoutEnd | date: 'medium' }}
                </span>
              }
            }
          </div>
          <p class="text-sm text-muted-foreground">{{ selectedUser()?.email ?? 'Loading user details…' }}</p>
        </div>
      </header>

      @if (selectedUser()) {
        <section class="grid gap-6 lg:grid-cols-[1.5fr_1fr]">
          <div class="space-y-6">
            @if (permissionService.has(perms.Users.Update)) {
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
            } @else {
              <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
                <h2 class="text-xl font-semibold tracking-tight">Profile</h2>
                <dl class="mt-6 space-y-3 text-sm">
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <dt class="text-muted-foreground">Username</dt>
                    <dd class="font-medium">{{ selectedUser()?.username }}</dd>
                  </div>
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <dt class="text-muted-foreground">Full name</dt>
                    <dd class="font-medium">{{ fullName() || '—' }}</dd>
                  </div>
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <dt class="text-muted-foreground">Email</dt>
                    <dd class="font-medium">{{ selectedUser()?.email }}</dd>
                  </div>
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <dt class="text-muted-foreground">Phone</dt>
                    <dd class="font-medium">{{ selectedUser()?.phoneNumber || '—' }}</dd>
                  </div>
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <dt class="text-muted-foreground">Member since</dt>
                    <dd class="font-medium">{{ selectedUser()?.createdAt | date: 'medium' }}</dd>
                  </div>
                </dl>
              </section>
            }

            @if (permissionService.has(perms.Users.ManagePermissions)) {
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
            }

            @if (permissionService.has(perms.Users.ManageClaims)) {
              <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
                <h2 class="text-xl font-semibold tracking-tight">Claims</h2>
                <p class="mt-2 text-sm text-muted-foreground">Add, edit, or remove claim values. Removing a claim persists immediately.</p>

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
                      <ui-button variant="destructive" size="icon" (click)="removeClaim(claim)" title="Remove claim">X</ui-button>
                    </div>
                  }
                </div>

                <div class="mt-4 flex flex-wrap gap-3 pt-4 border-t border-border">
                  <ui-button variant="secondary" (click)="addClaim()">Add claim</ui-button>
                  <ui-button variant="default" (click)="saveClaims()">Save claims</ui-button>
                </div>
              </section>
            }
          </div>

          <div class="space-y-6">
            <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
              <h2 class="text-xl font-semibold tracking-tight">Roles</h2>
              <p class="mt-2 text-sm text-muted-foreground">Assign roles to control the permissions this user inherits.</p>

              @if (selectedRoles().length > 0) {
                <div class="mt-4 flex flex-wrap gap-2">
                  @for (role of selectedRoles(); track role) {
                    <span class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold">{{ role }}</span>
                  }
                </div>
              } @else {
                <div class="mt-4 text-sm text-muted-foreground italic text-center py-2">No roles assigned.</div>
              }

              @if (permissionService.has(perms.Users.ManageRoles)) {
                <div class="mt-6 space-y-2.5">
                  @for (role of service.availableRoles(); track role.id) {
                    <label class="flex items-center justify-between gap-3 rounded-lg border border-border bg-background px-4 py-3 cursor-pointer hover:bg-accent/50 transition-colors">
                      <div>
                        <p class="font-medium text-sm">{{ role.name }}</p>
                        @if (role.description) {
                          <p class="text-xs text-muted-foreground mt-0.5">{{ role.description }}</p>
                        }
                      </div>
                      <input
                        type="checkbox"
                        class="peer h-4 w-4 shrink-0 rounded-sm border border-primary ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                        [checked]="isRoleSelected(role.name)"
                        (change)="toggleRole(role.name, $any($event.target).checked)"
                      />
                    </label>
                  }
                </div>

                <div class="mt-6 pt-4 border-t border-border">
                  <ui-button variant="default" (click)="saveRoles()">Save roles</ui-button>
                </div>
              }
            </section>

            @if (permissionService.hasAny(perms.Users.LockUnlock, perms.Users.Update, perms.Users.ResetPassword)) {
              <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
                <h2 class="text-xl font-semibold tracking-tight">Account security</h2>
                <p class="mt-2 text-sm text-muted-foreground">Manage lockout state and account activity.</p>

                <div class="mt-6 space-y-3 text-sm">
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <span class="text-muted-foreground">Email confirmed</span>
                    <span class="font-medium">{{ selectedUser()?.emailConfirmed ? 'Yes' : 'No' }}</span>
                  </div>
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <span class="text-muted-foreground">Phone confirmed</span>
                    <span class="font-medium">{{ selectedUser()?.phoneNumberConfirmed ? 'Yes' : 'No' }}</span>
                  </div>
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <span class="text-muted-foreground">Two-factor auth</span>
                    <span class="font-medium">{{ selectedUser()?.twoFactorEnabled ? 'Enabled' : 'Disabled' }}</span>
                  </div>
                  <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                    <span class="text-muted-foreground">Lockout</span>
                    <span class="font-medium">{{ isLocked() ? 'Locked' : 'Not locked' }}</span>
                  </div>
                </div>

                <div class="mt-6 flex flex-wrap gap-2 pt-4 border-t border-border">
                  @if (permissionService.has(perms.Users.LockUnlock)) {
                    @if (isLocked()) {
                      <ui-button variant="outline" size="sm" icon="unlock" (click)="unlock()">Unlock</ui-button>
                    } @else {
                      <ui-button variant="outline" size="sm" icon="lock" (click)="lock()">Lock for 30 min</ui-button>
                    }
                  }

                  @if (permissionService.has(perms.Users.Update)) {
                    @if (selectedUser()?.isActive) {
                      <ui-button variant="secondary" size="sm" icon="user-x" (click)="deactivate()">Deactivate</ui-button>
                    } @else {
                      <ui-button variant="secondary" size="sm" icon="user-check" (click)="activate()">Activate</ui-button>
                    }
                  }
                </div>

                @if (permissionService.has(perms.Users.ResetPassword)) {
                  <div class="mt-6 space-y-3 pt-4 border-t border-border">
                    <h3 class="text-sm font-medium leading-none">Reset password</h3>
                    <div class="flex gap-2">
                      <input
                        type="password"
                        placeholder="New password"
                        class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                        [value]="newPassword()"
                        (input)="newPassword.set($any($event.target).value)"
                      />
                      <ui-button variant="default" size="sm" (click)="resetPassword()" [disabled]="!newPassword()">Set</ui-button>
                    </div>
                  </div>
                }
              </section>
            }
          </div>
        </section>
      } @else {
        <div class="rounded-xl border bg-card p-6 text-muted-foreground shadow-sm text-center">Loading user details...</div>
      }
    </div>
  `,
})
export class UserDetailComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly service = inject(UsersService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;

  protected readonly permissions = signal<UserPermissionRequest[]>([]);
  protected readonly claims = signal<UserClaimRequest[]>([]);
  protected readonly selectedRoles = signal<string[]>([]);
  protected readonly newPassword = signal('');

  protected readonly selectedUser = this.service.selectedUser;
  protected readonly userName = computed(() => this.selectedUser()?.username ?? 'User details');
  protected readonly fullName = computed(() =>
    [this.selectedUser()?.firstName, this.selectedUser()?.lastName].filter(Boolean).join(' ').trim()
  );
  protected readonly isLocked = computed(() => {
    const end = this.selectedUser()?.lockoutEnd;
    return !!end && new Date(end).getTime() > Date.now();
  });

  constructor() {
    const userId = this.route.snapshot.params['id'] as string;
    if (userId) {
      this.loadUser(userId);
    }
  }

  protected async loadUser(userId: string): Promise<void> {
    // Load the data needed by the visible sections. The roles card is always shown, so the
    // user's own roles (view-gated) always load; the assignable roles list (role.view-gated)
    // and the editable cards only load when the caller has the matching manage permission.
    const loads: Promise<void>[] = [this.service.loadUser(userId), this.service.loadUserRoles(userId)];

    if (this.permissionService.has(ShopItPermissions.Users.ManageRoles)) {
      loads.push(this.service.loadAvailableRoles());
    }
    if (this.permissionService.has(ShopItPermissions.Users.ManagePermissions)) {
      loads.push(this.service.loadPermissions(userId));
    }
    if (this.permissionService.has(ShopItPermissions.Users.ManageClaims)) {
      loads.push(this.service.loadClaims(userId));
    }

    await Promise.all(loads);

    this.permissions.set(this.service.permissions());
    this.claims.set(this.service.claims());
    this.selectedRoles.set(this.service.userRoles());
  }

  // ------------------------------------------------------------------
  // Profile
  // ------------------------------------------------------------------

  protected async handleProfileSave(value: UpdateUserRequest): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.updateUser(user.id, value);
  }

  // ------------------------------------------------------------------
  // Roles
  // ------------------------------------------------------------------

  protected isRoleSelected(roleName: string): boolean {
    return this.selectedRoles().some((r) => r.toLowerCase() === roleName.toLowerCase());
  }

  protected toggleRole(roleName: string, checked: boolean): void {
    this.selectedRoles.update((current) =>
      checked ? [...current, roleName] : current.filter((r) => r.toLowerCase() !== roleName.toLowerCase())
    );
  }

  protected async saveRoles(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.setUserRoles(user.id, this.selectedRoles());
  }

  // ------------------------------------------------------------------
  // Permissions
  // ------------------------------------------------------------------

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

  // ------------------------------------------------------------------
  // Claims
  // ------------------------------------------------------------------

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

  protected async removeClaim(claim: UserClaimRequest): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }

    const confirmed = confirm(`Remove claim "${claim.claimType}: ${claim.claimValue}"?`);
    if (!confirmed) {
      return;
    }

    await this.service.removeClaim(user.id, claim.claimType, claim.claimValue);
    this.claims.set(this.service.claims());
  }

  protected async saveClaims(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.saveClaims(user.id, this.claims(), []);
  }

  // ------------------------------------------------------------------
  // Security
  // ------------------------------------------------------------------

  protected async lock(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.lockUser(user.id, null);
  }

  protected async unlock(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.unlockUser(user.id);
  }

  protected async activate(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    await this.service.activateUser(user.id);
  }

  protected async deactivate(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }
    const confirmed = confirm(`Deactivate ${user.username}? The account will no longer be active.`);
    if (!confirmed) {
      return;
    }
    await this.service.deactivateUser(user.id);
  }

  protected async resetPassword(): Promise<void> {
    const user = this.selectedUser();
    if (!user || !this.newPassword()) {
      return;
    }
    const ok = await this.service.updateUserPassword(user.id, this.newPassword());
    if (ok) {
      this.newPassword.set('');
    }
  }
}
