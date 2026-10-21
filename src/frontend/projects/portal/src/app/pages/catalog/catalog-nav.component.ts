import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/**
 * Section links shared by the catalog pages.
 *
 * Two routes rather than tabs on one page: each has its own title, filters and empty state, and a URL that
 * says which one is open. A tab would have to encode the same state in a query parameter to be linkable.
 */
@Component({
  selector: 'app-catalog-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav aria-label="Catalog sections" class="flex gap-1 border-b border-border">
      <a
        routerLink="/catalog/products"
        routerLinkActive="border-primary text-foreground"
        ariaCurrentWhenActive="page"
        class="border-b-2 border-transparent px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
      >
        Products
      </a>
      <a
        routerLink="/catalog/categories"
        routerLinkActive="border-primary text-foreground"
        ariaCurrentWhenActive="page"
        class="border-b-2 border-transparent px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
      >
        Categories
      </a>
    </nav>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogNavComponent {}
