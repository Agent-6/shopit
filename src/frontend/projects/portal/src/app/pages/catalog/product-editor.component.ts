import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  OnInit,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CategoryOption, Product, ProductWriteRequest } from './catalog.model';
import { CatalogFailure } from './catalog-validation';
import { UiButtonComponent } from '../../shared/components/ui-button.component';

interface ProductFormState {
  name: string;
  sku: string;
  description: string;
  amount: string;
  /** Sent back unchanged when editing; omitted on create so the catalog stamps its configured currency. */
  currency: string;
  categoryId: string;
}

/**
 * The create/edit product form, as a native modal dialog.
 *
 * `<dialog>` rather than a hand-built overlay: `showModal()` gives the focus trap, Escape handling and an
 * inert background from the platform, which is exactly the focus management a modal needs and is easy to get
 * subtly wrong by hand. The parent keeps ownership of *whether* the editor is open — Escape and Cancel both
 * emit `cancel` and the parent removes the element.
 *
 * Server failures arrive as a {@link CatalogFailure} input rather than being caught here: the page owns the
 * request, and this component only decides where each message goes. Field messages come from the API's
 * `ProblemDetails` (through `catalog-validation.ts`), so a rejected SKU is reported under the SKU input with
 * the API's own wording, not in a banner at the top.
 */
@Component({
  selector: 'app-product-editor',
  standalone: true,
  imports: [UiButtonComponent],
  templateUrl: './product-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductEditorComponent implements OnInit {
  readonly model = input<Product | null>(null);
  readonly categories = input<CategoryOption[]>([]);
  readonly failure = input<CatalogFailure | null>(null);
  readonly busy = input(false);

  readonly submit = output<ProductWriteRequest>();
  readonly cancel = output<void>();

  protected readonly title = computed(() =>
    this.model() ? `Edit ${this.model()!.name}` : 'New product'
  );
  protected readonly submitLabel = computed(() =>
    this.model() ? 'Save changes' : 'Create product'
  );

  /** The currency the price is stored in; shown in the price label so the amount is not ambiguous. */
  protected readonly currency = computed(
    () => this.model()?.price.currency ?? null
  );

  protected readonly form = signal<ProductFormState>({
    name: '',
    sku: '',
    description: '',
    amount: '',
    currency: '',
    categoryId: '',
  });

  protected readonly fieldClass =
    'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50';

  protected readonly textareaClass =
    'flex min-h-20 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50';

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly firstField = viewChild<ElementRef<HTMLInputElement>>('firstField');
  private dialogElement: HTMLDialogElement | null = null;

  constructor() {
    afterNextRender(() => {
      this.dialogElement = this.dialog().nativeElement;

      if (!this.dialogElement.open) {
        // `showModal`, not `show`: only the modal form makes the rest of the page inert and traps Tab.
        this.dialogElement.showModal();
      }

      // The native dialog focuses its first focusable descendant, which is the Cancel button in the header.
      // The name is what the user came to type, so it takes focus instead.
      this.firstField()?.nativeElement.focus();
    });

    // A dialog removed while open (a successful save closes the parent's `@if`) must be closed explicitly,
    // otherwise the top layer can keep swallowing pointer events.
    inject(DestroyRef).onDestroy(() => {
      if (this.dialogElement?.open) {
        this.dialogElement.close();
      }
    });

    // A rejected save that is about a specific field moves focus to the first one. The message is rendered
    // there, and a keyboard user should not have to tab around looking for what the API objected to.
    effect(() => {
      const failure = this.failure();
      if (!failure || Object.keys(failure.fields).length === 0) {
        return;
      }

      // The effect runs before the new `aria-invalid` bindings are in the DOM, so wait for the update.
      queueMicrotask(() => this.focusFirstError());
    });
  }

  ngOnInit(): void {
    // Inputs are set before ngOnInit, and the editor is recreated for each open (`@if` in the page), so this
    // is the right place to seed the form from the product being edited.
    const product = this.model();

    this.form.set({
      name: product?.name ?? '',
      sku: product?.sku ?? '',
      description: product?.description ?? '',
      amount: product?.price.amount ?? '',
      currency: product?.price.currency ?? '',
      categoryId: product?.categoryId ?? '',
    });
  }

  protected messagesFor(field: string): string[] {
    return this.failure()?.fields[field] ?? [];
  }

  protected hasError(field: string): boolean {
    return this.messagesFor(field).length > 0;
  }

  protected describedBy(...ids: Array<string | null>): string | null {
    const present = ids.filter((id): id is string => !!id);
    return present.length > 0 ? present.join(' ') : null;
  }

  /** Shown only when the failure belongs to no field, so a field error is never repeated in two places. */
  protected readonly generalFailure = computed(() => {
    const failure = this.failure();
    if (!failure) {
      return null;
    }

    return Object.keys(failure.fields).length === 0 ? failure.message : null;
  });

  /** The 409's explanation: the API says the SKU exists, this says what to do about it. */
  protected readonly duplicateSku = computed(
    () => this.failure()?.code === 'Product.SkuAlreadyExists'
  );

  protected updateField<Key extends keyof ProductFormState>(
    field: Key,
    value: ProductFormState[Key]
  ): void {
    this.form.update((current) => ({ ...current, [field]: value }));
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();

    // The submit button stays enabled while the request runs (disabling a focused button would drop focus to
    // the document), so a second submit is ignored here instead.
    if (this.busy()) {
      return;
    }

    const state = this.form();
    const description = state.description.trim();

    this.submit.emit({
      name: state.name.trim(),
      sku: state.sku.trim(),
      description: description.length > 0 ? description : null,
      price: {
        amount: state.amount.trim(),
        currency: state.currency.trim() ? state.currency.trim().toUpperCase() : null,
      },
      categoryId: state.categoryId || null,
    });
  }

  protected onNativeCancel(event: Event): void {
    // The parent owns "is the editor open": removing the element is the close, so the native one is skipped.
    event.preventDefault();
    this.cancel.emit();
  }

  private focusFirstError(): void {
    const dialog = this.dialogElement;
    if (!dialog) {
      return;
    }

    const invalid = dialog.querySelector<HTMLElement>('[aria-invalid="true"]');
    (invalid ?? this.firstField()?.nativeElement)?.focus();
  }

  protected close(): void {
    this.cancel.emit();
  }
}
