import { Component, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { StaffService } from '../../core/services/staff.service';
import { GaragesService } from '../../core/services/garages.service';
import { StaffDto } from '../../core/models/staff.models';
import { GarageDto } from '../../core/models/garage.models';

@Component({
  selector: 'app-staff-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './staff-management.html',
  styleUrls: ['./staff-management.css'],
})
export class StaffManagementComponent implements OnInit {
  @Input() garageId!: string;

  staff = signal<StaffDto[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  showCreateStaffModal = signal(false);
  staffForm!: FormGroup;
  selectedRole = signal<'ChefAtelier' | 'Mecanicien'>('Mecanicien');

  constructor(
    private readonly staffService: StaffService,
    private readonly fb: FormBuilder
  ) {
    this.staffForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      phone: ['', [Validators.required, Validators.pattern(/^[+\-\d\s()]+$/)]],
    });
  }

  ngOnInit(): void {
    this.loadStaff();
  }

  loadStaff(): void {
    this.loading.set(true);
    this.staffService.getGarageStaff(this.garageId).subscribe({
      next: (response) => {
        this.staff.set(response);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement du personnel');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  onCreateStaff(): void {
    if (!this.staffForm.valid) {
      this.error.set('Veuillez remplir tous les champs correctement');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    const request = {
      ...this.staffForm.value,
      role: this.selectedRole(),
      garageId: this.garageId,
    };

    this.staffService.createStaff(request).subscribe({
      next: () => {
        this.success.set(`${this.getRoleLabel()} créé(e) avec succès`);
        this.staffForm.reset();
        this.showCreateStaffModal.set(false);
        this.loadStaff();
        this.isSubmitting.set(false);
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la création');
        this.isSubmitting.set(false);
      },
    });
  }

  deleteStaff(staffId: string): void {
    if (confirm('Êtes-vous sûr de vouloir supprimer ce membre du personnel?')) {
      this.staffService.deleteStaff(staffId).subscribe({
        next: () => {
          this.success.set('Personnel supprimé avec succès');
          this.loadStaff();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set('Erreur lors de la suppression');
        },
      });
    }
  }

  getRoleLabel(role?: string): string {
    const r = role || this.selectedRole();
    return r === 'ChefAtelier' ? 'Chef d\'Atelier' : 'Mécanicien';
  }

  getChefs(): StaffDto[] {
    return this.staff().filter((s) => s.role === 'ChefAtelier');
  }

  getMechanics(): StaffDto[] {
    return this.staff().filter((s) => s.role === 'Mecanicien');
  }

  closeModal(): void {
    this.showCreateStaffModal.set(false);
    this.staffForm.reset();
    this.error.set(null);
    this.selectedRole.set('Mecanicien');
  }
}

