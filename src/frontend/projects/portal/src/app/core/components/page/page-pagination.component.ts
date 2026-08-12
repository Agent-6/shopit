import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { UiButtonComponent } from '../../../shared/components/ui-button.component';

@Component({
  selector: 'app-page-pagination',
  standalone: true,
  imports: [UiButtonComponent],
  template: `
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between text-sm text-muted-foreground">
      <p class="shrink-0">Page {{ page() }} of {{ pageCount() }}</p>

      <div class="flex flex-wrap items-center gap-3">
        @if (pageSizeOptions().length) {
          <label class="flex items-center gap-2">
            <span class="text-xs font-medium text-foreground">Rows per page</span>
            <select
              class="h-9 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              [value]="pageSize() ?? ''"
              [disabled]="disabled()"
              (change)="onPageSizeChange($any($event.target).value)">
              @for (size of pageSizeOptions(); track size) {
                <option [value]="size">{{ size }}</option>
              }
            </select>
          </label>
        }

        <div class="flex items-center gap-2">
          <ui-button variant="outline" size="sm" (click)="goTo(page() - 1)" [disabled]="disabled() || page() <= 1">
            Previous
          </ui-button>
          <ui-button variant="outline" size="sm" (click)="goTo(page() + 1)" [disabled]="disabled() || page() >= pageCount()">
            Next
          </ui-button>
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PagePaginationComponent {
  readonly page = input.required<number>();
  readonly pageCount = input.required<number>();
  readonly pageSize = input<number>();
  readonly pageSizeOptions = input<number[]>([]);
  readonly disabled = input(false);

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected goTo(page: number): void {
    this.pageChange.emit(page);
  }

  protected onPageSizeChange(value: string): void {
    this.pageSizeChange.emit(Number(value));
  }
}
