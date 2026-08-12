import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-page-filters',
  standalone: true,
  template: `
    <section class="rounded-lg border bg-card text-card-foreground p-5 shadow-sm">
      <ng-content></ng-content>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageFiltersComponent {}
