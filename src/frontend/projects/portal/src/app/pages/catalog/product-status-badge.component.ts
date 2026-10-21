import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ProductStatus } from './catalog.model';

/**
 * A product's lifecycle status as a text badge.
 *
 * The status is always spelled out: colour is a supplement, so the badge still reads correctly for someone
 * who cannot distinguish the three tints.
 */
@Component({
  selector: 'app-product-status-badge',
  standalone: true,
  template: `
    @switch (status()) {
      @case ('Active') {
        <span
          class="inline-flex items-center rounded-full border border-green-500/30 bg-green-500/10 px-2.5 py-0.5 text-xs font-medium text-green-700 dark:text-green-500"
        >
          Active
        </span>
      }
      @case ('Archived') {
        <span
          class="inline-flex items-center rounded-full border border-muted bg-muted/40 px-2.5 py-0.5 text-xs font-medium text-muted-foreground"
        >
          Archived
        </span>
      }
      @default {
        <span
          class="inline-flex items-center rounded-full border border-amber-500/30 bg-amber-500/10 px-2.5 py-0.5 text-xs font-medium text-amber-700 dark:text-amber-500"
        >
          Draft
        </span>
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductStatusBadgeComponent {
  readonly status = input.required<ProductStatus>();
}
