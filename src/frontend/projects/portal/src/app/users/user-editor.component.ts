import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, signal } from '@angular/core';
import { User, UserClaimRequest, CreateUserRequest, UpdateUserRequest } from './users.model';

type UserEditorState = Partial<CreateUserRequest> & Partial<UpdateUserRequest> & { password?: string | null };

@Component({
  selector: 'app-user-editor',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="space-y-6">
      <div class="flex items-center justify-between gap-4">
        <div>
          <h2 class="text-xl font-semibold text-slate-900">{{ title }}</h2>
        </div>
        <button type="button" class="rounded-full bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-200" (click)="cancel.emit()">Cancel</button>
      </div>

      <div class="grid gap-4">
        <div class="grid gap-2">
          <label class="text-sm font-medium text-slate-700">Username</label>
          <input
            type="text"
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
            [value]="form().username"
            (input)="updateField('username', $any($event.target).value)"
            required
          />
        </div>

        <div class="grid gap-2">
          <label class="text-sm font-medium text-slate-700">Email</label>
          <input
            type="email"
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
            [value]="form().email"
            (input)="updateField('email', $any($event.target).value)"
            required
          />
        </div>

        <div class="grid gap-2" *ngIf="mode === 'create'">
          <label class="text-sm font-medium text-slate-700">Password</label>
          <input
            type="password"
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
            [value]="form().password ?? ''"
            (input)="updateField('password', $any($event.target).value)"
            required
          />
        </div>

        <div class="grid gap-4 md:grid-cols-2">
          <div class="grid gap-2">
            <label class="text-sm font-medium text-slate-700">First name</label>
            <input
              type="text"
              class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
              [value]="form().firstName ?? ''"
              (input)="updateField('firstName', $any($event.target).value || null)"
            />
          </div>
          <div class="grid gap-2">
            <label class="text-sm font-medium text-slate-700">Last name</label>
            <input
              type="text"
              class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
              [value]="form().lastName ?? ''"
              (input)="updateField('lastName', $any($event.target).value || null)"
            />
          </div>
        </div>

        <div class="grid gap-2">
          <label class="text-sm font-medium text-slate-700">Phone number</label>
          <input
            type="tel"
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
            [value]="form().phoneNumber ?? ''"
            (input)="updateField('phoneNumber', $any($event.target).value || null)"
          />
        </div>

        <div class="grid gap-2">
          <label class="text-sm font-medium text-slate-700">Roles</label>
          <input
            type="text"
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
            [value]="form().roles?.join(', ') ?? ''"
            (input)="updateRoles($any($event.target).value)"
            placeholder="admin, user"
          />
        </div>

        <div class="flex items-center gap-3">
          <label class="inline-flex items-center gap-3 text-sm text-slate-600">
            <input
              type="checkbox"
              class="h-5 w-5 rounded border-slate-300 text-sky-600 focus:ring-sky-500"
              [checked]="form().isActive ?? true"
              (change)="updateField('isActive', $any($event.target).checked)"
            />
            Active account
          </label>
        </div>

        <div class="space-y-4 rounded-3xl border border-slate-200 bg-slate-50 p-4">
          <div class="flex items-center justify-between">
            <h3 class="text-base font-semibold text-slate-900">Claims</h3>
            <button type="button" class="rounded-full bg-slate-100 px-3 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-200" (click)="addClaim()">Add claim</button>
          </div>
          <div class="space-y-3">
            <div *ngFor="let claim of form().claims ?? []; index as i" class="grid gap-3 md:grid-cols-[1fr_1fr_auto]">
              <input
                type="text"
                class="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
                [value]="claim.claimType"
                (input)="updateClaim(i, 'claimType', $any($event.target).value)"
                placeholder="Claim type"
              />
              <input
                type="text"
                class="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
                [value]="claim.claimValue"
                (input)="updateClaim(i, 'claimValue', $any($event.target).value)"
                placeholder="Claim value"
              />
              <button type="button" class="rounded-full bg-rose-100 px-4 py-3 text-sm font-semibold text-rose-700 transition hover:bg-rose-200" (click)="removeClaim(i)">Remove</button>
            </div>
          </div>
        </div>
      </div>

      <div class="flex justify-end">
        <button type="button" class="rounded-full bg-sky-600 px-6 py-3 text-sm font-semibold text-white transition hover:bg-sky-700" (click)="submitForm()">{{ submitLabel }}</button>
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
