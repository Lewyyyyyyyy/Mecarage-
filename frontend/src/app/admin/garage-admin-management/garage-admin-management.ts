import { Component, Input, Output, EventEmitter, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../../core/services/admin.service';
import { GaragesService } from '../../core/services/garages.service';
import { GarageAdminDto, CreateGarageAdminRequest } from '../../core/models/garage-admin.models';
import { GarageDto } from '../../core/models/garage.models';

@Component({
  selector: 'app-garage-admin-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './garage-admin-management.html',
  styleUrls: ['./garage-admin-management.css'],
})
export class GarageAdminManagementComponent implements OnInit {
  @Input() tenantId!: string;
  @Output() refresh = new EventEmitter<void>();

  garageAdmins = signal<GarageAdminDto[]>([]);
  garages = signal<GarageDto[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  showCreateAdminModal = signal(false);
  createAdminForm!: FormGroup;

  constructor(
    private readonly adminService: AdminService,
    private readonly garagesService: GaragesService,
    private readonly fb: FormBuilder
  ) {
    this.createAdminForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      phone: ['', [Validators.required, Validators.pattern(/^[+\-\d\s()]+$/)]],
      garageId: ['', [Validators.required]],
    });
  }

  ngOnInit(): void {
    this.loadGarageAdmins();
    this.loadGarages();
  }

  loadGarageAdmins(): void {
    this.loading.set(true);
    this.adminService.getGarageAdmins(this.tenantId).subscribe({
      next: (response) => {
        this.garageAdmins.set(response.data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des administrateurs de garage');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  loadGarages(): void {
    this.garagesService.getTenantGarages(this.tenantId).subscribe({
      next: (garagList) => {
        this.garages.set(garagList || []);
      },
      error: (err) => {
        console.error('Erreur lors du chargement des garages', err);
      },
    });
  }

  onCreateAdmin(): void {
    if (!this.createAdminForm.valid) {
      this.error.set('Veuillez remplir tous les champs correctement');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    const request: CreateGarageAdminRequest = {
      ...this.createAdminForm.value,
      tenantId: this.tenantId,
    };

    this.adminService.createGarageAdmin(request).subscribe({
      next: () => {
        this.success.set('Administrateur de garage créé avec succès');
        this.createAdminForm.reset();
        this.showCreateAdminModal.set(false);
        this.loadGarageAdmins();
        this.isSubmitting.set(false);
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la création de l\'administrateur');
        this.isSubmitting.set(false);
      },
    });
  }

  getAvailableGarages(): GarageDto[] {
    const adminGarageIds = new Set(
      this.garageAdmins()
        .filter((admin) => admin.hasAdmin)
        .map((admin) => admin.garageId)
    );
    return this.garages().filter((garage) => !adminGarageIds.has(garage.id));
  }

  closeModal(): void {
    this.showCreateAdminModal.set(false);
    this.createAdminForm.reset();
    this.error.set(null);
  }
}


