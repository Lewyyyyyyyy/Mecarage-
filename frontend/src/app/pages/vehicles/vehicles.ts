import { Component, signal, inject, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { VehicleService } from '../../core/services/vehicle.service';
import { VehicleDto, CreateVehicleRequest } from '../../core/models/vehicle.models';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './vehicles.html',
  styleUrl: './vehicles.css'
})
export class VehiclesComponent implements OnInit {
  vehicleService = inject(VehicleService);
  fb = inject(FormBuilder);

  vehicles = signal<VehicleDto[]>([]);
  isLoading = signal(false);
  isAddingVehicle = signal(false);
  successMessage = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  isModalOpen = signal(false);

  fuelTypes = ['Essence', 'Diesel', 'Électrique', 'Hybride', 'GPL'];

  createVehicleForm = this.fb.group({
    brand: ['', [Validators.required]],
    model: ['', [Validators.required]],
    year: ['', [Validators.required, Validators.min(1900), Validators.max(new Date().getFullYear() + 1)]],
    licensePlate: ['', [Validators.required]],
    fuelType: ['Essence', [Validators.required]],
    mileage: ['', [Validators.required, Validators.min(0)]],
    vin: ['']
  });

  ngOnInit() {
    this.loadVehicles();
  }

  loadVehicles() {
    this.isLoading.set(true);
    this.vehicleService.getMyVehicles()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (data) => {
          this.vehicles.set(data);
        },
        error: (error) => {
          this.errorMessage.set(error?.error?.message || 'Failed to load vehicles');
        }
      });
  }

  openAddVehicleModal() {
    this.isModalOpen.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);
    this.createVehicleForm.reset({ fuelType: 'Essence' });
  }

  closeAddVehicleModal() {
    this.isModalOpen.set(false);
    this.createVehicleForm.reset();
    this.successMessage.set(null);
    this.errorMessage.set(null);
  }

  submitAddVehicle() {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.createVehicleForm.invalid) {
      this.createVehicleForm.markAllAsTouched();
      return;
    }

    const formValue = this.createVehicleForm.getRawValue();
    const payload: CreateVehicleRequest = {
      brand: formValue.brand!,
      model: formValue.model!,
      year: parseInt(formValue.year!.toString()),
      licensePlate: formValue.licensePlate!,
      fuelType: formValue.fuelType!,
      mileage: parseInt(formValue.mileage!.toString()),
      vin: formValue.vin || null
    };

    this.isAddingVehicle.set(true);

    this.vehicleService.createVehicle(payload)
      .pipe(finalize(() => this.isAddingVehicle.set(false)))
      .subscribe({
        next: (response) => {
          this.successMessage.set(response.message);
          this.createVehicleForm.reset({ fuelType: 'Essence' });
          setTimeout(() => {
            this.closeAddVehicleModal();
            this.loadVehicles();
          }, 1500);
        },
        error: (error) => {
          this.errorMessage.set(error?.error?.message || 'Failed to add vehicle');
        }
      });
  }
}

