import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { PageHeaderComponent } from '../../core/components/page/page-header.component';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { PermissionGroup } from './permissions.model';
import { PermissionsService } from './permissions.service';

const CHECKBOX_CLASS =
  'h-4 w-4 shrink-0 rounded-sm border border-primary ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50';

@Component({
  selector: 'app-permission-matrix',
  standalone: true,
  imports: [PageHeaderComponent, UiButtonComponent, UiIconComponent],
  template: `
    <section class="flex flex-col gap-6">
      <app-page-header
        title="Permission matrix"
        subtitle="Grant permissions to roles. Changes are applied per role when you save it."
      >
        <ui-button variant="outline" icon="refresh-cw" (click)="load()">Refresh</ui-button>
      </app-page-header>

      <div class="flex flex-wrap items-end gap-4">
        <div class="w-full max-w-xs space-y-1.5">
          <label class="text-sm font-medium leading-none" for="matrix-search">Search permissions</label>
          <input
            id="matrix-search"
            type="search"
            placeholder="Name or key…"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            [value]="filter()"
            (input)="filter.set($any($event.target).value)"
          />
        </div>
        <p class="text-sm text-muted-foreground ml-auto pb-2">
          {{ roles().length }} role{{ roles().length === 1 ? '' : 's' }} · {{ permissionCount() }} permissions
        </p>
      </div>

      @if (service.error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          <ui-icon name="alert-circle" class="inline-block mr-2 h-4 w-4"></ui-icon>
          {{ service.error() }}
        </div>
      }

      @if (service.loading() && !matrix()) {
        <div class="rounded-xl border bg-card p-10 text-sm text-muted-foreground text-center">
          Loading permission matrix…
        </div>
      } @else if (matrix()) {
        @if (filter() && filteredGroups().length === 0) {
          <div class="rounded-lg border bg-card p-6 text-sm text-muted-foreground text-center">
            No permissions match your search.
          </div>
        } @else {
          <div class="rounded-xl border bg-card shadow-sm overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm border-collapse">
                <thead>
                  <tr class="border-b border-border bg-muted/30">
                    <th class="sticky left-0 z-10 bg-muted/30 px-4 py-3 text-left font-semibold min-w-[240px]">
                      Permission
                    </th>
                    @for (role of roles(); track role.id) {
                      <th class="px-3 py-3 min-w-[150px] align-top">
                        <div class="flex flex-col items-center gap-1.5">
                          <span class="font-semibold">{{ role.name }}</span>
                          <span class="text-xs text-muted-foreground">{{ grantedCount(role.id) }}/{{ permissionCount() }}</span>
                          @if (savedRole() === role.id) {
                            <span class="inline-flex items-center gap-1 text-xs font-medium text-green-600">
                              <ui-icon name="check" class="h-3.5 w-3.5"></ui-icon>
                              Saved
                            </span>
                          } @else if (isDirty(role.id)) {
                            <ui-button
                              variant="default"
                              size="sm"
                              (click)="saveRole(role.id)"
                              [disabled]="savingRole() === role.id"
                            >
                              {{ savingRole() === role.id ? 'Saving…' : 'Save changes' }}
                            </ui-button>
                          }
                        </div>
                      </th>
                    }
                  </tr>
                </thead>
                <tbody>
                  @for (group of filteredGroups(); track group.name) {
                    <tr class="bg-muted/20 border-b border-border">
                      <td class="sticky left-0 bg-muted/20 px-4 py-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                        {{ group.displayName }}
                      </td>
                      @for (role of roles(); track role.id) {
                        <td class="px-3 py-2 text-center">
                          <input
                            type="checkbox"
                            class="{{ CHECKBOX_CLASS }}"
                            [checked]="isGroupGranted(role.id, group)"
                            [title]="isGroupGranted(role.id, group) ? 'Revoke all in this group' : 'Grant all in this group'"
                            (change)="toggleGroup(role.id, group, $any($event.target).checked)"
                          />
                        </td>
                      }
                    </tr>
                    @for (permission of group.permissions; track permission.name) {
                      <tr class="border-b border-border/60 hover:bg-muted/30 transition-colors">
                        <td class="sticky left-0 bg-card px-4 py-2.5">
                          <p class="font-medium leading-snug">{{ permission.displayName }}</p>
                          @if (permission.description) {
                            <p class="text-xs text-muted-foreground mt-0.5">{{ permission.description }}</p>
                          }
                          <code class="text-[10px] text-muted-foreground/80">{{ permission.name }}</code>
                        </td>
                        @for (role of roles(); track role.id) {
                          <td class="px-3 py-2.5 text-center">
                            <input
                              type="checkbox"
                              class="{{ CHECKBOX_CLASS }}"
                              [checked]="isGranted(role.id, permission.name)"
                              [title]="'Grant ' + permission.name + ' to ' + role.name"
                              (change)="toggle(role.id, permission.name, $any($event.target).checked)"
                            />
                          </td>
                        }
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>
          </div>
        }
      }
    </section>
  `,
})
export class PermissionMatrixComponent implements OnDestroy {
  protected readonly CHECKBOX_CLASS = CHECKBOX_CLASS;
  private savedRoleTimer: ReturnType<typeof setTimeout> | null = null;
  protected readonly service = inject(PermissionsService);

