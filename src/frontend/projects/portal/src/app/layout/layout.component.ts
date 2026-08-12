import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar/sidebar.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent],
  template: `
    <div class="flex h-screen bg-background overflow-hidden relative w-full text-foreground">
      <app-sidebar class="z-20"></app-sidebar>
      <main class="flex-1 overflow-auto bg-muted/30 relative w-full">
        <div class="w-full max-w-6xl mx-auto p-6 md:p-10 animate-in fade-in slide-in-from-bottom-4 duration-500">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `
})
export class LayoutComponent { }
