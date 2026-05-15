import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { UsersService } from '../../core/services/users.service';
import { GaragesService } from '../../core/services/garages.service';
import { AdminService } from '../../core/services/admin.service';
import { TenantsService } from '../../core/services/tenants.service';
import { UserDto, AssignUserToGarageRequest } from '../../core/models/user.models';
import { GarageDto, CreateGarageRequest } from '../../core/models/garage.models';
import { TenantDto } from '../../core/models/tenant.models';
import { AdminKpis } from '../../core/models/admin.models';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-super-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './super-admin-dashboard.html',
  styleUrls: ['./super-admin-dashboard.css'],
})
export class SuperAdminDashboardComponent implements OnInit {
  users = signal<UserDto[]>([]);
  garages = signal<GarageDto[]>([]);
  tenants = signal<TenantDto[]>([]);
  kpis = signal<AdminKpis | null>(null);
  loading = signal(false);
  kpisLoading = signal(false);
  tenantsLoading = signal(false);
  garagesLoading = signal(false);
  usersLoading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  // Modal controls
  showAssignModal = signal(false);
  assignForm!: FormGroup;
  selectedUserId = signal<string | null>(null);

  // Create garage modal
  showCreateGarageModal = signal(false);
  createGarageForm!: FormGroup;

  isSubmitting = signal(false);
  selectedSuperTab = signal<'overview' | 'tenants' | 'garages' | 'users'>('overview');

  searchQuery = signal('');
  selectedRole = signal('');

  filteredUsers = computed(() => {
    const users = this.users();
    const query = this.searchQuery().toLowerCase();
    const role = this.selectedRole();

    return users.filter(u => {
      const matchesQuery =
        u.firstName.toLowerCase().includes(query) ||
        u.lastName.toLowerCase().includes(query) ||
        u.email.toLowerCase().includes(query);
      const matchesRole = !role || u.role === role;
      return matchesQuery && matchesRole;
    });
  });

  stats = computed(() => {
    const users = this.users();
    return {
      totalUsers: users.length,
      admins: users.filter(u => u.role === 'AdminEntreprise').length,
      chefs: users.filter(u => u.role === 'ChefAtelier').length,
      mechanics: users.filter(u => u.role === 'Mecanicien').length,
      clients: users.filter(u => u.role === 'Client').length,
      totalGarages: this.garages().length,
      activeGarages: this.garages().filter(g => g.isActive).length,
      activeTenants: this.tenants().filter(t => t.isActive).length,
      garagesWithAdmin: this.garages().filter(g => g.adminId).length,
    };
  });

  constructor(
    private usersService: UsersService,
    private garagesService: GaragesService,
    private adminService: AdminService,
    private tenantsService: TenantsService,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.assignForm = this.fb.group({
      garageId: ['', Validators.required],
    });

    this.createGarageForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      address: ['', Validators.required],
      city: ['', Validators.required],
      phone: ['', [Validators.required, Validators.pattern(/^[\d\s\-\+\(\)]{8,}$/)]],
      latitude: [''],
      longitude: [''],
    });
  }

  ngOnInit() {
    this.loadUsers();
    this.loadGarages();
    this.loadTenants();
    this.loadKpis();
  }

  loadUsers() {
    this.usersLoading.set(true);
    this.usersService.getAllUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.usersLoading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des utilisateurs');
        this.usersLoading.set(false);
        console.error(err);
      },
    });
  }

  loadGarages() {
    this.garagesLoading.set(true);
    this.garagesService.getAllGarages().subscribe({
      next: (garages) => {
        this.garages.set(garages);
        this.garagesLoading.set(false);
      },
      error: (err) => {
        console.error('Erreur lors du chargement des garages', err);
        this.garagesLoading.set(false);
      },
    });
  }

  loadTenants() {
    this.tenantsLoading.set(true);
    this.tenantsService.getAll().subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        this.tenantsLoading.set(false);
      },
      error: (err) => {
        console.error('Erreur lors du chargement des tenants', err);
        this.tenantsLoading.set(false);
      },
    });
  }

  loadKpis() {
    this.kpisLoading.set(true);
    this.adminService.getKpis().subscribe({
      next: (kpis) => {
        this.kpis.set(kpis);
        this.kpisLoading.set(false);
      },
      error: (err) => {
        console.error('Erreur lors du chargement des KPIs', err);
        this.kpisLoading.set(false);
      },
    });
  }

  navigateToTenants() {
    this.router.navigate(['/tenants']);
  }

  openAssignModal(userId: string) {
    this.selectedUserId.set(userId);
    this.showAssignModal.set(true);
    this.assignForm.reset();
  }

  closeAssignModal() {
    this.showAssignModal.set(false);
    this.selectedUserId.set(null);
  }

  assignUserToGarage() {
    if (!this.assignForm.valid || !this.selectedUserId()) {
      return;
    }

    const request: AssignUserToGarageRequest = {
      userId: this.selectedUserId()!,
      garageId: this.assignForm.get('garageId')!.value,
    };

    this.usersService.assignUserToGarage(request).subscribe({
      next: () => {
        this.success.set('Utilisateur assigné au garage avec succès');
        this.loadUsers();
        this.closeAssignModal();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de l\'assignation');
        console.error(err);
      },
    });
  }

  openCreateGarageModal() {
    this.showCreateGarageModal.set(true);
    this.createGarageForm.reset();
  }

  closeCreateGarageModal() {
    this.showCreateGarageModal.set(false);
  }

  createGarage() {
    if (!this.createGarageForm.valid) {
      return;
    }

    this.isSubmitting.set(true);
    const formValue = this.createGarageForm.value;
    const request: CreateGarageRequest = {
      // Don't send tenantId - backend extracts it from JWT claims
      name: formValue.name?.trim() || '',
      address: formValue.address?.trim() || '',
      city: formValue.city?.trim() || '',
      phone: formValue.phone?.trim() || '',
      latitude: formValue.latitude ? parseFloat(formValue.latitude) : null,
      longitude: formValue.longitude ? parseFloat(formValue.longitude) : null,
    };

    this.garagesService.create(request)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.success.set('Garage créé avec succès');
          this.loadGarages();
          this.loadKpis();
          this.closeCreateGarageModal();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          // Extract detailed validation errors from backend
          let errorMessage = 'Erreur lors de la création du garage';

          if (err.error?.errors) {
            // Build detailed error message from validation errors
            const validationErrors = Object.entries(err.error.errors)
              .map(([field, messages]: [string, any]) => {
                const msgs = Array.isArray(messages) ? messages : [messages];
                return `${field}: ${msgs.join(', ')}`;
              })
              .join('\n');
            errorMessage = validationErrors;
          } else if (err.error?.message) {
            errorMessage = err.error.message;
          }

          this.error.set(errorMessage);
          console.error('Garage creation error:', err);
        },
      });
  }


  clearMessages() {
    this.error.set(null);
    this.success.set(null);
  }

  onSearchChange(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchQuery.set(target.value);
  }

  onRoleChange(event: Event) {
    const target = event.target as HTMLSelectElement;
    this.selectedRole.set(target.value);
  }
}
