import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  TemplateRef,
  computed,
  contentChildren,
  inject,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { PagePaginationComponent } from './page-pagination.component';

export interface PageTableColumn {
  key: string;
  header: string;
  align?: 'left' | 'right';
}

/**
 * Place on an <ng-template> inside <app-page-table> to render a custom cell
 * for a specific column. The template context exposes the row as $implicit.
 *
 * @example
 * <app-page-table [columns]="columns" [rows]="users()">
 *   <ng-template appPageTableCell="actions" let-user>
 *     <button (click)="edit(user)">Edit</button>
 *   </ng-template>
 * </app-page-table>
 */
@Directive({
  selector: '[appPageTableCell]',
  standalone: true,
})
export class PageTableCellDirective {
  readonly columnKey = input.required<string>({ alias: 'appPageTableCell' });
  readonly templateRef = inject<TemplateRef<unknown>>(TemplateRef);
}

@Component({
  selector: 'app-page-table',
  standalone: true,
  imports: [NgTemplateOutlet, PagePaginationComponent],
  template: `
    <div class="overflow-x-auto rounded-lg border bg-card text-card-foreground shadow-sm">
      <table class="w-full text-sm">
        <thead class="border-b bg-muted/50">
          <tr class="text-left font-medium text-muted-foreground">
            @for (column of columns(); track column.key) {
              <th class="h-12 px-4 align-middle font-medium" [class.text-right]="column.align === 'right'">
                {{ column.header }}
              </th>
            }
          </tr>
        </thead>
        <tbody>
          @for (row of rows(); track trackRow($index, row)) {
            <tr class="border-b transition-colors hover:bg-muted/50">
              @for (column of columns(); track column.key) {
                <td class="p-4 align-middle" [class.text-right]="column.align === 'right'">
                  @if (templates().get(column.key); as cell) {
                    <ng-container
                      [ngTemplateOutlet]="cell"
                      [ngTemplateOutletContext]="{ $implicit: row, row: row, index: $index }"></ng-container>
                  } @else {
                    {{ getCellValue(row, column.key) }}
                  }
                </td>
              }
            </tr>
          }
        </tbody>
      </table>

      @if (showPagination()) {
        <div class="border-t px-4 py-3">
          <app-page-pagination
            [page]="page() ?? 1"
            [pageCount]="pageCount() ?? 1"
            [pageSize]="pageSize()"
            [pageSizeOptions]="pageSizeOptions()"
            [disabled]="disabled()"
            (pageChange)="pageChange.emit($event)"
            (pageSizeChange)="pageSizeChange.emit($event)"></app-page-pagination>
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageTableComponent<T> {
  readonly columns = input.required<PageTableColumn[]>();
  readonly rows = input.required<T[]>();
  readonly trackBy = input<(index: number, row: T) => unknown>();

  /** When bound, renders the pagination bar inside the table card. */
  readonly page = input<number>();
  readonly pageCount = input<number>();
  readonly pageSize = input<number>();
  readonly pageSizeOptions = input<number[]>([5, 10, 20]);
  readonly disabled = input(false);

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  private readonly cellDirectives = contentChildren(PageTableCellDirective);

  protected readonly templates = computed(() => {
    const map = new Map<string, TemplateRef<unknown>>();
    for (const cell of this.cellDirectives()) {
      map.set(cell.columnKey(), cell.templateRef);
    }
    return map;
  });

  protected readonly showPagination = computed(() => this.page() !== undefined);

  protected trackRow(index: number, row: T): unknown {
    return this.trackBy() ? this.trackBy()!(index, row) : index;
  }

  protected getCellValue(row: T, key: string): unknown {
    return (row as Record<string, unknown>)[key];
  }
}
