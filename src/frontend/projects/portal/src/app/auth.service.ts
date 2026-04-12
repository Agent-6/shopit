import { Injectable } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './auth.config';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  constructor(private oauthService: OAuthService) {
    this.configure();
  }

  private async configure() {
    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();
    await this.oauthService.loadDiscoveryDocumentAndTryLogin();
  }

  public login() {
    this.oauthService.initCodeFlow();
  }

  public logOut() {
    this.oauthService.logOut();
  }

  public get identityClaims() {
    return this.oauthService.getIdentityClaims();
  }

  public get isAuthenticated(): boolean {
    return this.oauthService.hasValidAccessToken();
  }
}
