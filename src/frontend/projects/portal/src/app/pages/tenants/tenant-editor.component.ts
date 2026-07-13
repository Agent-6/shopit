import { Component, EventEmitter, Input, OnChanges, Output, signal } from '@angular/core';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { CreateTenantRequest, Tenant, UpdateTenantRequest } from './tenant.model';

type TenantEditorState = Partial<CreateTenantRequest> & Partial<UpdateTenantRequest>;

@Component({
  selector: 'app-tenant-editor',
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
          <label class="text-sm font-medium leading-none">Tenant name</label>
          <input
            type="text"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [value]="form().name"
            (input)="updateField('name', $any($event.target).value)"
            required
          />
        </div>
      </div>

      <div class="flex justify-end pt-4 border-t border-border">
        <ui-button variant="default" (click)="submitForm()">{{ submitLabel }}</ui-button>
      </div>
    </section>
  `
})
export class TenantEditorComponent implements OnChanges {
  @Input() title = 'Edit tenant';
  @Input() submitLabel = 'Save tenant';
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() model?: Tenant | null = null;
  @Output() submit = new EventEmitter<CreateTenantRequest | UpdateTenantRequest>();
  @Output() cancel = new EventEmitter<void>();

  protected readonly form = signal<TenantEditorState>({ name: '' });

  ngOnChanges(): void {
    this.form.set(this.model ? { name: this.model.name } : { name: '' });
  }

  updateField(field: keyof TenantEditorState, value: string): void {
    this.form.update((current) => ({ ...current, [field]: value }));
  }

  submitForm(): void {
    const current = { ...this.form() };

    if (this.mode === 'edit') {
      delete current.id;
    }

    this.submit.emit(current as CreateTenantRequest | UpdateTenantRequest);
  }
}
