import { Component, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SymptomReportService } from '../../core/services/workshop.service';

interface PendingReview {
  id: string;
  clientId: string;
  clientName: string;
  vehicleId: string;
  vehicleInfo: string;
  symptomsDescription: string;
  submittedAt: string;
}

@Component({
  selector: 'app-chef-pending-reviews',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './chef-pending-reviews.html',
  styleUrls: ['./chef-pending-reviews.css'],
})
export class ChefPendingReviewsComponent implements OnInit {
  @Input() garageId!: string;

  reviews = signal<PendingReview[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  selectedReviewId: string | null = null;
  feedbackForm!: FormGroup;
  showFeedbackModal = signal(false);

  constructor(
    private readonly symptomService: SymptomReportService,
    private readonly fb: FormBuilder
  ) {
    this.feedbackForm = this.fb.group({
      feedback: ['', [Validators.required, Validators.minLength(20)]],
      newStatus: ['Approved', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadPendingReviews();
  }

  loadPendingReviews(): void {
    this.loading.set(true);
    this.symptomService.getPendingReviews(this.garageId).subscribe({
      next: (response) => {
        this.reviews.set(response);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des examens en attente');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  openFeedbackModal(reviewId: string): void {
    this.selectedReviewId = reviewId;
    this.showFeedbackModal.set(true);
    this.feedbackForm.reset({ newStatus: 'Approved' });
  }

  onSubmitFeedback(): void {
    if (!this.feedbackForm.valid || !this.selectedReviewId) {
      this.error.set('Veuillez remplir tous les champs correctement');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    this.symptomService.addFeedback(this.selectedReviewId, this.feedbackForm.value).subscribe({
      next: () => {
        this.success.set('Feedback enregistré avec succès');
        this.feedbackForm.reset();
        this.showFeedbackModal.set(false);
        this.selectedReviewId = null;
        this.loadPendingReviews();
        this.isSubmitting.set(false);
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de l\'enregistrement du feedback');
        this.isSubmitting.set(false);
      },
    });
  }

  closeModal(): void {
    this.showFeedbackModal.set(false);
    this.feedbackForm.reset();
    this.error.set(null);
    this.selectedReviewId = null;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  getSelectedReview(): PendingReview | undefined {
    return this.reviews().find((r) => r.id === this.selectedReviewId);
  }
}

