import { Component, Input } from '@angular/core';
import { UiIconComponent } from './ui-icon.component';

@Component({
  selector: 'ui-button',
  standalone: true,
  imports: [UiIconComponent],
  host: { style: 'display: contents' },
  template: `
    <button
      [type]="type"
      [disabled]="disabled"
      [class]="computedClasses"
      (click)="onClick()">
        @if (icon) {
          <ui-icon [name]="icon" class="h-4 w-4 shrink-0"></ui-icon>
        }
        <ng-content></ng-content>
    </button>
  `
})
export class UiButtonComponent {
  @Input() variant: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link' = 'default';
  @Input() size: 'default' | 'sm' | 'lg' | 'icon' = 'default';
  @Input() icon?: string;
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() disabled = false;
  @Input() class = '';

  get computedClasses(): string {
    let base = 'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 cursor-pointer ';

    switch (this.variant) {
      case 'default': base += 'bg-primary text-primary-foreground hover:bg-primary/90 '; break;
      case 'destructive': base += 'bg-destructive text-destructive-foreground hover:bg-destructive/90 '; break;
      case 'outline': base += 'border border-input bg-background hover:bg-accent hover:text-accent-foreground '; break;
      case 'secondary': base += 'bg-secondary text-secondary-foreground hover:bg-secondary/80 '; break;
      case 'ghost': base += 'hover:bg-accent hover:text-accent-foreground '; break;
      case 'link': base += 'text-primary underline-offset-4 hover:underline '; break;
    }

    switch (this.size) {
      case 'default': base += 'h-10 px-4 py-2 '; break;
      case 'sm': base += 'h-9 rounded-md px-3 '; break;
      case 'lg': base += 'h-11 rounded-md px-8 '; break;
      case 'icon': base += 'h-10 w-10 '; break;
    }

    return base + this.class;
  }

  onClick() {
    // Allows Angular click event binding natively via standard bubbling
  }
}
