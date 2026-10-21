import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns the catalog API's failures into something an editor can render *next to a field*.
 *
 * The API answers a failure with `ProblemDetails`: a `code`, a human `detail`, and — when a validation layer
 * reports several fields at once — an `errors` map keyed by field name. The catalog's domain errors are
 * single failures, so they arrive as a `code` with no `errors` map; {@link ERROR_CODE_FIELDS} is the mapping
 * from those codes to the form field that produced them, and it is the only place that knowledge lives.
 *
 * Kept as pure functions, with no Angular state, so the mapping can be unit-tested without a component or a
 * running API.
 */

/** The `ProblemDetails` members this portal reads. Unknown members are ignored on purpose. */
export interface ProblemDetails {
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly code?: string;
  readonly errors?: Readonly<Record<string, readonly string[] | string>>;
}

/** Messages per form field, ready for `aria-describedby`. */
export type FieldErrors = Record<string, string[]>;

/** A failed catalog request, in a shape a component can render without catching anything. */
export interface CatalogFailure {
  readonly ok: false;
  /** The API's own wording when it sent one, otherwise a message this portal chose. */
  readonly message: string;
  readonly status: number;
  /** The API's error code, for example `Product.SkuAlreadyExists`. */
  readonly code?: string;
  /** Field-level messages; empty when the failure is not about a specific field. */
  readonly fields: FieldErrors;
}

/** The result of a catalog write: either the API's value or a renderable failure. */
export type CatalogResult<T> = { readonly ok: true; readonly value: T } | CatalogFailure;

/**
 * The field each catalog error code belongs to, keyed by code.
 *
 * A code that is not listed here is still shown — in the editor's general error banner — rather than
 * swallowed: an unknown validation error the portal cannot place is better than a silent failed save.
 */
const ERROR_CODE_FIELDS: Readonly<Record<string, string>> = {
  'Product.NameRequired': 'name',
  'Product.NameTooLong': 'name',
  'Product.SkuRequired': 'sku',
  'Product.SkuTooLong': 'sku',
  'Product.SkuInvalidCharacters': 'sku',
  'Product.SkuAlreadyExists': 'sku',
  'Product.DescriptionTooLong': 'description',
  'Product.PriceRequired': 'price',
  'Product.NegativePrice': 'price',
  'Product.InvalidPriceScale': 'price',
  'Product.InvalidCurrency': 'price',
  'Product.CurrencyMismatch': 'price',
  'Product.CategoryIdEmpty': 'categoryId',
  'Product.CategoryNotFound': 'categoryId',
  'Category.NameRequired': 'name',
  'Category.NameTooLong': 'name',
  'Category.NameAlreadyExists': 'name',
};

/** The form field a catalog error code belongs to, or `null` when it is not a field failure. */
export function fieldForErrorCode(code: string | undefined): string | null {
  if (!code) {
    return null;
  }

  return ERROR_CODE_FIELDS[code] ?? null;
}

/**
 * Reads the `ProblemDetails` body from an HTTP failure, or `null` when the body is not an object.
 *
 * A proxy or gateway error page is HTML, not `ProblemDetails`; treating it as one would render markup in a
 * message.
 */
export function problemDetailsFrom(error: unknown): ProblemDetails | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const body = error.error as unknown;
  return body !== null && typeof body === 'object' ? (body as ProblemDetails) : null;
}

/**
 * Builds the field messages from a `ProblemDetails` body.
 *
 * `errors` is honoured first because it is the API speaking in its own field vocabulary. Keys are normalised
 * to camelCase so `Name` and `name` land on the same field, and a dotted key (`Price.Amount`) is reduced to
 * its root (`price`) — the field the editor actually renders. When the body has no `errors` map, the single
 * `code` is mapped through {@link ERROR_CODE_FIELDS} so a domain failure still appears under its input.
 */
export function fieldErrorsFromProblem(problem: ProblemDetails | null): FieldErrors {
  const fields: FieldErrors = {};

  if (!problem) {
    return fields;
  }

  for (const [key, value] of Object.entries(problem.errors ?? {})) {
    const field = normalizeFieldName(key);
    const messages = (Array.isArray(value) ? value : [value]).filter(
      (message): message is string => typeof message === 'string' && message.trim().length > 0
    );

    if (messages.length > 0) {
      fields[field] = [...(fields[field] ?? []), ...messages];
    }
  }

  const codeField = fieldForErrorCode(problem.code);
  if (codeField && problem.detail && !fields[codeField]?.length) {
    fields[codeField] = [problem.detail];
  }

  return fields;
}

/**
 * Turns any thrown HTTP failure into a {@link CatalogFailure}.
 *
 * @param error The thrown value, normally an `HttpErrorResponse`.
 * @param fallback A portal message used when the API did not send a usable detail.
 */
export function toCatalogFailure(error: unknown, fallback: string): CatalogFailure {
  const problem = problemDetailsFrom(error);
  const status = error instanceof HttpErrorResponse ? error.status : 0;

  return {
    ok: false,
    message: problem?.detail?.trim() || fallbackForStatus(status) || fallback,
    status,
    ...(problem?.code ? { code: problem.code } : {}),
    fields: fieldErrorsFromProblem(problem),
  };
}

/**
 * The message a *read* failure shows in a page banner.
 *
 * Reads have no field to attach to, so this is {@link toCatalogFailure}'s message; kept as its own function so
 * a page does not have to know about the failure shape.
 */
export function readFailureMessage(error: unknown, fallback: string): string {
  return toCatalogFailure(error, fallback).message;
}

function normalizeFieldName(key: string): string {
  const root = key.split('.')[0]?.trim() ?? key;

  return root.length === 0 ? key : root.charAt(0).toLowerCase() + root.slice(1);
}

function fallbackForStatus(status: number): string | null {
  switch (status) {
    case 0:
      return 'The request could not be sent. Check your connection and try again.';
    case 403:
      return 'You do not have permission to do that.';
    case 404:
      return 'That item no longer exists.';
    default:
      return status >= 500 ? 'The catalog service reported an error. Please try again.' : null;
  }
}
