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
}
