import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RolesService } from './roles.service';
import { RoleClaimRequest, UpdateRoleRequest } from './role.model';
import { RoleEditorComponent } from './role-editor.component';
import { Component, computed, inject, signal } from '@angular/core';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [RouterLink, RoleEditorComponent, UiButtonComponent],
  template: `
    <div class="space-y-6">
      <header class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
        <div class="flex flex-col gap-2">
          <div class="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
            <a class="font-semibold text-primary transition-colors hover:text-primary/80" routerLink="/roles">← Back to roles</a>
            <span>Role details</span>
          </div>
          <h1 class="text-3xl font-semibold tracking-tight">{{ roleName() }}</h1>
          <p class="text-sm text-muted-foreground">{{ selectedRole()?.description || 'No description' }}</p>
        </div>
      </header>

      @if (selectedRole()) {
        <section class="grid gap-6 lg:grid-cols-[1.5fr_1fr]">
          @if (permissionService.has(perms.Roles.Update)) {
            <div class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
              <app-role-editor
                [title]="'Edit ' + (selectedRole()?.name ?? 'role')"
                [submitLabel]="'Save role'"
                [mode]="'edit'"
                [model]="selectedRole()"
                (submit)="handleProfileSave($event)"
                (cancel)="router.navigate(['/roles'])"
              ></app-role-editor>
            </div>
          } @else {
            <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
              <h2 class="text-xl font-semibold tracking-tight">Role</h2>
              <dl class="mt-6 space-y-3 text-sm">
                <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                  <dt class="text-muted-foreground">Name</dt>
                  <dd class="font-medium">{{ selectedRole()?.name }}</dd>
                </div>
                <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                  <dt class="text-muted-foreground">Description</dt>
                  <dd class="font-medium text-right">{{ selectedRole()?.description || '—' }}</dd>
                </div>
              </dl>
            </section>
          }

          <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
            <h2 class="text-xl font-semibold tracking-tight">Permissions (claims)</h2>
            <p class="mt-2 text-sm text-muted-foreground">Claims granted to every user assigned this role. These show up as inherited permissions on users.</p>

            <div class="mt-6 space-y-4">
              @for (claim of claims(); track claim; let i = $index) {
                <div class="grid gap-3 md:grid-cols-[1fr_1fr_auto]">
                  <input
                    type="text"
                    class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    [value]="claim.claimType"
                    [readonly]="!permissionService.has(perms.Roles.ManagePermissions)"
                    (input)="updateClaim(i, 'claimType', $any($event.target).value)"
                    placeholder="Claim type (e.g. permission)"
                  />
                  <input
                    type="text"
                    class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    [value]="claim.claimValue"
                    [readonly]="!permissionService.has(perms.Roles.ManagePermissions)"
                    (input)="updateClaim(i, 'claimValue', $any($event.target).value)"
                    placeholder="Claim value"
                  />
                  @if (permissionService.has(perms.Roles.ManagePermissions)) {
                    <ui-button variant="destructive" size="icon" (click)="removeClaim(i)" title="Remove claim">X</ui-button>
                  }
                </div>
              }

              @if (!claims().length) {
                <div class="text-sm text-muted-foreground italic text-center py-2">No claims assigned. Add a claim below.</div>
              }
            </div>

            @if (permissionService.has(perms.Roles.ManagePermissions)) {
              <div class="mt-4 flex flex-wrap gap-3 pt-4 border-t border-border">
                <ui-button variant="secondary" (click)="addClaim()">Add claim</ui-button>
                <ui-button variant="default" (click)="saveClaims()">Save claims</ui-button>
              </div>
            } @else {
              <p class="mt-6 text-xs text-muted-foreground">You have read-only access to this role's permissions.</p>
            }
          </section>
        </section>
      } @else {
        <div class="rounded-xl border bg-card p-6 text-muted-foreground shadow-sm text-center">Loading role details...</div>
      }
    </div>
  `,
})
export class RoleDetailComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly service = inject(RolesService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;

  protected readonly claims = signal<RoleClaimRequest[]>([]);
  protected readonly selectedRole = this.service.selectedRole;
  protected readonly roleName = computed(() => this.selectedRole()?.name ?? 'Role details');

  constructor() {
    const roleId = this.route.snapshot.params['id'] as string;
    if (roleId) {
      this.loadRole(roleId);
    }
  }

  protected async loadRole(roleId: string): Promise<void> {
    await this.service.loadRole(roleId);
    this.claims.set(this.service.selectedRole()?.claims ?? []);
  }

  protected async handleProfileSave(value: UpdateRoleRequest): Promise<void> {
    const role = this.selectedRole();
    if (!role) {
      return;
    }
    await this.service.updateRole(role.id, value);
  }

  protected addClaim(): void {
    this.claims.update((current) => [...current, { claimType: '', claimValue: '' }]);
  }

  protected updateClaim(index: number, field: keyof RoleClaimRequest, value: string): void {
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
    const role = this.selectedRole();
    if (!role) {
      return;
    }
    await this.service.saveRoleClaims(role.id, this.claims());
  }
}
