import { Injectable, signal, effect } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private darkMode = signal<boolean>(
    localStorage.getItem('theme') === 'dark'
  );

  isDarkMode = this.darkMode.asReadonly();

  constructor() {
    // Initial application without effect to be safe
    this.applyTheme(this.darkMode());

    effect(() => {
      this.applyTheme(this.darkMode());
    });
  }

  private applyTheme(isDark: boolean) {
    console.log('Applying theme. Is dark?', isDark);
    if (isDark) {
      document.documentElement.classList.add('dark');
      document.body.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark');
      document.body.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }

  toggleDarkMode() {
    this.darkMode.update((dark) => !dark);
  }
}
