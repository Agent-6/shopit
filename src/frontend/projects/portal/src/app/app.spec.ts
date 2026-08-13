import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { App } from './app';

describe('App', () => {
  const oauthServiceStub = {
    configure: () => undefined,
    setupAutomaticSilentRefresh: () => undefined,
    loadDiscoveryDocumentAndTryLogin: () => Promise.resolve(false),
    initCodeFlow: () => undefined,
    revokeTokenAndLogout: () => Promise.resolve(false),
    getIdentityClaims: () => ({}),
    hasValidAccessToken: () => false,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        { provide: OAuthService, useValue: oauthServiceStub },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the router outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
});
