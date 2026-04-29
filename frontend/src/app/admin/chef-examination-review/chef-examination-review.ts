import { Component, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RepairTaskService } from '../../core/services/workshop.service';
import { PendingExaminationDto } from '../../core/models/workshop.models';

@Component({
  selector: 'app-chef-examination-review',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './chef-examination-review.html',
})
export class ChefExaminationReviewComponent implements OnInit {
  @Input() garageId = '';

  examinations = signal<PendingExaminationDto[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  showReviewModal = signal(false);
  selectedExamination: PendingExaminationDto | null = null;
  reviewForm: FormGroup;

  constructor(
    private taskService: RepairTaskService,
    private fb: FormBuilder
  ) {
    this.reviewForm = this.fb.group({
      isApproved: [true, Validators.required],
      serviceFee: [0, [Validators.required, Validators.min(0)]],
      declineReason: [''],
    });
  }

  ngOnInit(): void {
    if (this.garageId) {
      this.loadExaminations();
    }
  }

  loadExaminations(): void {
    this.loading.set(true);
    this.taskService.getPendingExaminations(this.garageId).subscribe({
      next: (exams) => {
        this.examinations.set(exams || []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des rapports d\'examen');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  openReviewModal(exam: PendingExaminationDto): void {
    this.selectedExamination = exam;
    this.reviewForm.reset({ isApproved: true, serviceFee: 100, declineReason: '' });
    this.showReviewModal.set(true);
  }

  submitReview(): void {
    if (!this.reviewForm.valid || !this.selectedExamination) return;

    const form = this.reviewForm.value;
    this.isSubmitting.set(true);
    this.error.set(null);

    this.taskService.reviewExamination(
      this.selectedExamination.repairTaskId,
      form.isApproved,
      form.serviceFee,
      form.declineReason || undefined
    ).subscribe({
      next: () => {
        this.success.set(form.isApproved
          ? 'Examen approuvé ! Devis envoyé au client.'
          : 'Examen refusé. Facture d\'examen créée.');
        this.showReviewModal.set(false);
        this.selectedExamination = null;
        this.isSubmitting.set(false);
        this.loadExaminations();
        setTimeout(() => this.success.set(null), 4000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la révision');
        this.isSubmitting.set(false);
      },
    });
  }

  get totalPartsEstimate(): number {
    if (!this.selectedExamination) return 0;
    return this.calcPartsTotal(this.selectedExamination.partsNeeded);
  }

  calcPartsTotal(parts: { quantity: number; estimatedPrice: number }[]): number {
    return parts.reduce((sum, p) => sum + p.quantity * p.estimatedPrice, 0);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      year: 'numeric', month: 'long', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }
}

