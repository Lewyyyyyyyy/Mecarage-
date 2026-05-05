import { Component, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { NotificationService } from '../services/workshop.service';
import { ClientNotificationDto } from '../models/workshop.models';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './notification-bell.html',
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private notifService = inject(NotificationService);
  private router = inject(Router);

  notifications = signal<ClientNotificationDto[]>([]);
  isOpen = signal(false);
  private pollInterval: ReturnType<typeof setInterval> | null = null;

  unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  ngOnInit(): void {
    this.load();
    // Poll every 30 seconds
    this.pollInterval = setInterval(() => this.load(), 30_000);
  }

  ngOnDestroy(): void {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  load(): void {
    this.notifService.getMyNotifications().subscribe({
      next: (n) => this.notifications.set(n || []),
      error: () => {},
    });
  }

  toggleDropdown(): void {
    this.isOpen.update(v => !v);
  }

  markAndNavigate(notif: ClientNotificationDto): void {
    if (!notif.isRead) {
      this.notifService.markAsRead(notif.id).subscribe(() => {
        this.notifications.update(list =>
          list.map(n => n.id === notif.id ? { ...n, isRead: true } : n)
        );
      });
    }
    this.isOpen.set(false);
    const route = this.resolveRoute(notif);
    if (route) this.router.navigateByUrl(route);
  }

  private resolveRoute(notif: ClientNotificationDto): string | null {
    switch (notif.notificationType) {
      case 'ChefFeedbackApproved':
        return '/appointments?book=1';
      case 'InvoiceReady':
        return '/history?tab=invoices';
      case 'ExaminationDeclined':
        return '/history?tab=appointments';
      case 'ReadyForPickup':
        return '/history?tab=appointments';
      case 'ChefFeedbackDeclined':
        return notif.symptomReportId ? `/symptoms/${notif.symptomReportId}` : '/symptoms';
      default:
        return '/history';
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit',
    });
  }
}

