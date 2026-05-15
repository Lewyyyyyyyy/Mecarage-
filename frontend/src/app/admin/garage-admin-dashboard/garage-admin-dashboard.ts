import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { GaragesService } from '../../core/services/garages.service';
import { AuthService } from '../../auth/auth.service';
import { GarageInterventionDto, GarageClientDto } from '../../core/models/garage.models';
import { StaffManagementComponent } from '../staff-management/staff-management';
import { InboxComponent } from '../../core/inbox/inbox';
import { ChefExaminationReviewComponent } from '../chef-examination-review/chef-examination-review';
import { ChefRepairManagementComponent } from '../chef-repair-management/chef-repair-management';
import { InterventionTrackerComponent } from '../intervention-tracker/intervention-tracker';
import { RepairTaskService } from '../../core/services/workshop.service';

@Component({
  selector: 'app-garage-admin-dashboard',
  standalone: true,
  imports: [CommonModule, StaffManagementComponent, InboxComponent, ChefExaminationReviewComponent, ChefRepairManagementComponent, InterventionTrackerComponent, RouterLink],
  templateUrl: './garage-admin-dashboard.html',
  styleUrls: ['./garage-admin-dashboard.css'],
})
export class GarageAdminDashboardComponent implements OnInit {
  garageId: string = '';
  selectedTab = signal<'dashboard' | 'kpis' | 'staff' | 'reports' | 'appointments' | 'examinations' | 'repairs' | 'interventions'>('dashboard');
  pendingExaminationsCount = signal(0);
  interventions = signal<GarageInterventionDto[]>([]);
  clients = signal<GarageClientDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  selectedIntervention = signal<GarageInterventionDto | null>(null);
  showInterventionDetails = signal(false);

  searchInterventionQuery = signal('');
  searchClientQuery = signal('');

  filteredInterventions = computed(() => {
    const interventions = this.interventions();
    const query = this.searchInterventionQuery().toLowerCase();
    return interventions.filter(
      i =>
        i.clientName.toLowerCase().includes(query) ||
        i.vehicleInfo.toLowerCase().includes(query) ||
        i.status.toLowerCase().includes(query)
    );
  });

  filteredClients = computed(() => {
    const clients = this.clients();
    const query = this.searchClientQuery().toLowerCase();
    return clients.filter(
      c =>
        c.firstName.toLowerCase().includes(query) ||
        c.lastName.toLowerCase().includes(query) ||
        c.email.toLowerCase().includes(query)
    );
  });

  stats = computed(() => {
    const interventions = this.interventions();
    const clients = this.clients();
    return {
      totalInterventions: interventions.length,
      pendingInterventions: interventions.filter(i => i.status === 'Pending').length,
      completedInterventions: interventions.filter(i => i.status === 'Completed').length,
      totalClients: clients.length,
      totalVehicles: clients.reduce((sum, c) => sum + c.vehicleCount, 0),
    };
  });

  garageKpis = computed(() => {
    const interventions = this.interventions();
    const clients = this.clients();
    const total = interventions.length;
    const pending = interventions.filter(i => i.status === 'Pending').length;
    const inProgress = interventions.filter(i => i.status === 'InProgress').length;
    const completed = interventions.filter(i => i.status === 'Completed').length;
    const cancelled = interventions.filter(i => i.status === 'Cancelled').length;
    const completionRate = total > 0 ? Math.round((completed / total) * 100) : 0;

    // Unique vehicles seen at this garage (by vehicleInfo string)
    const uniqueVehicleSet = new Set(interventions.map(i => i.vehicleInfo));
    const uniqueVehicles = uniqueVehicleSet.size;

    // Unique clients seen at this garage (by email)
    const uniqueClientSet = new Set(interventions.map(i => i.clientEmail));
    const uniqueClients = uniqueClientSet.size;

    // Avg interventions per client
    const totalInterventionFiles = clients.reduce((s, c) => s + c.interventionCount, 0);
    const avgInterventions = clients.length > 0
      ? (totalInterventionFiles / clients.length).toFixed(1)
      : '0';

    // Total registered vehicles across all clients
    const totalRegisteredVehicles = clients.reduce((s, c) => s + c.vehicleCount, 0);

    return {
      total, pending, inProgress, completed, cancelled,
      completionRate, uniqueVehicles, uniqueClients,
      avgInterventions, totalRegisteredVehicles,
    };
  });

  constructor(private garagesService: GaragesService, private route: ActivatedRoute, private authService: AuthService) {}

  ngOnInit() {
    // Chef d'atelier lands directly on the reports tab
    if (this.authService.user()?.role === 'ChefAtelier') {
      this.selectedTab.set('reports');
    }

    this.route.params.subscribe(params => {
      this.garageId = params['garageId'];
      this.loadGarageData();
    });
  }

  loadGarageData() {
    // ChefAtelier only needs inbox/examination data — skip heavy dashboard queries
    if (this.authService.user()?.role === 'ChefAtelier') {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    // Load interventions
    this.garagesService.getGarageInterventions(this.garageId).subscribe({
      next: (interventions) => {
        this.interventions.set(interventions);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des interventions');
        console.error(err);
      },
    });

    // Load clients
    this.garagesService.getGarageClients(this.garageId).subscribe({
      next: (clients) => {
        this.clients.set(clients);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des clients');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  showIntervention(intervention: GarageInterventionDto) {
    this.selectedIntervention.set(intervention);
    this.showInterventionDetails.set(true);
  }

  closeInterventionDetails() {
    this.showInterventionDetails.set(false);
    this.selectedIntervention.set(null);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'InProgress':
        return 'bg-blue-100 text-blue-800';
      case 'Completed':
        return 'bg-green-100 text-green-800';
      case 'Cancelled':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  clearError() {
    this.error.set(null);
  }

  sortClientsByInterventions = (a: any, b: any) => b.interventionCount - a.interventionCount;

  onSearchInterventionChange(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchInterventionQuery.set(target.value);
  }

  onSearchClientChange(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchClientQuery.set(target.value);
  }
}

