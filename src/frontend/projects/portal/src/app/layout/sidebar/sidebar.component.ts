import { Component, HostListener, signal, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { UiIconComponent } from '../../shared/components/ui-icon.component';
import { ThemeService, Theme } from '../../shared/services/theme.service';
import { AuthService } from '../../core/auth/auth.service';
import { PermissionService } from '../../core/auth/permission.service';
import { ShopItPermissions } from '../../core/auth/permissions';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterModule, UiIconComponent],
  template: `
    <aside
      class="h-screen bg-card border-r border-border flex flex-col transition-all duration-300 relative group"
      [class.w-64]="isExpanded()"
      [class.w-[60px]]="!isExpanded()"
      (mouseenter)="onMouseEnter()"
      (mouseleave)="onMouseLeave()">

      <div class="h-14 flex items-center border-b border-border shrink-0 relative overflow-hidden transition-all duration-300"
           [class.px-3]="isExpanded()"
           [class.justify-center]="!isExpanded()">
        <div class="h-8 w-8 bg-primary text-primary-foreground rounded-lg flex items-center justify-center shrink-0">
          <ui-icon name="layout-dashboard" class="h-5 w-5"></ui-icon>
        </div>
        @if (isExpanded()) {
          <span class="font-bold text-lg ml-3 whitespace-nowrap opacity-100 transition-opacity duration-300">ShopIt</span>
        }

        @if (isExpanded()) {
          <button
                  (click)="togglePin()"
                  class="absolute right-2 p-1.5 rounded-md hover:bg-accent text-muted-foreground focus:outline-none transition-colors hidden sm:block">
            <ui-icon [name]="isPinned() ? 'panel-left-close' : 'panel-left-open'" class="h-4 w-4"></ui-icon>
          </button>
        }
      </div>

      <nav class="flex-1 overflow-y-auto py-4 flex flex-col gap-1 px-2">
        @if (permissionService.has(perms.Users.View)) {
        <a routerLink="/users"
           routerLinkActive="bg-accent text-accent-foreground"
           class="flex items-center rounded-md hover:bg-muted text-muted-foreground transition-all duration-300 overflow-hidden shrink-0 h-10"
           [class.gap-3]="isExpanded()"
           [class.px-2]="isExpanded()"
           [class.justify-center]="!isExpanded()"
           title="Users">
          <ui-icon name="users" class="h-5 w-5 shrink-0"></ui-icon>
          @if (isExpanded()) {
            <span class="font-medium whitespace-nowrap transition-opacity duration-300">Users</span>
          }
        </a>
        }

        @if (permissionService.has(perms.Roles.View)) {
        <a routerLink="/roles"
           routerLinkActive="bg-accent text-accent-foreground"
           class="flex items-center rounded-md hover:bg-muted text-muted-foreground transition-all duration-300 overflow-hidden shrink-0 h-10"
           [class.gap-3]="isExpanded()"
           [class.px-2]="isExpanded()"
           [class.justify-center]="!isExpanded()"
           title="Roles">
          <ui-icon name="shield" class="h-5 w-5 shrink-0"></ui-icon>
          @if (isExpanded()) {
            <span class="font-medium whitespace-nowrap transition-opacity duration-300">Roles</span>
          }
        </a>
        }

        @if (permissionService.has(perms.Roles.ManagePermissions)) {
        <a routerLink="/permissions"
           routerLinkActive="bg-accent text-accent-foreground"
           class="flex items-center rounded-md hover:bg-muted text-muted-foreground transition-all duration-300 overflow-hidden shrink-0 h-10"
           [class.gap-3]="isExpanded()"
           [class.px-2]="isExpanded()"
           [class.justify-center]="!isExpanded()"
           title="Permissions">
          <ui-icon name="shield-check" class="h-5 w-5 shrink-0"></ui-icon>
          @if (isExpanded()) {
            <span class="font-medium whitespace-nowrap transition-opacity duration-300">Permissions</span>
          }
        </a>
        }

        @if (permissionService.has(perms.Tenants.View)) {
        <a routerLink="/tenants"
           routerLinkActive="bg-accent text-accent-foreground"
           class="flex items-center rounded-md hover:bg-muted text-muted-foreground transition-all duration-300 overflow-hidden shrink-0 h-10"
           [class.gap-3]="isExpanded()"
           [class.px-2]="isExpanded()"
           [class.justify-center]="!isExpanded()"
           title="Tenants">
          <ui-icon name="building-2" class="h-5 w-5 shrink-0"></ui-icon>
          @if (isExpanded()) {
            <span class="font-medium whitespace-nowrap transition-opacity duration-300">Tenants</span>
          }
        </a>
        }
      </nav>

      <div class="p-2 border-t border-border shrink-0 flex flex-col gap-2 relative overflow-hidden">

        <!-- Theme Controls (visible when expanded) -->
        @if (isExpanded()) {
          <div class="flex border border-border rounded-md p-1 mb-2 bg-muted/50">
            <button (click)="setTheme('light')"
                    [class.bg-background]="currentTheme() === 'light'"
                    [class.shadow-sm]="currentTheme() === 'light'"
                    [class.text-muted-foreground]="currentTheme() !== 'light'"
                    class="flex-1 flex justify-center p-1.5 rounded text-foreground hover:bg-background/80 transition-all">
              <ui-icon name="sun" class="h-4 w-4"></ui-icon>
            </button>
            <button (click)="setTheme('system')"
                    [class.bg-background]="currentTheme() === 'system'"
                    [class.shadow-sm]="currentTheme() === 'system'"
                    [class.text-muted-foreground]="currentTheme() !== 'system'"
                    class="flex-1 flex justify-center p-1.5 rounded hover:bg-background/80 transition-all">
              <ui-icon name="monitor" class="h-4 w-4"></ui-icon>
            </button>
            <button (click)="setTheme('dark')"
                    [class.bg-background]="currentTheme() === 'dark'"
                    [class.shadow-sm]="currentTheme() === 'dark'"
                    [class.text-muted-foreground]="currentTheme() !== 'dark'"
                    class="flex-1 flex justify-center p-1.5 rounded hover:bg-background/80 transition-all">
              <ui-icon name="moon" class="h-4 w-4"></ui-icon>
            </button>
          </div>
        }

        <!-- User profile area -->
        <div class="flex items-center w-full transition-all duration-300"
             [class.gap-3]="isExpanded()"
             [class.px-1]="isExpanded()"
             [class.justify-center]="!isExpanded()">
          <div class="h-8 w-8 bg-primary/10 text-primary border border-primary/20 rounded-full flex items-center justify-center shrink-0 font-medium text-xs">
            {{ userInitial }}
          </div>

          @if (isExpanded()) {
            <div class="flex-1 overflow-hidden transition-opacity duration-300">
              <div class="text-sm font-medium leading-none truncate">{{ userName }}</div>
              <div class="text-xs text-muted-foreground truncate mt-1">{{ userEmail }}</div>
            </div>

            <button (click)="logout()" class="p-2 hover:bg-destructive/10 hover:text-destructive rounded-md focus:outline-none transition-colors" title="Log Out">
              <ui-icon name="log-out" class="h-4 w-4"></ui-icon>
            </button>
          }
        </div>

        <!-- Logout button when collapsed -->
        @if (!isExpanded()) {
          <button (click)="logout()" class="mx-auto mt-2 p-2 hover:bg-destructive/10 hover:text-destructive rounded-md focus:outline-none transition-colors" title="Log Out">
            <ui-icon name="log-out" class="h-4 w-4"></ui-icon>
          </button>
        }

      </div>
    </aside>
  `
})
export class SidebarComponent {
  isPinned = signal(true);
  isHovered = signal(false);

  themeService = inject(ThemeService);
  authService = inject(AuthService);
  permissionService = inject(PermissionService);
  perms = ShopItPermissions;

  get currentTheme() {
    return this.themeService.current;
  }

  get claims() {
    return this.authService.identityClaims as any || {};
  }

  get userName() {
    return this.claims['name'] || this.claims['preferred_username'] || 'User';
  }

  get userEmail() {
    return this.claims['email'] || 'No email';
  }

  get userInitial() {
    return this.userName.substring(0, 1).toUpperCase();
  }

  isExpanded(): boolean {
    return this.isPinned() || this.isHovered();
  }

  onMouseEnter() {
    this.isHovered.set(true);
  }

  onMouseLeave() {
    this.isHovered.set(false);
  }

  togglePin() {
    this.isPinned.update(v => !v);
  }

  setTheme(theme: string) {
    this.themeService.setTheme(theme as Theme);
  }

  logout() {
    this.authService.logOut();
  }
}
