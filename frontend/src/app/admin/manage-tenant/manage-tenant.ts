import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TenantsService, TenantDto, UpdateTenantRequest } from '../../core/services/tenants.service';
import { GaragesService } from '../../core/services/garages.service';
import { GarageDto } from '../../core/models/garage.models';
import { GarageAdminManagementComponent } from '../garage-admin-management/garage-admin-management';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-manage-tenant',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, GarageAdminManagementComponent],
  templateUrl: './manage-tenant.html',
  styleUrls: ['./manage-tenant.css'],
})
export class ManageTenantComponent implements OnInit {
  tenant = signal<TenantDto | null>(null);
  garages = signal<GarageDto[]>([]);
  loading = signal(false);
  garagesLoading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  tenantId = '';

  showEditModal = signal(false);
  editForm!: FormGroup;
  isSubmitting = signal(false);

  showCreateGarageModal = signal(false);
  createGarageForm!: FormGroup;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tenantsService: TenantsService,
    private garagesService: GaragesService,
    private fb: FormBuilder
  ) {
    this.editForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[\d\s\-\+\(\)]{8,}$/)]],
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
    this.tenantId = this.route.snapshot.paramMap.get('id') || '';
    if (this.tenantId) {
      this.loadTenant();
    }
  }

  loadTenant() {
    this.loading.set(true);
    this.tenantsService.getTenantById(this.tenantId).subscribe({
      next: (tenant) => {
        this.tenant.set(tenant);
        this.editForm.patchValue({
          name: tenant.name,
          email: tenant.email,
          phone: tenant.phone,
        });
        this.loading.set(false);
        this.loadGarages();
      },
      error: (err) => {
        this.error.set('Error loading tenant');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  loadGarages() {
    this.garagesLoading.set(true);
    // Get garages for this specific tenant
    this.garagesService.getTenantGarages(this.tenantId).subscribe({
      next: (garages) => {
        this.garages.set(garages);
        this.garagesLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading garages', err);
        this.garagesLoading.set(false);
      },
    });
  }

  openEditModal() {
    this.showEditModal.set(true);
  }

  closeEditModal() {
    this.showEditModal.set(false);
  }

  updateTenant() {
    if (!this.editForm.valid) {
      return;
    }

    this.isSubmitting.set(true);
    const request: UpdateTenantRequest = {
      name: this.editForm.get('name')?.value?.trim() || '',
      email: this.editForm.get('email')?.value?.trim() || '',
      phone: this.editForm.get('phone')?.value?.trim() || '',
    };

    this.tenantsService.updateTenant(this.tenantId, request)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.success.set('Tenant updated successfully');
          this.loadTenant();
          this.closeEditModal();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Error updating tenant');
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
    const request = {
      name: formValue.name?.trim() || '',
      address: formValue.address?.trim() || '',
      city: formValue.city?.trim() || '',
      phone: formValue.phone?.trim() || '',
      latitude: formValue.latitude ? parseFloat(formValue.latitude) : null,
      longitude: formValue.longitude ? parseFloat(formValue.longitude) : null,
    };

    this.garagesService.createWithTenant(request, this.tenantId)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.success.set('Garage created successfully');
          this.loadGarages();
          this.closeCreateGarageModal();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Error creating garage');
          console.error(err);
        },
      });
  }

  goBack() {
    this.router.navigate(['/tenants']);
  }
}

