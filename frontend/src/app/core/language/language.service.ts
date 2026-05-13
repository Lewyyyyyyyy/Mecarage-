import { Injectable, signal, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export interface Language {
  code: string;
  label: string;
  flag: string;
  dir: 'ltr' | 'rtl';
}

export const LANGUAGES: Language[] = [
  { code: 'fr', label: 'Français', flag: '🇫🇷', dir: 'ltr' },
  { code: 'en', label: 'English',  flag: '🇬🇧', dir: 'ltr' },
  { code: 'ar', label: 'العربية',  flag: '🇩🇿', dir: 'rtl' },
];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translate = inject(TranslateService);

  currentLang = signal<string>(localStorage.getItem('lang') ?? 'fr');

  init() {
    this.translate.addLangs(['en', 'fr', 'ar']);
    this.translate.setDefaultLang('fr');
    const saved = localStorage.getItem('lang') ?? 'fr';
    this.setLanguage(saved);
  }

  setLanguage(code: string) {
    this.translate.use(code);
    this.currentLang.set(code);
    localStorage.setItem('lang', code);
    const lang = LANGUAGES.find(l => l.code === code);
    document.documentElement.setAttribute('dir', lang?.dir ?? 'ltr');
    document.documentElement.setAttribute('lang', code);
  }

  getLanguages() {
    return LANGUAGES;
  }

  getCurrentLanguage(): Language {
    return LANGUAGES.find(l => l.code === this.currentLang()) ?? LANGUAGES[0];
  }
}

