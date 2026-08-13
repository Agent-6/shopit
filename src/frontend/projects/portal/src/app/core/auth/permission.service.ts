import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * Loads the current user's effective permissions (from `GET /users/me/permissions`) once
 * and exposes them for route guards, sidebar visibility, and button/card gating.
 */
@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly http = inject(HttpClient);
  private readonly permissions = signal<string[]>([]);
  private loadPromise: Promise<void> | null = null;

  get all(): string[] {
    return this.permissions();
  }

  /** Fetches and caches the caller's permissions. Safe to call multiple times. */
  load(): Promise<void> {
    const existing = this.loadPromise;
    if (existing) {
      return existing;
    }

    // Capture the promise in a local: TypeScript cannot narrow the mutable property
    // to non-null after the guard, so returning it directly fails to typecheck.
    const promise = lastValueFrom(
      this.http.get<{ permissions: string[] }>(`${environment.apiUrl}/identity/users/me/permissions`)
    )
      .then((response) => this.permissions.set(response?.permissions ?? []))
      .catch(() => {
        this.permissions.set([]);
        // Don't cache failures — a later call can retry (e.g. after a token refresh).
        this.loadPromise = null;
      });

    this.loadPromise = promise;
    return promise;
  }

  has(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  hasAny(...permissions: string[]): boolean {
    return permissions.some((permission) => this.has(permission));
  }
}
