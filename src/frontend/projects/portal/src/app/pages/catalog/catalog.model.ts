/**
 * The catalog HTTP contract, as the Catalog API serialises it (#77–#79).
 *
 * Money keeps the API's shape — `{ "amount": "19.99", "currency": "EUR" }` with the amount as a *string* —
 * because ADR-0005 makes the string the contract. Nothing here turns an amount into a number except the
 * display pipe, which never feeds a value back into a request.
 */

/** A price exactly as the API sends and accepts it. */
export interface Money {
  /** A decimal written with the invariant culture, for example `"19.99"`. */
  readonly amount: string;
  /** The ISO 4217 code, uppercase. */
  readonly currency: string;
}

/** A price in a request body; the currency may be omitted to accept the catalog's configured one. */
export interface MoneyRequest {
  readonly amount: string;
  readonly currency: string | null;
}

/** A product's lifecycle status; serialised by name, so it survives renumbering the backend enum. */
export type ProductStatus = 'Draft' | 'Active' | 'Archived';

/** One product as the list endpoint reports it. */
export interface Product {
  readonly id: string;
  readonly name: string;
  readonly sku: string;
  readonly description: string | null;
  readonly price: Money;
  readonly categoryId: string | null;
  readonly status: ProductStatus;
  readonly createdOn: string;
  readonly lastModifiedOn: string | null;
}

/** A single product, with the audit actors the detail endpoint reports (ADR-0004). */
export interface ProductDetail extends Product {
  readonly tenantId: string;
  /** The user id that created the product, or `null` when no interactive user did (seeding). */
  readonly createdBy: string | null;
  /** The user id that last modified it, or `null` if it never has been modified. */
  readonly lastModifiedBy: string | null;
}

/** A category as the category endpoints report it. */
export interface Category {
  readonly id: string;
  readonly name: string;
  readonly createdOn: string;
  readonly lastModifiedOn: string | null;
}

/** The subset of a category a filter dropdown or a name lookup needs. */
export interface CategoryOption {
  readonly id: string;
  readonly name: string;
}

/** A page of results, with the counts paging needs. */
export interface CatalogPage<T> {
  readonly items: T[];
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

/** The sort keys the product list accepts; unknown keys are a 400 rather than a silent default. */
export type ProductSortBy = 'name' | 'sku' | 'price' | 'createdOn';

/** The sort directions the product list accepts. */
export type ProductSortOrder = 'asc' | 'desc';

/** The body of a create- or update-product request. An update replaces every editable field. */
export interface ProductWriteRequest {
  readonly name: string;
  readonly sku: string;
  readonly description: string | null;
  readonly price: MoneyRequest;
  readonly categoryId: string | null;
}

/** The body of a create- or rename-category request. */
export interface CategoryWriteRequest {
  readonly name: string;
}

/** The body of a status-change request. */
export interface ChangeProductStatusRequest {
  readonly status: ProductStatus;
}
