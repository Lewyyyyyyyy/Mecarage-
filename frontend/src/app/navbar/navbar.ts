import { Component, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ThemeService } from '../theme';
import { AuthService } from '../auth/auth.service';
import { NotificationBellComponent } from '../core/notification-bell/notification-bell';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, NotificationBellComponent],
  templateUrl: './navbar.html',
})
export class NavbarComponent {
  isMenuOpen = signal(false);
  themeService = inject(ThemeService);
  authService = inject(AuthService);
  router = inject(Router);

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

  isSuperAdmin = computed(() => {
    const currentUser = this.authService.user();
    return currentUser?.role === 'SuperAdmin';
  });

  isGarageAdmin = computed(() => {
    const currentUser = this.authService.user();
    return currentUser?.role === 'AdminEntreprise' || currentUser?.role === 'ChefAtelier';
  });

  isAdminEntreprise = computed(() => {
    const currentUser = this.authService.user();
    return currentUser?.role === 'AdminEntreprise';
  });

  isClient = computed(() => {
    const currentUser = this.authService.user();
    return currentUser?.role === 'Client';
  });

  isChef = computed(() => {
    const currentUser = this.authService.user();
    return currentUser?.role === 'ChefAtelier';
  });

  isMechanic = computed(() => {
    const currentUser = this.authService.user();
    return currentUser?.role === 'Mecanicien';
  });

  toggleMenu = () => {
    this.isMenuOpen.update(value => !value);
  };

  isMenuOpenFn = () => this.isMenuOpen();

  toggleTheme() {
    this.themeService.toggleDarkMode();
  }

  isDark() {
    return this.themeService.isDarkMode();
  }

  logout() {
    this.authService.clearSession();
    this.router.navigate(['/login']);
  }
}
