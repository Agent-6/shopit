import { Pipe, PipeTransform } from '@angular/core';
import { Money } from './catalog.model';

/**
 * Renders a catalog price with `Intl.NumberFormat`, per ADR-0005.
 *
 * The amount stays a string end to end: it is only converted for the formatter, which needs a number, and the
 * result is never compared, summed, or sent back. That is why this is the *only* place in the catalog pages
 * where an amount becomes a number — a `parseFloat` in a calculation is what the string contract exists to
 * prevent.
 *
 * The currency comes from the API with each price rather than from a hard-coded code, so a change to the
 * catalog's configured currency needs no portal change.
 */
@Pipe({ name: 'money', standalone: true, pure: true })
export class MoneyPipe implements PipeTransform {
  // Intl formatters are expensive to build and change detection asks for the same currency repeatedly.
  private readonly formatters = new Map<string, Intl.NumberFormat>();

  transform(value: Money | null | undefined): string {
    if (!value || !value.amount || !value.currency) {
      return '—';
    }

    const formatter = this.formatterFor(value.currency);
    if (!formatter) {
      return `${value.amount} ${value.currency}`;
    }

    // Display-only conversion: the formatter cannot take a decimal string, and no arithmetic follows.
    return formatter.format(Number(value.amount));
  }

  private formatterFor(currency: string): Intl.NumberFormat | null {
    const cached = this.formatters.get(currency);
    if (cached) {
      return cached;
    }

    try {
      const formatter = new Intl.NumberFormat(undefined, { style: 'currency', currency });
      this.formatters.set(currency, formatter);
      return formatter;
    } catch {
      // An unknown or malformed currency code is shown verbatim rather than crashing the row.
      return null;
    }
  }
}
