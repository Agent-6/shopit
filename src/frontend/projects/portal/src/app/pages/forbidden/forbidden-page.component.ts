import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { UiButtonComponent } from '../../shared/components/ui-button.component';

@Component({
  selector: 'app-forbidden-page',
  standalone: true,
  imports: [UiButtonComponent],
  template: `
    <div class="flex min-h-[50vh] items-center justify-center">
      <div class="max-w-md text-center space-y-4">
        <div class="text-6xl font-bold text-primary/20">403</div>
        <h1 class="text-2xl font-semibold tracking-tight">Access denied</h1>
        <p class="text-sm text-muted-foreground">
          You don't have permission to view this page. If you believe this is a mistake,
          ask an administrator to grant your account the required permission.
        </p>
        <div class="flex justify-center gap-3 pt-2">
          <ui-button variant="default" (click)="logout()">Log out</ui-button>
        </div>
      </div>
    </div>
  `,
})
export class ForbiddenPageComponent {
  private readonly authService = inject(AuthService);

  protected logout(): void {
    this.authService.logOut();
  }
}
