import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-auth-callback',
  standalone: true,
  template: `
    <div style="display: flex; justify-content: center; align-items: center; height: 100vh;">
      <h2>Authenticating...</h2>
    </div>
  `
})
export class AuthCallbackComponent implements OnInit {
  constructor(private router: Router) {}
  
  ngOnInit() {
    // The OAuthService intercepts the raw query params locally upon bootstrap via 'loadDiscoveryDocumentAndTryLogin'
    // in the AuthService constructor. We simply spin here and jump internally
    setTimeout(() => {
      this.router.navigate(['/']);
    }, 1500);
  }
}
