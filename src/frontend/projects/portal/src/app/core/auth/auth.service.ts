import { inject, Injectable } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './auth.config';
import { Router } from '@angular/router';
import { APP_ROUTES } from '../../routes';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly router = inject(Router);
  private readonly oauthService = inject(OAuthService);

  public async configure() {
    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();
    await this.oauthService.loadDiscoveryDocumentAndTryLogin();
  }

  public login() {
    this.oauthService.initCodeFlow();
  }

  public switchAccount() {
    // Start a fresh code flow asking the auth server to show the account chooser,
    // even when the user is already signed in. The prompt is passed per-call (not via
    // customQueryParams) so silent refresh requests never carry it.
    this.oauthService.initCodeFlow('', { prompt: 'select_account' });
  }

  public logOut() {
    this.oauthService.revokeTokenAndLogout(true).then(() => {
      window.location.reload();
    });
  }

  public get identityClaims() {
    return this.oauthService.getIdentityClaims();
  }

  public get isAuthenticated(): boolean {
    return this.oauthService.hasValidAccessToken();
  }

  /**
   * The multi-tenancy side of the current session: 'Host' when the access token
   * carries no tenant or the host tenant id, 'Tenant' otherwise. The auth server
   * puts the `tenant_id` claim in the access token (not the identity token), so
   * the JWT payload is decoded directly.
   */
  public get currentSide(): 'Host' | 'Tenant' {
    try {
      const token = this.oauthService.getAccessToken();
      const payload = token.split('.')[1];
      if (!payload) {
        return 'Host';
      }
      const json = decodeURIComponent(
        atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
          .split('')
          .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
          .join('')
      );
      const claims = JSON.parse(json) as Record<string, unknown>;
      const tenantId = claims?.['tenant_id'];
      const value = typeof tenantId === 'string' ? tenantId : '';
      return !value || value === '00000000-0000-0000-0000-000000000000' ? 'Host' : 'Tenant';
    } catch {
      return 'Host';
    }
  }
}
