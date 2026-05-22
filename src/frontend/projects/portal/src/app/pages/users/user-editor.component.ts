import { Component, EventEmitter, Input, OnChanges, Output, signal } from '@angular/core';
import { User, UserClaimRequest, CreateUserRequest, UpdateUserRequest } from './users.model';
import { UiButtonComponent } from '../../shared/components/ui-button.component';

type UserEditorState = Partial<CreateUserRequest> & Partial<UpdateUserRequest> & { password?: string | null };

@Component({
  selector: 'app-user-editor',
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
          <label class="text-sm font-medium leading-none">Username</label>
          <input
            type="text"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [value]="form().username"
            (input)="updateField('username', $any($event.target).value)"
            required
          />
        </div>

        <div class="grid gap-2">
          <label class="text-sm font-medium leading-none">Email</label>
          <input
            type="email"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [value]="form().email"
            (input)="updateField('email', $any($event.target).value)"
            required
          />
        </div>

        @if (mode === 'create') {
          <div class="grid gap-2">
            <label class="text-sm font-medium leading-none">Password</label>
            <input
              type="password"
              class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              [value]="form().password ?? ''"
              (input)="updateField('password', $any($event.target).value)"
              required
            />
          </div>
        }

        <div class="grid gap-4 md:grid-cols-2">
          <div class="grid gap-2">
            <label class="text-sm font-medium leading-none">First name</label>
            <input
              type="text"
              class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              [value]="form().firstName ?? ''"
              (input)="updateField('firstName', $any($event.target).value || null)"
            />
          </div>
          <div class="grid gap-2">
            <label class="text-sm font-medium leading-none">Last name</label>
            <input
              type="text"
              class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              [value]="form().lastName ?? ''"
              (input)="updateField('lastName', $any($event.target).value || null)"
            />
          </div>
        </div>

        <div class="grid gap-2">
          <label class="text-sm font-medium leading-none">Phone number</label>
          <input
            type="tel"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [value]="form().phoneNumber ?? ''"
            (input)="updateField('phoneNumber', $any($event.target).value || null)"
          />
        </div>

        <div class="grid gap-2">
          <label class="text-sm font-medium leading-none">Roles (comma-separated)</label>
          <input
            type="text"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [value]="form().roles?.join(', ') ?? ''"
            (input)="updateRoles($any($event.target).value)"
            placeholder="admin, user"
          />
        </div>

        <div class="flex items-center gap-3 py-2">
          <label class="inline-flex items-center gap-2 text-sm font-medium leading-none cursor-pointer">
            <input
              type="checkbox"
              class="peer h-4 w-4 shrink-0 rounded-sm border border-primary ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              [checked]="form().isActive ?? true"
              (change)="updateField('isActive', $any($event.target).checked)"
            />
            <span>Active account</span>
          </label>
        </div>

        <div class="space-y-4 rounded-xl border bg-card p-4">
          <div class="flex items-center justify-between">
            <h3 class="text-sm font-medium leading-none">Custom Claims</h3>
            <ui-button variant="secondary" size="sm" (click)="addClaim()">Add claim</ui-button>
          </div>
          <div class="space-y-3">
            @for (claim of form().claims ?? []; track claim; let i = $index) {
              <div class="grid gap-3 md:grid-cols-[1fr_1fr_auto]">
                <input
                  type="text"
                  class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  [value]="claim.claimType"
                  (input)="updateClaim(i, 'claimType', $any($event.target).value)"
                  placeholder="Type (e.g. org_id)"
                />
                <input
                  type="text"
                  class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  [value]="claim.claimValue"
                  (input)="updateClaim(i, 'claimValue', $any($event.target).value)"
                  placeholder="Value"
                />
                <ui-button variant="destructive" size="icon" (click)="removeClaim(i)" title="Remove claim">X</ui-button>
              </div>
            }
            @if (!form().claims?.length) {
              <div class="text-sm text-muted-foreground italic text-center py-2">No claims assigned.</div>
            }
          </div>
        </div>
      </div>

      <div class="flex justify-end pt-4 border-t border-border">
        <ui-button variant="default" (click)="submitForm()">{{ submitLabel }}</ui-button>
      </div>
    </section>
  `
})
export class UserEditorComponent implements OnChanges {
  @Input() title = 'Edit user';
  @Input() submitLabel = 'Save user';
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() model?: User | null = null;
  @Output() submit = new EventEmitter<CreateUserRequest | UpdateUserRequest>();
  @Output() cancel = new EventEmitter<void>();

  protected readonly form = signal<UserEditorState>({
    username: '',
    email: '',
    password: '',
    firstName: null,
    lastName: null,
    phoneNumber: null,
    roles: [],
    claims: [],
    isActive: true
  });

  ngOnChanges(): void {
    const nextModel: UserEditorState = this.model
      ? {
          username: this.model.username,
          email: this.model.email,
          firstName: this.model.firstName ?? null,
          lastName: this.model.lastName ?? null,
          phoneNumber: this.model.phoneNumber ?? null,
          roles: this.model.roles ?? [],
          claims: this.model.claims ?? [],
          isActive: this.model.isActive ?? true
        }
      : {
          username: '',
          email: '',
          password: '',
          firstName: null,
          lastName: null,
          phoneNumber: null,
          roles: [],
          claims: [],
          isActive: true
        };

    this.form.set(nextModel);
  }

  updateField<Key extends keyof CreateUserRequest | keyof UpdateUserRequest>(field: Key, value: any): void {
    this.form.update((current) => ({ ...current, [field]: value }));
  }

  updateRoles(value: string): void {
    const roles = value
      .split(',')
      .map((item) => item.trim())
      .filter(Boolean);
    this.form.update((current) => ({ ...current, roles }));
  }

  addClaim(): void {
    this.form.update((current) => ({
      ...current,
      claims: [...(current.claims ?? []), { claimType: '', claimValue: '' }]
    }));
  }

  updateClaim(index: number, field: keyof UserClaimRequest, value: string): void {
    this.form.update((current) => {
      const claims = [...(current.claims ?? [])];
      claims[index] = { ...(claims[index] ?? { claimType: '', claimValue: '' }), [field]: value };
      return { ...current, claims };
    });
  }

  removeClaim(index: number): void {
    this.form.update((current) => ({
      ...current,
      claims: (current.claims ?? []).filter((_, i) => i !== index)
    }));
  }

  submitForm(): void {
    const current = { ...this.form() };

    if (this.mode === 'edit') {
      delete current.password;
    }

    this.submit.emit(current as CreateUserRequest | UpdateUserRequest);
  }
}
