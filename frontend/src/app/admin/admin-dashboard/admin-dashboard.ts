import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { GaragesService } from '../../core/services/garages.service';

type User = { id: number; name: string; email: string; role: 'client' | 'mechanic' | 'admin'; status: 'active' | 'blocked' };
type Request = { id: number; title: string; createdAt: string; status: 'pending' | 'approved' | 'rejected' };

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboardComponent {
  loadingRedirect = signal(false);
  redirectError = signal<string | null>(null);

  // Mock data (replace with API later)
  users = signal<User[]>([
    { id: 1, name: 'Aymen Ben Salah', email: 'aymen@mail.com', role: 'client', status: 'active' },
    { id: 2, name: 'Sami Trabelsi', email: 'sami@mail.com', role: 'mechanic', status: 'active' },
    { id: 3, name: 'Nour Mezghani', email: 'nour@mail.com', role: 'client', status: 'blocked' },
    { id: 4, name: 'Admin', email: 'admin@mail.com', role: 'admin', status: 'active' },
  ]);

  requests = signal<Request[]>([
    { id: 101, title: 'New mechanic verification', createdAt: '2026-02-20', status: 'pending' },
    { id: 102, title: 'Refund request #4451', createdAt: '2026-02-19', status: 'approved' },
    { id: 103, title: 'Complaint about service', createdAt: '2026-02-18', status: 'pending' },
  ]);

  query = signal('');

  filteredUsers = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return this.users();
    return this.users().filter(u =>
      `${u.name} ${u.email} ${u.role} ${u.status}`.toLowerCase().includes(q)
    );
  });

  stats = computed(() => {
    const users = this.users();
    const requests = this.requests();
    return {
      totalUsers: users.length,
      activeUsers: users.filter(u => u.status === 'active').length,
      mechanics: users.filter(u => u.role === 'mechanic').length,
      pendingRequests: requests.filter(r => r.status === 'pending').length,
    };
  });

  blockUser(id: number) {
    this.users.update(list => list.map(u => (u.id === id ? { ...u, status: 'blocked' } : u)));
  }

  unblockUser(id: number) {
    this.users.update(list => list.map(u => (u.id === id ? { ...u, status: 'active' } : u)));
  }

  approveRequest(id: number) {
    this.requests.update(list => list.map(r => (r.id === id ? { ...r, status: 'approved' } : r)));
  }

  rejectRequest(id: number) {
    this.requests.update(list => list.map(r => (r.id === id ? { ...r, status: 'rejected' } : r)));
  }

  constructor(
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly garagesService: GaragesService,
  ) {
    const role = this.authService.user()?.role;

    // Garage admins should land on the workshop dashboard, not this mock template
    if (role === 'AdminEntreprise' || role === 'ChefAtelier') {
      this.loadingRedirect.set(true);

      // 1. Use garageId from JWT (fastest — available after re-login with new token)
      const jwtGarageId = this.authService.user()?.garageId;
      if (jwtGarageId) {
        this.router.navigate(['/garage-admin', jwtGarageId], { replaceUrl: true });
        return;
      }

      // 2. Fall back to fetching garages list and finding the right one
      const currentUserId = this.authService.user()?.id;
      this.garagesService.getMyGarages().subscribe({
        next: (garages) => {
          if (!garages?.length) {
            this.redirectError.set('Aucun garage associé à ce compte.');
            this.loadingRedirect.set(false);
            return;
          }
          // Prefer the garage where this user is the admin, else take first
          const myGarage = garages.find(g => g.adminId === currentUserId) ?? garages[0];
          this.router.navigate(['/garage-admin', myGarage.id], { replaceUrl: true });
        },
        error: () => {
          this.redirectError.set('Impossible de charger le garage associé.');
          this.loadingRedirect.set(false);
        },
      });
    }
  }
}
