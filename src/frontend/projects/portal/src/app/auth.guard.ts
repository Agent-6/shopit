import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  
  if (authService.isAuthenticated) {
    return true;
  }
  
  // Fallback to login if standard synchronous token validation indicates unauthorized
  authService.login();
  return false;
};
