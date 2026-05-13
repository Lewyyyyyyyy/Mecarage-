import { Injectable, signal, effect } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private darkMode = signal<boolean>(
    localStorage.getItem('theme') === 'dark' ||
      (!localStorage.getItem('theme') &&
        window.matchMedia('(prefers-color-scheme: dark)').matches)
  );

  isDarkMode = this.darkMode.asReadonly();

  constructor() {
    this.applyTheme(this.darkMode());
    effect(() => {
      this.applyTheme(this.darkMode());
    });
  }

  private applyTheme(isDark: boolean) {
    const root = document.documentElement;
    const body = document.body;

    if (isDark) {
      root.classList.add('dark');
      body.classList.add('dark');
      body.setAttribute('data-theme', 'dark');
      localStorage.setItem('theme', 'dark');
    } else {
      root.classList.remove('dark');
      body.classList.remove('dark');
      body.setAttribute('data-theme', 'light');
      localStorage.setItem('theme', 'light');
    }
  }

  toggleDarkMode() {
    this.darkMode.update((dark) => !dark);
  }
}
