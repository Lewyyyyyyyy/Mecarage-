import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AppointmentService } from '../../core/services/workshop.service';
import { GaragesService } from '../../core/services/garages.service';
import { GarageDto } from '../../core/models/garage.models';

@Component({
  selector: 'app-appointment-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './appointment-create.html',
  styleUrls: ['./appointment-create.css'],
})
export class AppointmentCreateComponent implements OnInit {
  garages = signal<GarageDto[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  appointmentForm!: FormGroup;
  symptomReportId: string | null = null;
  createdAppointmentId: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly appointmentService: AppointmentService,
    private readonly garagesService: GaragesService,
    private readonly route: ActivatedRoute
  ) {
    this.appointmentForm = this.fb.group({
      vehicleId: ['', Validators.required],
      garageId: ['', Validators.required],
      preferredDate: ['', Validators.required],
      preferredTime: ['', Validators.required],
      specialRequests: [''],
    });
  }

  ngOnInit(): void {
    this.loadGarages();
    this.route.queryParams.subscribe((params) => {
      if (params['symptomReportId']) {
        this.symptomReportId = params['symptomReportId'];
      }
    });
  }

  loadGarages(): void {
    this.loading.set(true);
    this.garagesService.getMyGarages().subscribe({
      next: (garages) => {
        this.garages.set(garages || []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des garages');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  onSubmit(): void {
    if (!this.appointmentForm.valid) {
      this.error.set('Veuillez remplir tous les champs correctement');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    const request = {
      ...this.appointmentForm.value,
      symptomReportId: this.symptomReportId,
    };

    this.appointmentService.createAppointment(request).subscribe({
      next: (response) => {
        this.createdAppointmentId = response.appointmentId;
        this.success.set('Rendez-vous créé avec succès. Le chef d\'atelier l\'examinera bientôt.');
        this.appointmentForm.reset();
        this.isSubmitting.set(false);
        setTimeout(() => {
          this.success.set(null);
        }, 5000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la création du rendez-vous');
        this.isSubmitting.set(false);
      },
    });
  }
}

