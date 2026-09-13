import { Component, computed, EventEmitter, inject, Input, OnChanges, Output, signal } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { Role, RoleMultiTenancySide, CreateRoleRequest, UpdateRoleRequest } from './role.model';
import { UiButtonComponent } from '../../shared/components/ui-button.component';

type RoleEditorState = Partial<CreateRoleRequest> & Partial<UpdateRoleRequest>;

@Component({
  selector: 'app-role-editor',
  standalone: true,
  imports: [UiButtonComponent],
  template: `
    <section class="space-y-6">
      <div class="flex items-center justify-between gap-4 border-b border-border pb-4">
        <div>
          <h2 class="text-xl font-semibold tracking-tight">{{ title }}</h2>
        </div>
        <ui-button variant="outline" size="sm" (click)="cancel.emit()">Cancel</ui-button>
      </div>

      <div class="grid gap-4">
        <div class="grid gap-2">
          <label class="text-sm font-medium leading-none">Name</label>
          <input
            type="text"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [value]="form().name ?? ''"
            (input)="updateField('name', $any($event.target).value)"
            placeholder="e.g. Support Agent"
            required
          />
        </div>

        <div class="grid gap-2">
          <label class="text-sm font-medium leading-none">Description</label>
          <textarea
            rows="3"
            class="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [value]="form().description ?? ''"
            (input)="updateField('description', $any($event.target).value || null)"
            placeholder="What this role is for"
          ></textarea>
        </div>

        @if (mode === 'create') {
          <div class="grid gap-2">
            <label class="text-sm font-medium leading-none">Multi-tenancy side</label>
            <select
              class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              [value]="form().multiTenancySide ?? 'Both'"
              (change)="updateField('multiTenancySide', $any($event.target).value)"
            >
              @for (option of sideOptions(); track option) {
                <option [value]="option">{{ sideLabel(option) }}</option>
              }
            </select>
            <p class="text-xs text-muted-foreground">
              Roles are provisioned only on the side they are available on. Both-side roles are created wherever
              the seeder runs; host-only and tenant-only roles stay on their side.
            </p>
          </div>
        }
      </div>

      <div class="flex justify-end pt-4 border-t border-border">
        <ui-button variant="default" (click)="submitForm()">{{ submitLabel }}</ui-button>
      </div>
    </section>
  `,
})
export class RoleEditorComponent implements OnChanges {
  @Input() title = 'Edit role';
  @Input() submitLabel = 'Save role';
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() model?: Role | null = null;
  @Output() submit = new EventEmitter<CreateRoleRequest | UpdateRoleRequest>();
  @Output() cancel = new EventEmitter<void>();

  private readonly authService = inject(AuthService);

  protected readonly form = signal<RoleEditorState>({
    name: '',
    description: null,
    multiTenancySide: 'Both'
  });

  protected readonly sideOptions = computed<RoleMultiTenancySide[]>(() =>
    this.authService.currentSide === 'Host'
      ? ['Both', 'Host']
      : ['Both', 'Tenant']
  );

  protected sideLabel(side: RoleMultiTenancySide): string {
    return side === 'Host' ? 'Host only' : side === 'Tenant' ? 'Tenant only' : 'Both sides';
  }

  ngOnChanges(): void {
    this.form.set(
      this.model
        ? { name: this.model.name, description: this.model.description ?? null }
        : { name: '', description: null, multiTenancySide: 'Both' }
    );
  }

  updateField<Key extends keyof CreateRoleRequest | keyof UpdateRoleRequest>(field: Key, value: any): void {
    this.form.update((current) => ({ ...current, [field]: value }));
  }

  submitForm(): void {
    this.submit.emit({ ...this.form() } as CreateRoleRequest | UpdateRoleRequest);
  }
}
