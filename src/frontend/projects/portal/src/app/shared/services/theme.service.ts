import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'dark' | 'light' | 'system';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly current = signal<Theme>('system');

  constructor() {
    const stored = localStorage.getItem('portal-theme') as Theme;
    if (stored) {
      this.current.set(stored);
    }
    
    this.applyTheme(this.current());

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (this.current() === 'system') this.applyTheme('system');
    });

    effect(() => {
      const mode = this.current();
      if (mode === 'system') {
        localStorage.removeItem('portal-theme');
      } else {
        localStorage.setItem('portal-theme', mode);
      }
      this.applyTheme(mode);
    });
  }

  setTheme(theme: Theme) {
    this.current.set(theme);
  }

  private applyTheme(theme: Theme) {
    const isDark = theme === 'dark' || (theme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
    if (isDark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }
}
