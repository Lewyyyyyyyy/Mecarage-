import { Injectable, signal, inject, OnDestroy } from '@angular/core';
import { NotificationService } from './workshop.service';
import { AuthService } from '../../auth/auth.service';

@Injectable({ providedIn: 'root' })
export class InboxBadgeService implements OnDestroy {
  private notifService = inject(NotificationService);
  private authService = inject(AuthService);

  unreadCount = signal<number>(0);

  private pollInterval: ReturnType<typeof setInterval> | null = null;
  private started = false;

  /** Call once when user logs in (or on app init if already authenticated) */
  start(): void {
    if (this.started) return;
    const role = this.authService.user()?.role;
    if (!['ChefAtelier', 'AdminEntreprise', 'Mecanicien'].includes(role ?? '')) return;

    this.started = true;
    this.load();
    this.pollInterval = setInterval(() => this.load(), 30_000);
  }

  /** Reset when user logs out */
  stop(): void {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
    this.started = false;
    this.unreadCount.set(0);
  }

  /** Refresh immediately (call after marking notifications read in inbox) */
  refresh(): void {
    this.load();
  }

  private load(): void {
    this.notifService.getUnreadCount().subscribe({
      next: (res) => this.unreadCount.set(res.count),
      error: () => { /* silently ignore polling errors */ }
    });
  }

  ngOnDestroy(): void {
    this.stop();
  }
}

