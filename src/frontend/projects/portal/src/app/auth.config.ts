import { AuthConfig } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
  // Use the local API auth port we found in launchSettings.json
  // Note: OpenIddict discovery document strict checks require the trailing slash!
  issuer: 'https://localhost:7234/',

  // URL of the SPA to redirect the user to after login
  redirectUri: window.location.origin + '/auth-callback',

  // URL of the SPA to redirect the user to after logout
  postLogoutRedirectUri: window.location.origin,

  // The SPA's id. The API is already mapped to use this in Program.cs
  clientId: 'angular-spa',

  // Use the authorization code flow
  responseType: 'code',

  // Scopes requested from the auth server
  scope: 'openid profile email roles',

  // Display trace details in console
  showDebugInformation: true,
  
  // Set to false strictly for local development localhost HTTP binding (avoiding standard Https enforce limits)
  requireHttps: false, 
};
