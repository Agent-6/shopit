import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';
import { TenantEditorComponent } from './tenant-editor.component';
import { Tenant } from './tenant.model';
import { TenantService } from './tenant.service';

@Component({
  selector: 'app-tenant-detail',
  standalone: true,
  imports: [RouterLink, TenantEditorComponent, UiButtonComponent],
  template: `
    <div class="space-y-6">
      <header class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
        <div class="flex flex-col gap-2">
          <div class="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
            <a class="font-semibold text-primary transition-colors hover:text-primary/80" routerLink="/tenants">← Back to tenants</a>
            <span>Tenant details</span>
          </div>
          <h1 class="text-3xl font-semibold tracking-tight">{{ tenantName() }}</h1>
          <p class="text-sm text-muted-foreground">{{ selectedTenant()?.isActive ? 'Active tenant' : 'Inactive tenant' }}</p>
        </div>
      </header>

      @if (selectedTenant()) {
        <section class="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
          @if (permissionService.has(perms.Tenants.Update)) {
            <div class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
              <app-tenant-editor
                [title]="'Edit ' + (selectedTenant()?.name ?? 'tenant')"
                [submitLabel]="'Save tenant'"
                [mode]="'edit'"
                [model]="selectedTenant()"
                (submit)="handleTenantSave($event)"
                (cancel)="router.navigate(['/tenants'])"
              ></app-tenant-editor>
            </div>
          } @else {
            <section class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
              <h2 class="text-xl font-semibold tracking-tight">Tenant</h2>
              <dl class="mt-6 space-y-3 text-sm">
                <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                  <dt class="text-muted-foreground">Name</dt>
                  <dd class="font-medium">{{ selectedTenant()?.name }}</dd>
                </div>
                <div class="flex items-center justify-between rounded-lg border border-border bg-background px-4 py-3">
                  <dt class="text-muted-foreground">Status</dt>
                  <dd class="font-medium">{{ selectedTenant()?.isActive ? 'Active' : 'Inactive' }}</dd>
                </div>
              </dl>
            </section>
          }

          <div class="rounded-xl border bg-card text-card-foreground p-6 shadow-sm">
            <h2 class="text-xl font-semibold tracking-tight">Account status</h2>
            <p class="mt-2 text-sm text-muted-foreground">Use these actions to change the tenant availability.</p>

            <div class="mt-6 space-y-4">
              <div class="rounded-lg border border-border bg-background p-4">
                <p class="text-sm font-medium">Current status</p>
                <p class="mt-1 text-sm text-muted-foreground">{{ selectedTenant()?.isActive ? 'Active' : 'Inactive' }}</p>
              </div>

              @if (permissionService.has(perms.Tenants.ActivateDeactivate)) {
                <div class="flex flex-wrap gap-3">
                  @if (selectedTenant()?.isActive) {
                    <ui-button variant="outline" (click)="deactivateTenant()">Deactivate tenant</ui-button>
                  } @else {
                    <ui-button variant="default" (click)="activateTenant()">Activate tenant</ui-button>
                  }
                </div>
              } @else {
                <p class="text-xs text-muted-foreground">You have read-only access to tenant status.</p>
              }
            </div>
          </div>
        </section>
      } @else {
        <div class="rounded-xl border bg-card p-6 text-muted-foreground shadow-sm text-center">Loading tenant details...</div>
      }
    </div>
  `
})
export class TenantDetailComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly service = inject(TenantService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;

  protected readonly selectedTenant = this.service.selectedTenant;
  protected readonly tenantName = computed(() => this.selectedTenant()?.name ?? 'Tenant details');

  constructor() {
    const tenantId = this.route.snapshot.params['id'] as string;
    if (tenantId) {
      this.loadTenant(tenantId);
    }
  }

  protected loadTenant(tenantId: string): void {
    this.service.loadTenant(tenantId);
  }

  protected handleTenantSave(value: { name: string }): void {
    const tenant = this.selectedTenant();
    if (!tenant) {
      return;
    }

    this.service.updateTenant(tenant.id, value.name);
  }

  protected activateTenant(): void {
    const tenant = this.selectedTenant();
    if (tenant) {
      this.service.activateTenant(tenant.id);
    }
  }

  protected deactivateTenant(): void {
    const tenant = this.selectedTenant();
    if (tenant) {
      this.service.deactivateTenant(tenant.id);
    }
  }
}
