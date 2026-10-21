import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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
import { Category, CategoryWriteRequest } from './catalog.model';
import { CatalogFailure } from './catalog-validation';
import { CatalogNavComponent } from './catalog-nav.component';
import { CatalogService } from './catalog.service';
import { CategoryEditorComponent } from './category-editor.component';

/**
 * Category management: the paged list, a count of the products each category holds, and create/rename/delete.
 *
 * The product count is what decides whether delete is offered. The API refuses a delete while products still
 * reference the category (409), so a delete button there would only produce an error; the count is loaded per
 * page (see `CatalogService.loadCategoryProductCounts`) and "unknown" is treated as "in use" so the UI never
 * offers a delete it cannot justify.
 */
@Component({
  selector: 'app-categories-page',
  standalone: true,
  templateUrl: './categories-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    CatalogNavComponent,
    CategoryEditorComponent,
    PageFiltersComponent,
    PageHeaderComponent,
    PageTableComponent,
    PageTableCellDirective,
    UiButtonComponent,
    UiIconComponent,
  ],
})
export class CategoriesPageComponent {
  protected readonly service = inject(CatalogService);
  protected readonly permissionService = inject(PermissionService);
  protected readonly perms = ShopItPermissions;

  protected readonly editorOpen = signal(false);
  protected readonly editingCategory = signal<Category | null>(null);
  protected readonly editorFailure = signal<CatalogFailure | null>(null);
  protected readonly editorBusy = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected readonly canWrite = computed(() =>
    this.permissionService.has(ShopItPermissions.Catalog.Write)
  );

  protected readonly columns = computed<PageTableColumn[]>(() => {
    const columns: PageTableColumn[] = [
      { key: 'name', header: 'Name' },
      { key: 'products', header: 'Products' },
      { key: 'created', header: 'Created' },
    ];

    if (this.canWrite()) {
      columns.push({ key: 'actions', header: 'Actions', align: 'right' });
    }

    return columns;
  });

  constructor() {
    this.service.loadCategories();
  }

  protected trackById(_index: number, category: Category): string {
    return category.id;
  }

  // ------------------------------------------------------------------
  // Filters and paging
  // ------------------------------------------------------------------

  protected onSearchSubmit(event: Event): void {
    event.preventDefault();
    this.service.categoryPage.set(1);
    this.service.loadCategories();
  }

  protected resetFilters(): void {
    this.service.categoryFilter.set('');
    this.service.categoryPage.set(1);
    this.service.loadCategories();
  }

  protected goToPage(page: number): void {
    this.service.categoryPage.set(
      Math.min(Math.max(1, page), this.service.categoryPageCount())
    );
    this.service.loadCategories();
  }

  protected setPageSize(size: number): void {
    this.service.categoryPageSize.set(size);
    this.service.categoryPage.set(1);
    this.service.loadCategories();
  }

  // ------------------------------------------------------------------
  // Editor
  // ------------------------------------------------------------------

  protected openCreate(): void {
    this.editingCategory.set(null);
    this.editorFailure.set(null);
    this.editorOpen.set(true);
  }

  protected openRename(category: Category): void {
    this.editingCategory.set(category);
    this.editorFailure.set(null);
    this.editorOpen.set(true);
  }

  protected closeEditor(): void {
    this.editorOpen.set(false);
    this.editingCategory.set(null);
    this.editorFailure.set(null);
  }

  protected async saveCategory(request: CategoryWriteRequest): Promise<void> {
    const editing = this.editingCategory();
    this.editorFailure.set(null);
    this.editorBusy.set(true);

    try {
      const result = editing
        ? await this.service.renameCategory(editing.id, request)
        : await this.service.createCategory(request);

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
  // Delete
  // ------------------------------------------------------------------

  protected canDelete(category: Category): boolean {
    // Only a known zero is safe: a category with products cannot be deleted, and an unknown count must not
    // be read as zero.
    return this.service.categoryProductCount(category.id) === 0;
  }

  protected async deleteCategory(category: Category): Promise<void> {
    if (!this.canDelete(category)) {
      return;
    }

    const confirmed = confirm(
      `Delete the category "${category.name}"? Products must be recategorised first.`
    );
    if (!confirmed) {
      return;
    }

    this.actionError.set(null);
    const result = await this.service.deleteCategory(category.id);
    if (!result.ok) {
      this.actionError.set(result.message);
    }
  }
}