  protected readonly filter = signal('');
  protected readonly pending = signal<Record<string, string[]>>({}); // roleId -> granted permission names
  protected readonly dirtyRoles = signal<Set<string>>(new Set());
  protected readonly savingRole = signal<string | null>(null);
  protected readonly savedRole = signal<string | null>(null);

  protected readonly matrix = this.service.matrix;
  protected readonly groups = computed(() => this.matrix()?.groups ?? []);
  protected readonly roles = computed(() => this.matrix()?.roles ?? []);
  protected readonly permissionCount = computed(() =>
    this.groups().reduce((sum, group) => sum + group.permissions.length, 0)
  );

  private readonly catalogNames = computed(() =>
    new Set(this.groups().flatMap((group) => group.permissions.map((permission) => permission.name)))
  );

  protected readonly filteredGroups = computed(() => {
    const query = this.filter().trim().toLowerCase();
    if (!query) {
      return this.groups();
    }

    return this.groups()
      .map((group) => ({
        ...group,
        permissions: group.permissions.filter(
          (permission) =>
            permission.displayName.toLowerCase().includes(query) || permission.name.toLowerCase().includes(query)
        ),
      }))
      .filter((group) => group.permissions.length > 0);
  });

  constructor() {
    this.load();
  }

  protected async load(): Promise<void> {
    await this.service.loadMatrix();
    this.dirtyRoles.set(new Set());

    const next: Record<string, string[]> = {};
    for (const role of this.roles()) {
      next[role.id] = role.claims
        .filter((claim) => this.catalogNames().has(claim.type))
        .map((claim) => claim.type);
    }
    this.pending.set(next);
  }

  // ------------------------------------------------------------------
  // Toggles
  // ------------------------------------------------------------------

  protected isGranted(roleId: string, permissionName: string): boolean {
    return this.pending()[roleId]?.includes(permissionName) ?? false;
  }

  protected grantedCount(roleId: string): number {
    return this.pending()[roleId]?.length ?? 0;
  }

  protected isDirty(roleId: string): boolean {
    return this.dirtyRoles().has(roleId);
  }

  protected isGroupGranted(roleId: string, group: PermissionGroup): boolean {
    const granted = this.pending()[roleId] ?? [];
    return group.permissions.length > 0 && group.permissions.every((permission) => granted.includes(permission.name));
  }

  protected toggle(roleId: string, permissionName: string, checked: boolean): void {
    this.updateRole(roleId, (granted) => {
      if (checked) {
        granted.add(permissionName);
      } else {
        granted.delete(permissionName);
      }
      return granted;
    });
  }

  protected toggleGroup(roleId: string, group: PermissionGroup, checked: boolean): void {
    this.updateRole(roleId, (granted) => {
      for (const permission of group.permissions) {
        if (checked) {
          granted.add(permission.name);
        } else {
          granted.delete(permission.name);
        }
      }
      return granted;
    });
  }

  private updateRole(roleId: string, mutate: (granted: Set<string>) => Set<string>): void {
    this.pending.update((current) => {
      const next = mutate(new Set(current[roleId] ?? []));
      return { ...current, [roleId]: [...next] };
    });

    this.dirtyRoles.update((current) => {
      const next = new Set(current);
      next.add(roleId);
      return next;
    });

    // A fresh edit must take precedence over the transient "Saved" indicator.
    if (this.savedRole() === roleId) {
      this.savedRole.set(null);
    }
  }

  ngOnDestroy(): void {
    if (this.savedRoleTimer !== null) {
      clearTimeout(this.savedRoleTimer);
    }
  }

  // ------------------------------------------------------------------
  // Save
  // ------------------------------------------------------------------

  protected async saveRole(roleId: string): Promise<void> {
    const role = this.roles().find((r) => r.id === roleId);
    if (!role) {
      return;
    }

    const granted = this.pending()[roleId] ?? [];

    // Send the full catalog so unchecked permissions are revoked; custom claims are
    // preserved by the backend (the permissions endpoint only touches catalog claims).
    const permissions = this.catalogNames().size > 0
      ? [...this.catalogNames()].map((name) => ({
          permissionName: name,
          isGranted: granted.includes(name),
        }))
      : [];

    this.savingRole.set(roleId);
    const ok = await this.service.saveRolePermissions(roleId, permissions);
    this.savingRole.set(null);

    if (ok) {
      this.dirtyRoles.update((current) => {
        const next = new Set(current);
        next.delete(roleId);
        return next;
      });
      this.savedRole.set(roleId);
      if (this.savedRoleTimer !== null) {
        clearTimeout(this.savedRoleTimer);
      }
      this.savedRoleTimer = setTimeout(() => {
        if (this.savedRole() === roleId) {
          this.savedRole.set(null);
        }
      }, 2000);
    }
  }
}
