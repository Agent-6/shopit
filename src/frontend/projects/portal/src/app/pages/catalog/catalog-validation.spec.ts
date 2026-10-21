import { HttpErrorResponse } from '@angular/common/http';
import {
  fieldErrorsFromProblem,
  fieldForErrorCode,
  toCatalogFailure,
} from './catalog-validation';
import { MoneyPipe } from './money.pipe';

/**
 * The catalog editor's error placement, pinned without a component or a running API.
 *
 * These are the rules the acceptance criteria depend on: a field-level failure lands under its input, a 409
 * duplicate SKU keeps its code so the editor can add the "choose a different one" explanation, and a failure
 * the portal cannot place is still shown rather than swallowed.
 */
describe('catalog validation', () => {
  it('maps a domain error code to the field that produced it', () => {
    expect(fieldForErrorCode('Product.SkuAlreadyExists')).toBe('sku');
    expect(fieldForErrorCode('Product.NameTooLong')).toBe('name');
    expect(fieldForErrorCode('Product.NegativePrice')).toBe('price');
    expect(fieldForErrorCode('Product.CategoryNotFound')).toBe('categoryId');
    expect(fieldForErrorCode('Category.NameAlreadyExists')).toBe('name');
    expect(fieldForErrorCode('Product.TenantRequired')).toBeNull();
    expect(fieldForErrorCode(undefined)).toBeNull();
  });

  it('explains a 409 duplicate SKU under the sku field and keeps the code', () => {
    const response = new HttpErrorResponse({
      status: 409,
      error: {
        status: 409,
        code: 'Product.SkuAlreadyExists',
        detail: "A product with the SKU 'TEE-001' already exists.",
      },
    });

    const failure = toCatalogFailure(response, 'Unable to save the product.');

    expect(failure.ok).toBe(false);
    expect(failure.status).toBe(409);
    expect(failure.code).toBe('Product.SkuAlreadyExists');
    expect(failure.fields['sku']).toEqual(["A product with the SKU 'TEE-001' already exists."]);
    expect(failure.message).toContain('TEE-001');
  });

  it('honours a ProblemDetails errors map and normalises field keys', () => {
    const fields = fieldErrorsFromProblem({
      code: 'General.Validation',
      detail: 'One or more validation errors occurred.',
      errors: {
        Name: ['A product name is required.'],
        'Price.Amount': 'A price cannot have more than 2 decimal places.',
      },
    });

    expect(fields['name']).toEqual(['A product name is required.']);
    expect(fields['price']).toEqual(['A price cannot have more than 2 decimal places.']);
  });

  it('keeps an unplaceable failure visible as a general message', () => {
    const response = new HttpErrorResponse({
      status: 403,
      error: {
        status: 403,
        code: 'Product.TenantRequired',
        detail: 'The catalog is tenant-scoped: a product cannot be read or changed without a tenant.',
      },
    });

    const failure = toCatalogFailure(response, 'Unable to save the product.');

    expect(failure.fields).toEqual({});
    expect(failure.message).toContain('tenant-scoped');
  });

  it('does not render a gateway HTML error page as a message', () => {
    const response = new HttpErrorResponse({
      status: 502,
      error: '<html><body>Bad gateway</body></html>',
    });

    const failure = toCatalogFailure(response, 'Unable to save the product.');

    expect(failure.message).toBe('The catalog service reported an error. Please try again.');
  });
});

describe('money pipe', () => {
  it('renders a missing price as a dash rather than throwing', () => {
    expect(new MoneyPipe().transform(null)).toBe('—');
  });

  it('falls back to the raw amount for a currency Intl does not know', () => {
    // Never crash a list row over a malformed currency; show what the API stored instead.
    expect(new MoneyPipe().transform({ amount: '19.99', currency: 'NOTACODE' })).toBe('19.99 NOTACODE');
  });
});
