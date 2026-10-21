import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';
import { PageFiltersComponent } from '../../core/components/page/page-filters.component';
import { PageHeaderComponent } from '../../core/components/page/page-header.component';
import {
  PageTableCellDirective,
  PageTableColumn,
  PageTableComponent,
} from '../../core/components/page/page-table.component';
import { UiButtonComponent } from '../../shared/components/ui-button.component';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { CatalogNavComponent } from './catalog-nav.component';
import { CatalogService } from './catalog.service';
import {
  Product,
  ProductSortBy,
  ProductSortOrder,
  ProductStatus,
  ProductWriteRequest,
} from './catalog.model';
import { CatalogFailure } from './catalog-validation';
import { MoneyPipe } from './money.pipe';
import { ProductEditorComponent } from './product-editor.component';
import { ProductStatusBadgeComponent } from './product-status-badge.component';

/**
 * The catalog's product list: search, category/status filters, sorting, paging, and the create/edit entry
 * points.
 *
 * Create and the row actions are gated on `catalog.write` and are *absent* without it, not disabled — a
 * disabled button still says "this exists and you may not have it", which is not the API's answer either (it
 * would be a 403). The route itself is already gated on `catalog.view`.
 *
 * The table's default columns are kept narrow (short status labels, a date-only "updated" value, icon row
 * actions) so a laptop-width window does not need horizontal scrolling; the only way to widen it is long
 * user-entered names, which truncate.
 */
@Component({
  selector: 'app-products-page',
  standalone: true,
  templateUrl: './products-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    CatalogNavComponent,
    MoneyPipe,
    PageFiltersComponent,
    PageHeaderComponent,
    PageTableComponent,
    PageTableCellDirective,
    ProductEditorComponent,
    ProductStatusBadgeComponent,
    UiButtonComponent,
    UiIconComponent,
  ],
})
export class ProductsPageComponent {
  protected readonly service = inject(CatalogService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;

  protected readonly editorOpen = signal(false);
  protected readonly editingProduct = signal<Product | null>(null);
  protected readonly editorFailure = signal<CatalogFailure | null>(null);
  protected readonly editorBusy = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected readonly canWrite = computed(() =>
    this.permissionService.has(ShopItPermissions.Catalog.Write)
  );

  /**
   * The default columns, plus the actions column only for writers.
   *
   * The name is a link to the detail page, so there is no separate "View" action competing for width.
   */
  protected readonly columns = computed<PageTableColumn[]>(() => {
    const columns: PageTableColumn[] = [
      { key: 'name', header: 'Name' },
      { key: 'sku', header: 'SKU' },
      { key: 'category', header: 'Category' },
      { key: 'price', header: 'Price' },
      { key: 'status', header: 'Status' },
      { key: 'updated', header: 'Updated' },
    ];

    if (this.canWrite()) {
      columns.push({ key: 'actions', header: 'Actions', align: 'right' });
    }

    return columns;
  });

  constructor() {
    this.service.loadProducts();
    this.service.ensureCategoryOptions();
  }

  protected trackById(_index: number, product: Product): string {
    return product.id;
  }

  // ------------------------------------------------------------------
  // Filters and paging
  // ------------------------------------------------------------------

  protected onSearchSubmit(event: Event): void {
    event.preventDefault();
    this.applyFilters();
  }

  protected onCategoryChange(value: string): void {
    this.service.categoryId.set(value || null);
    this.applyFilters();
  }

  protected onStatusChange(value: string): void {
    this.service.status.set(value ? (value as ProductStatus) : null);
    this.applyFilters();
  }

  protected onSortByChange(value: string): void {
    this.service.sortBy.set(value as ProductSortBy);
    this.service.loadProducts();
  }

  protected onSortOrderChange(value: string): void {
    this.service.sortOrder.set(value as ProductSortOrder);
    this.service.loadProducts();
  }

  protected resetFilters(): void {
    this.service.filter.set('');
    this.service.categoryId.set(null);
    this.service.status.set(null);
    this.service.sortBy.set('name');
    this.service.sortOrder.set('asc');
    this.applyFilters();
  }

  protected goToPage(page: number): void {
    this.service.page.set(Math.min(Math.max(1, page), this.service.pageCount()));
    this.service.loadProducts();
  }

  protected setPageSize(size: number): void {
    this.service.pageSize.set(size);
    this.service.page.set(1);
    this.service.loadProducts();
  }

  private applyFilters(): void {
    // A new filter can shrink the result below the current page; start from the first page instead of
    // showing an out-of-range one.
    this.service.page.set(1);
    this.service.loadProducts();
  }

  // ------------------------------------------------------------------
  // Editor
  // ------------------------------------------------------------------

  protected openCreate(): void {
    this.editingProduct.set(null);
    this.editorFailure.set(null);
    this.editorOpen.set(true);
  }

  protected editProduct(product: Product): void {
    this.editingProduct.set(product);
    this.editorFailure.set(null);
    this.editorOpen.set(true);
  }

  protected closeEditor(): void {
    this.editorOpen.set(false);
    this.editingProduct.set(null);
    this.editorFailure.set(null);
  }

  protected async saveProduct(request: ProductWriteRequest): Promise<void> {
    const editing = this.editingProduct();
    this.editorFailure.set(null);
    this.editorBusy.set(true);

    try {
      const result = editing
        ? await this.service.updateProduct(editing.id, request)
        : await this.service.createProduct(request);

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

  protected async archive(product: Product): Promise<void> {
    const confirmed = confirm(
      `Archive "${product.name}"? An archived product is withdrawn and can no longer be edited or restored.`
    );
    if (!confirmed) {
      return;
    }

    this.actionError.set(null);
    const result = await this.service.changeProductStatus(product.id, 'Archived');
    if (!result.ok) {
      this.actionError.set(result.message);
    }
  }
}
