import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { OAuthStorage } from 'angular-oauth2-oidc';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authStorage = inject(OAuthStorage);
  const token = authStorage.getItem('access_token');

  // Only add token to API requests
  if (token && req.url.includes('/api/')) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    return next(authReq);
  }

  return next(req);
};
