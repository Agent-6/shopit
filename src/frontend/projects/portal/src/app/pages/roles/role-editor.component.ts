import { Component, EventEmitter, Input, OnChanges, Output, signal } from '@angular/core';
import { Role, CreateRoleRequest, UpdateRoleRequest } from './role.model';
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

  protected readonly form = signal<RoleEditorState>({
    name: '',
    description: null
  });

  ngOnChanges(): void {
    this.form.set(
      this.model
        ? { name: this.model.name, description: this.model.description ?? null }
        : { name: '', description: null }
    );
  }

  updateField<Key extends keyof CreateRoleRequest | keyof UpdateRoleRequest>(field: Key, value: any): void {
    this.form.update((current) => ({ ...current, [field]: value }));
  }

  submitForm(): void {
    this.submit.emit({ ...this.form() } as CreateRoleRequest | UpdateRoleRequest);
  }
}
