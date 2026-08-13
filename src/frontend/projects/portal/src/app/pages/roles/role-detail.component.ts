import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RolesService } from './roles.service';
import { UpdateRoleRequest } from './role.model';
import { RoleEditorComponent } from './role-editor.component';
import { Component, computed, inject } from '@angular/core';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [RouterLink, RoleEditorComponent],
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
            <h2 class="text-xl font-semibold tracking-tight">Permissions</h2>
            <p class="mt-2 text-sm text-muted-foreground">Permissions granted to every user assigned this role. These show up as inherited permissions on users.</p>

            @if (permissionService.has(perms.Roles.ManagePermissions)) {
              <p class="mt-6 text-sm text-muted-foreground">
                Manage permissions from the
                <a class="font-medium text-primary hover:underline" routerLink="/permissions">permission matrix</a>.
              </p>
            }

            <div class="mt-6 space-y-3">
              @for (claim of selectedRole()?.claims ?? []; track claim) {
                <div class="flex items-center justify-between gap-3 rounded-lg border border-border bg-background px-4 py-3">
                  <dt class="text-muted-foreground text-sm">{{ claim.type }}</dt>
                  <dd class="font-medium text-sm break-all text-right">{{ claim.value }}</dd>
                </div>
              }
              @if (!(selectedRole()?.claims?.length ?? 0)) {
                <div class="text-sm text-muted-foreground italic text-center py-2">No permissions assigned to this role.</div>
              }
            </div>
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
  }

  protected async handleProfileSave(value: UpdateRoleRequest): Promise<void> {
    const role = this.selectedRole();
    if (!role) {
      return;
    }
    await this.service.updateRole(role.id, value);
  }
}
