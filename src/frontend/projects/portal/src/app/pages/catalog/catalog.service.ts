import { HttpClient, HttpParams } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CatalogPage,
  Category,
  CategoryOption,
  CategoryWriteRequest,
  Product,
  ProductDetail,
  ProductSortBy,
  ProductSortOrder,
  ProductStatus,
  ProductWriteRequest,
} from './catalog.model';
import {
  CatalogFailure,
  CatalogResult,
  readFailureMessage,
  toCatalogFailure,
} from './catalog-validation';

/**
 * The catalog pages' data access: the product list and detail, the category list, and the writes.
 *
 * State lives in signals so a page renders from the service without a subscription per component, matching the
 * users/roles pages. Reads keep their own loading/error/loaded signals because a page must be able to tell an
 * empty catalog from a failed request — the empty state is not an error state.
 *
 * Every URL is `${environment.apiUrl}/catalog/...`, the same shape the Identity-backed pages use; the catalog
 * API enforces `catalog.view` / `catalog.write` itself, so these methods do not try to be the gate.
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/catalog`;

  // ------------------------------------------------------------------
  // Product list
  // ------------------------------------------------------------------

  readonly products = signal<Product[]>([]);
  readonly productsLoading = signal(false);
  readonly productsError = signal<string | null>(null);
  /** True once at least one list read has completed, so the empty state is not shown while loading. */
  readonly productsLoaded = signal(false);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly filter = signal('');
  readonly categoryId = signal<string | null>(null);
  readonly status = signal<ProductStatus | null>(null);
  readonly sortBy = signal<ProductSortBy>('name');
  readonly sortOrder = signal<ProductSortOrder>('asc');
  readonly totalCount = signal(0);
  readonly totalPages = signal(1);

  readonly pageCount = computed(() => Math.max(1, this.totalPages()));

  /** Whether anything narrows the list; it decides between "no products yet" and "nothing matches". */
  readonly hasProductFilters = computed(
    () =>
      this.filter().trim().length > 0 ||
      this.categoryId() !== null ||
      this.status() !== null
  );

  // ------------------------------------------------------------------
  // Product detail
  // ------------------------------------------------------------------

  readonly product = signal<ProductDetail | null>(null);
  readonly productLoading = signal(false);
  readonly productError = signal<string | null>(null);

  // ------------------------------------------------------------------
  // Categories
  // ------------------------------------------------------------------

  /** The paged category list the management page renders. */
  readonly categories = signal<Category[]>([]);
  readonly categoriesLoading = signal(false);
  readonly categoriesError = signal<string | null>(null);
  readonly categoriesLoaded = signal(false);
  readonly categoryPage = signal(1);
  readonly categoryPageSize = signal(20);
  readonly categoryFilter = signal('');
  readonly categoryTotalCount = signal(0);
  readonly categoryTotalPages = signal(1);

  readonly categoryPageCount = computed(() => Math.max(1, this.categoryTotalPages()));

  /**
   * Products per category for the categories on the current page.
   *
   * The categories read does not carry a count, so each one is counted with a one-row `GET /products`
   * (`pageSize=1`) and its `totalCount`. The requests run in parallel and only for the visible page; a
   * category whose count could not be read is absent, and the page treats "unknown" as "do not offer delete".
   * The alternative — adding a count to the category DTO — is a catalog-service change this portal issue does
   * not make.
   */
  readonly categoryProductCounts = signal<Readonly<Record<string, number>>>({});
  readonly categoryCountsLoading = signal(false);

  /** All categories, for the product filter and for resolving a product's category name. */
  readonly categoryOptions = signal<CategoryOption[]>([]);
  readonly categoryOptionsError = signal<string | null>(null);
  private categoryOptionsLoading: Promise<void> | null = null;

  readonly categoryNames = computed(
    () => new Map(this.categoryOptions().map((category) => [category.id, category.name]))
  );

  // Guards against an older response landing after a newer one and overwriting it.
  private productsRequest = 0;
  private productRequest = 0;
  private categoriesRequest = 0;

  /**
   * The name of a category id, or `null` when the product is uncategorised.
   *
   * "Unknown id" and "uncategorised" render the same way on purpose: a category that was deleted between
   * reads is not something the list should invent a name for.
   */
  categoryName(categoryId: string | null): string | null {
    return categoryId ? (this.categoryNames().get(categoryId) ?? null) : null;
  }

  /** The product count for a category, or `null` while it is unknown. */
  categoryProductCount(categoryId: string): number | null {
    return this.categoryProductCounts()[categoryId] ?? null;
  }

  // ------------------------------------------------------------------
  // Product reads
  // ------------------------------------------------------------------

  async loadProducts(): Promise<void> {
    const request = ++this.productsRequest;
    this.productsLoading.set(true);
    this.productsError.set(null);

    let params = new HttpParams()
      .set('page', String(this.page()))
      .set('pageSize', String(this.pageSize()))
      .set('sortBy', this.sortBy())
      .set('sortOrder', this.sortOrder());

    if (this.filter().trim()) {
      params = params.set('filter', this.filter().trim());
    }
    if (this.categoryId()) {
      params = params.set('categoryId', this.categoryId()!);
    }
    if (this.status()) {
      params = params.set('status', this.status()!);
    }

    try {
      const result = await lastValueFrom(
        this.http.get<CatalogPage<Product>>(`${this.baseUrl}/products`, { params })
      );

      if (request !== this.productsRequest) {
        return;
      }

      this.products.set(result.items ?? []);
      this.totalCount.set(result.totalCount ?? 0);
      this.totalPages.set(result.totalPages ?? 1);
      this.productsLoaded.set(true);
    } catch (error) {
      if (request !== this.productsRequest) {
        return;
      }
      this.productsError.set(
        readFailureMessage(error, 'Unable to load products. Please try again.')
      );
    } finally {
      if (request === this.productsRequest) {
        this.productsLoading.set(false);
      }
    }
  }

  async loadProduct(productId: string): Promise<void> {
    const request = ++this.productRequest;
    this.productLoading.set(true);
    this.productError.set(null);

    try {
      const product = await lastValueFrom(
        this.http.get<ProductDetail>(`${this.baseUrl}/products/${productId}`)
      );

      if (request !== this.productRequest) {
        return;
      }

      this.product.set(product);
    } catch (error) {
      if (request !== this.productRequest) {
        return;
      }
      this.product.set(null);
      this.productError.set(
        readFailureMessage(error, 'Unable to load this product. Please try again.')
      );
    } finally {
      if (request === this.productRequest) {
        this.productLoading.set(false);
      }
    }
  }

  // ------------------------------------------------------------------
  // Product writes
  // ------------------------------------------------------------------

  async createProduct(request: ProductWriteRequest): Promise<CatalogResult<ProductDetail>> {
    try {
      const created = await lastValueFrom(
        this.http.post<ProductDetail>(`${this.baseUrl}/products`, request)
      );

      // The create response predates the audit stamping, so the list is re-read rather than patched.
      await this.loadProducts();
      return { ok: true, value: created };
    } catch (error) {
      return toCatalogFailure(error, 'Unable to create the product. Please check the form.');
    }
  }

  async updateProduct(
    productId: string,
    request: ProductWriteRequest
  ): Promise<CatalogResult<ProductDetail>> {
    try {
      const updated = await lastValueFrom(
        this.http.put<ProductDetail>(`${this.baseUrl}/products/${productId}`, request)
      );

      await this.loadProducts();
      // The update response carries the audit values from *before* the change (the stamping runs after the
      // handler), so a detail page that is open re-reads instead of showing an older "last modified".
      if (this.product()?.id === productId) {
        await this.loadProduct(productId);
      }
      return { ok: true, value: updated };
    } catch (error) {
      return toCatalogFailure(error, 'Unable to save the product. Please check the form.');
    }
  }

  async changeProductStatus(
    productId: string,
    status: ProductStatus
  ): Promise<CatalogResult<unknown>> {
    try {
      const updated = await lastValueFrom(
        this.http.put<unknown>(`${this.baseUrl}/products/${productId}/status`, { status })
      );

      await this.loadProducts();
      if (this.product()?.id === productId) {
        await this.loadProduct(productId);
      }
      return { ok: true, value: updated };
    } catch (error) {
      return toCatalogFailure(error, 'Unable to change the product status. Please try again.');
    }
  }

  // ------------------------------------------------------------------
  // Categories
  // ------------------------------------------------------------------

  /**
   * Loads every category for the product filter and for name lookups.
   *
   * Cached after the first success: a dropdown's vocabulary changes only when someone edits categories, and
   * the pages that need it reload after their own writes.
   */
  ensureCategoryOptions(): Promise<void> {
    if (this.categoryOptions().length > 0) {
      return Promise.resolve();
    }

    const existing = this.categoryOptionsLoading;
    if (existing) {
      return existing;
    }

    const promise = lastValueFrom(
      this.http.get<CatalogPage<Category>>(`${this.baseUrl}/categories`, {
        // 100 is the API's maximum page size: a filter should offer the catalog's categories, not the first
        // twenty of them. A catalog larger than that needs a searchable category picker, not a longer list.
        params: new HttpParams().set('pageNumber', '1').set('pageSize', '100'),
      })
    )
      .then((result) => {
        this.categoryOptionsError.set(null);
        this.categoryOptions.set(
          (result.items ?? []).map((category) => ({ id: category.id, name: category.name }))
        );
      })
      .catch((error: unknown) => {
        // Not cached: a later visit to a page that needs the filter can retry.
        this.categoryOptionsLoading = null;
        this.categoryOptionsError.set(
          readFailureMessage(error, 'Unable to load the category filter. Please try again.')
        );
      });

    this.categoryOptionsLoading = promise;
    return promise;
  }

  async loadCategories(): Promise<void> {
    const request = ++this.categoriesRequest;
    this.categoriesLoading.set(true);
    this.categoriesError.set(null);

    let params = new HttpParams()
      .set('pageNumber', String(this.categoryPage()))
      .set('pageSize', String(this.categoryPageSize()));

    if (this.categoryFilter().trim()) {
      params = params.set('filter', this.categoryFilter().trim());
    }

    try {
      const result = await lastValueFrom(
        this.http.get<CatalogPage<Category>>(`${this.baseUrl}/categories`, { params })
      );

      if (request !== this.categoriesRequest) {
        return;
      }

      const items = result.items ?? [];
      this.categories.set(items);
      this.categoryTotalCount.set(result.totalCount ?? 0);
      this.categoryTotalPages.set(result.totalPages ?? 1);
      this.categoriesLoaded.set(true);

      await this.loadCategoryProductCounts(items.map((category) => category.id), request);
    } catch (error) {
      if (request !== this.categoriesRequest) {
        return;
      }
      this.categoriesError.set(
        readFailureMessage(error, 'Unable to load categories. Please try again.')
      );
    } finally {
      if (request === this.categoriesRequest) {
        this.categoriesLoading.set(false);
      }
    }
  }

  async createCategory(request: CategoryWriteRequest): Promise<CatalogResult<Category>> {
    try {
      const created = await lastValueFrom(
        this.http.post<Category>(`${this.baseUrl}/categories`, request)
      );

      await this.reloadCategoriesAfterWrite();
      return { ok: true, value: created };
    } catch (error) {
      return toCatalogFailure(error, 'Unable to create the category. Please check the name.');
    }
  }

  async renameCategory(
    categoryId: string,
    request: CategoryWriteRequest
  ): Promise<CatalogResult<Category>> {
    try {
      const updated = await lastValueFrom(
        this.http.put<Category>(`${this.baseUrl}/categories/${categoryId}`, request)
      );

      await this.reloadCategoriesAfterWrite();
      return { ok: true, value: updated };
    } catch (error) {
      return toCatalogFailure(error, 'Unable to rename the category. Please check the name.');
    }
  }

  async deleteCategory(categoryId: string): Promise<CatalogResult<void>> {
    try {
      await lastValueFrom(this.http.delete<void>(`${this.baseUrl}/categories/${categoryId}`));

      await this.reloadCategoriesAfterWrite();
      return { ok: true, value: undefined };
    } catch (error) {
      const failure: CatalogFailure = toCatalogFailure(
        error,
        'Unable to delete the category. Please try again.'
      );

      // A 409 means products still reference it: re-reading refreshes the count the page uses to decide
      // whether delete should be offered at all, so the next render shows the truth instead of a stale 0.
      if (failure.status === 409) {
        await this.reloadCategoriesAfterWrite();
      }
      return failure;
    }
  }

  /**
   * Re-reads the paged list and the dropdown after a category write.
   *
   * The dropdown is invalidated rather than patched so a rename or delete cannot leave a stale name in the
   * product filter.
   */
  private async reloadCategoriesAfterWrite(): Promise<void> {
    this.categoryOptionsLoading = null;
    this.categoryOptions.set([]);
    await Promise.all([this.loadCategories(), this.ensureCategoryOptions()]);
  }

  private async loadCategoryProductCounts(
    categoryIds: string[],
    request: number
  ): Promise<void> {
    this.categoryCountsLoading.set(true);
    const counts: Record<string, number> = {};

    await Promise.all(
      categoryIds.map(async (categoryId) => {
        try {
          const page = await lastValueFrom(
            this.http.get<CatalogPage<Product>>(`${this.baseUrl}/products`, {
              params: new HttpParams()
                .set('categoryId', categoryId)
                .set('page', '1')
                .set('pageSize', '1'),
            })
          );
          counts[categoryId] = page.totalCount ?? 0;
        } catch {
          // Count unknown: leave it out so the page does not offer a delete it cannot justify.
        }
      })
    );

    // A newer page load owns the rows now; its counts must not be replaced by this one's.
    if (request !== this.categoriesRequest) {
      return;
    }

    this.categoryProductCounts.set(counts);
    this.categoryCountsLoading.set(false);
  }
}
