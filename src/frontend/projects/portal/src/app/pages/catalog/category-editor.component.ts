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
import { Category, CategoryWriteRequest } from './catalog.model';
import { CatalogFailure } from './catalog-validation';
import { UiButtonComponent } from '../../shared/components/ui-button.component';

/**
 * The create/rename category form, as a native modal dialog (see `product-editor.component.ts` for why the
 * platform dialog is used rather than a hand-built overlay).
 *
 * Category errors are single-field — the name — so the only mapping needed is the failure's `code` to the
 * name input; `catalog-validation.ts` does it for both editors from one table.
 */
@Component({
  selector: 'app-category-editor',
  standalone: true,
  imports: [UiButtonComponent],
  templateUrl: './category-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryEditorComponent implements OnInit {
  readonly model = input<Category | null>(null);
  readonly failure = input<CatalogFailure | null>(null);
  readonly busy = input(false);

  readonly submit = output<CategoryWriteRequest>();
  readonly cancel = output<void>();

  protected readonly title = computed(() =>
    this.model() ? `Rename ${this.model()!.name}` : 'New category'
  );
  protected readonly submitLabel = computed(() =>
    this.model() ? 'Save changes' : 'Create category'
  );

  protected readonly name = signal('');

  protected readonly fieldClass =
    'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50';

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly firstField = viewChild<ElementRef<HTMLInputElement>>('firstField');
  private dialogElement: HTMLDialogElement | null = null;

  constructor() {
    afterNextRender(() => {
      this.dialogElement = this.dialog().nativeElement;

      if (!this.dialogElement.open) {
        this.dialogElement.showModal();
      }

      this.firstField()?.nativeElement.focus();
    });

    inject(DestroyRef).onDestroy(() => {
      if (this.dialogElement?.open) {
        this.dialogElement.close();
      }
    });

    // A rejected name moves focus back to the input the message is rendered under.
    effect(() => {
      const failure = this.failure();
      if (!failure || Object.keys(failure.fields).length === 0) {
        return;
      }

      queueMicrotask(() => this.focusFirstError());
    });
  }

  ngOnInit(): void {
    this.name.set(this.model()?.name ?? '');
  }

  protected messagesFor(field: string): string[] {
    return this.failure()?.fields[field] ?? [];
  }

  protected hasError(field: string): boolean {
    return this.messagesFor(field).length > 0;
  }

  /** Shown only when the failure belongs to no field, so a field error is never repeated in a banner. */
  protected readonly generalFailure = computed(() => {
    const failure = this.failure();
    if (!failure) {
      return null;
    }

    return Object.keys(failure.fields).length === 0 ? failure.message : null;
  });

  /** The 409's explanation: the API says the name is taken, this says what to do about it. */
  protected readonly duplicateName = computed(
    () => this.failure()?.code === 'Category.NameAlreadyExists'
  );

  protected onSubmit(event: Event): void {
    event.preventDefault();

    // The submit button stays enabled while the request runs (disabling a focused button would drop focus to
    // the document), so a second submit is ignored here instead.
    if (this.busy()) {
      return;
    }

    this.submit.emit({ name: this.name().trim() });
  }

  protected onNativeCancel(event: Event): void {
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
