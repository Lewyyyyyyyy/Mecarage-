import { Component, signal, inject, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ThemeService } from '../theme';
import { AuthService } from '../auth/auth.service';
import { NotificationBellComponent } from '../core/notification-bell/notification-bell';
import { LanguageService, LANGUAGES } from '../core/language/language.service';
import { InboxBadgeService } from '../core/services/inbox-badge.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, NotificationBellComponent, TranslateModule],
  templateUrl: './navbar.html',
})
export class NavbarComponent implements OnInit {
  isMenuOpen = signal(false);
  isLangOpen = signal(false);
  themeService = inject(ThemeService);
  authService = inject(AuthService);
  router = inject(Router);
  langService = inject(LanguageService);
  badgeService = inject(InboxBadgeService);

  languages = LANGUAGES;

  isAuthenticated = this.authService.isAuthenticated;
  user = computed(() => {
    const currentUser = this.authService.user();
    if (currentUser) {
      return {
        ...currentUser,
        initials: `${currentUser.firstName[0]}${currentUser.lastName[0]}`.toUpperCase()
      };
    }
    return null;
  });

  isSuperAdmin = computed(() => this.authService.user()?.role === 'SuperAdmin');
  isGarageAdmin = computed(() => {
    const r = this.authService.user()?.role;
    return r === 'AdminEntreprise' || r === 'ChefAtelier';
  });
  isAdminEntreprise = computed(() => this.authService.user()?.role === 'AdminEntreprise');
  isClient = computed(() => this.authService.user()?.role === 'Client');
  isChef = computed(() => this.authService.user()?.role === 'ChefAtelier');
  isMechanic = computed(() => this.authService.user()?.role === 'Mecanicien');

  inboxBadge = this.badgeService.unreadCount;

  ngOnInit(): void {
    this.badgeService.start();
  }

  toggleMenu = () => this.isMenuOpen.update(v => !v);
  isMenuOpenFn = () => this.isMenuOpen();

  toggleLangMenu = () => this.isLangOpen.update(v => !v);
  closeLangMenu = () => this.isLangOpen.set(false);

  currentLang = this.langService.currentLang;
  getCurrentLanguage = () => this.langService.getCurrentLanguage();

  selectLanguage(code: string) {
    this.langService.setLanguage(code);
    this.isLangOpen.set(false);
  }

  toggleTheme() { this.themeService.toggleDarkMode(); }
  isDark() { return this.themeService.isDarkMode(); }

  logout() {
    this.badgeService.stop();
    this.authService.clearSession();
    this.router.navigate(['/login']);
  }
}
