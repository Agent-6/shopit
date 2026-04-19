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
