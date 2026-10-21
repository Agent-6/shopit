import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { CatalogService } from './catalog.service';
import { ProductStatus, ProductWriteRequest } from './catalog.model';
import { CatalogFailure } from './catalog-validation';
import { MoneyPipe } from './money.pipe';
import { ProductEditorComponent } from './product-editor.component';
import { ProductStatusBadgeComponent } from './product-status-badge.component';

/**
 * A single product, with the audit trail the API reports and the lifecycle actions.
 *
 * Audit actors are user ids: the catalog read reports `createdBy` / `lastModifiedBy` as `Guid`s (ADR-0004),
 * and resolving them to display names would be an Identity read that needs `user.view` — a catalog reader
 * need not hold it. The page therefore labels them as ids instead of guessing or omitting "who".
 *
 * Writes are absent without `catalog.write`; an archived product has no edit action even for a writer,
 * because archive is terminal and the API would refuse the update with a 409.
 */
@Component({
  selector: 'app-product-detail',
  standalone: true,
  templateUrl: './product-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    MoneyPipe,
    ProductEditorComponent,
    ProductStatusBadgeComponent,
    UiButtonComponent,
    UiIconComponent,
  ],
})
export class ProductDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly productId: string;

  protected readonly service = inject(CatalogService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;

  protected readonly product = this.service.product;

  protected readonly editorOpen = signal(false);
  protected readonly editorFailure = signal<CatalogFailure | null>(null);
  protected readonly editorBusy = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected readonly canWrite = computed(() =>
    this.permissionService.has(ShopItPermissions.Catalog.Write)
  );
  protected readonly canEdit = computed(
    () => this.canWrite() && this.product()?.status !== 'Archived'
  );
  protected readonly canActivate = computed(
    () => this.canWrite() && this.product()?.status === 'Draft'
  );
  protected readonly canArchive = computed(
    () => this.canWrite() && this.product() !== null && this.product()?.status !== 'Archived'
  );

  protected readonly categoryName = computed(() =>
    this.service.categoryName(this.product()?.categoryId ?? null)
  );

  constructor() {
    this.productId = this.route.snapshot.params['id'] as string;

    if (this.productId) {
      this.service.loadProduct(this.productId);
      this.service.ensureCategoryOptions();
    }
  }

  // ------------------------------------------------------------------
  // Editor
  // ------------------------------------------------------------------

  protected openEditor(): void {
    this.editorFailure.set(null);
    this.editorOpen.set(true);
  }

  protected closeEditor(): void {
    this.editorOpen.set(false);
    this.editorFailure.set(null);
  }

  protected async saveProduct(request: ProductWriteRequest): Promise<void> {
    this.editorFailure.set(null);
    this.editorBusy.set(true);

    try {
      const result = await this.service.updateProduct(this.productId, request);

      if (!result.ok) {
        this.editorFailure.set(result);
        return;
      }

      this.closeEditor();
    } finally {
      this.editorBusy.set(false);
    }
  }

  // ------------------------------------------------------------------
  // Lifecycle
  // ------------------------------------------------------------------

  protected async changeStatus(status: ProductStatus): Promise<void> {
    const product = this.product();
    if (!product) {
      return;
    }

    if (status === 'Archived') {
      const confirmed = confirm(
        `Archive "${product.name}"? An archived product is withdrawn and can no longer be edited or restored.`
      );
      if (!confirmed) {
        return;
      }
    }

    this.actionError.set(null);
    const result = await this.service.changeProductStatus(product.id, status);
    if (!result.ok) {
      this.actionError.set(result.message);
    }
  }
}
