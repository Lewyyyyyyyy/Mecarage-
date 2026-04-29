import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SymptomReportService } from '../../core/services/workshop.service';
import { VehiclesService } from '../../core/services/vehicles.service';
import { TenantsService } from '../../core/services/tenants.service';
import { GaragesService } from '../../core/services/garages.service';
import { VehicleDto, CreateSymptomReportDto } from '../../core/models/workshop.models';
import { TenantDto } from '../../core/models/tenant.models';
import { GarageDto } from '../../core/models/garage.models';

@Component({
  selector: 'app-symptom-report-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './symptom-report-form.html',
  styleUrls: ['./symptom-report-form.css'],
})
export class SymptomReportFormComponent implements OnInit {
  vehicles = signal<VehicleDto[]>([]);
  tenants = signal<TenantDto[]>([]);
  garages = signal<GarageDto[]>([]);
  loading = signal(false);
  loadingGarages = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  reportForm!: FormGroup;
  createdReportId: string | null = null;
  garageId: string | null = null;
  chefAtelierId: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly symptomService: SymptomReportService,
    private readonly vehiclesService: VehiclesService,
    private readonly tenantsService: TenantsService,
    private readonly garagesService: GaragesService,
    private readonly route: ActivatedRoute
  ) {
    this.reportForm = this.fb.group({
      tenantId: ['', Validators.required],
      garageId: ['', Validators.required],
      vehicleId: ['', Validators.required],
      symptomsDescription: ['', [Validators.required, Validators.minLength(20)]],
    });
  }

  ngOnInit(): void {
    // Try to get garageId from route params first (from /garage-admin/:garageId/symptoms/new)
    // Fall back to query params if not found
    const routeGarageId = this.route.snapshot.paramMap.get('garageId')
      || this.route.snapshot.queryParamMap.get('garageId');

    this.loadVehicles();
    this.loadTenants(routeGarageId);
  }

  loadVehicles(): void {
    this.loading.set(true);
    this.vehiclesService.getMyVehicles().subscribe({
      next: (vehicles) => {
        this.vehicles.set(vehicles);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des véhicules');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  loadTenants(preselectedGarageId?: string | null): void {
    this.tenantsService.getAll().subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        if (preselectedGarageId && tenants.length > 0) {
          // If we have a pre-selected garage, load that garage's details
          this.loadGarageDetails(preselectedGarageId);
        }
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des tenants');
        console.error(err);
      },
    });
  }

  onTenantChange(tenantId: string): void {
    if (!tenantId) {
      this.garages.set([]);
      this.reportForm.patchValue({ garageId: '' });
      return;
    }

    this.loadingGarages.set(true);
    this.garagesService.getTenantGarages(tenantId).subscribe({
      next: (garages) => {
        this.garages.set(garages);
        this.reportForm.patchValue({ garageId: '' });
        this.loadingGarages.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des garages');
        console.error(err);
        this.loadingGarages.set(false);
      },
    });
  }

  loadGarageDetails(garageId: string): void {
    // Find the garage among all garages to get tenant info
    // This will be called after garages are loaded
    setTimeout(() => {
      // Workaround: we'll need to find the garage and set its tenant
      // Load all garages first
      this.garagesService.getAllGarages().subscribe({
        next: (allGarages) => {
          const selectedGarage = allGarages.find(g => g.id === garageId);
          if (selectedGarage) {
            this.reportForm.patchValue({
              tenantId: selectedGarage.tenantId,
              garageId: garageId
            });
            this.onTenantChange(selectedGarage.tenantId);
          }
        },
        error: (err) => console.error(err),
      });
    }, 0);
  }

  onSubmit(): void {
    if (!this.reportForm.valid) {
      this.error.set('Veuillez remplir tous les champs correctement');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    const formValue = this.reportForm.value;
    const request: CreateSymptomReportDto = {
      vehicleId: formValue.vehicleId,
      symptomsDescription: formValue.symptomsDescription,
      garageId: formValue.garageId,
      chefAtelierId: undefined, // Will be set by backend based on garage
    };

    this.symptomService.createReport(request).subscribe({
      next: (response) => {
        this.createdReportId = response.reportId;
        this.success.set('Rapport de symptômes créé avec succès. Le chef d\'atelier a été notifié.');
        this.reportForm.reset();
        this.isSubmitting.set(false);
        setTimeout(() => {
          this.success.set(null);
          this.createdReportId = null;
        }, 5000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la création du rapport');
        this.isSubmitting.set(false);
      },
    });
  }

  getVehicleLabel(vehicleId: string): string {
    const vehicle = this.vehicles().find((v) => v.id === vehicleId);
    return vehicle ? `${vehicle.brand} ${vehicle.model} (${vehicle.year})` : '';
  }

  getTenantLabel(tenantId: string): string {
    const tenant = this.tenants().find((t) => t.id === tenantId);
    return tenant ? tenant.name : '';
  }

  getGarageLabel(garageId: string): string {
    const garage = this.garages().find((g) => g.id === garageId);
    return garage ? `${garage.name} - ${garage.city}` : '';
  }

  protected readonly HTMLSelectElement = HTMLSelectElement;
}

