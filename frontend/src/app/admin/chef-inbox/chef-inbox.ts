import { CommonModule } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ChefInboxItemDto } from '../../core/models/workshop.models';
import { SymptomReportService } from '../../core/services/workshop.service';

@Component({
  selector: 'app-chef-inbox',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './chef-inbox.html',
  styleUrls: ['./chef-inbox.css'],
})
export class ChefInboxComponent implements OnInit {
  private _garageId = '';

  @Input()
  set garageId(value: string) {
    this._garageId = value ?? '';
    if (this._garageId) {
      this.loadInbox();
    }
  }

  get garageId(): string {
    return this._garageId;
  }

  inbox: ChefInboxItemDto[] = [];
  loading = false;
  isSubmitting = false;
  error: string | null = null;
  success: string | null = null;
  showFeedbackModal = false;

  selectedItemId: string | null = null;
  feedbackForm: FormGroup;

  constructor(
    private readonly symptomService: SymptomReportService,
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute
  ) {
    this.feedbackForm = this.fb.group({
      feedback: ['', [Validators.required, Validators.minLength(20)]],
      newStatus: ['Approved', Validators.required],
      availablePeriodStart: [null],
      availablePeriodEnd: [null],
    });
  }

  ngOnInit(): void {
    if (!this.garageId) {
      this.route.params.subscribe((params) => {
        const routeGarageId = params['garageId'] ?? '';
        if (routeGarageId) {
          this.garageId = routeGarageId;
        }
      });
    }
  }

  loadInbox(): void {
    if (!this.garageId) {
      this.error = "Garage introuvable pour l'inbox du chef";
      return;
    }

    this.loading = true;
    this.error = null;

    this.symptomService.getChefInbox(this.garageId).subscribe({
      next: (response) => {
        this.inbox = response;
        this.loading = false;
      },
      error: (err) => {
        this.error = "Erreur lors du chargement de l'inbox du chef";
        console.error(err);
        this.loading = false;
      },
    });
  }

  openReview(reviewId: string): void {
    this.selectedItemId = reviewId;
    this.showFeedbackModal = true;
    this.feedbackForm.reset({ newStatus: 'Approved' });
  }

  submitReview(): void {
    if (!this.feedbackForm.valid || !this.selectedItemId) {
      this.error = 'Veuillez remplir tous les champs correctement';
      return;
    }

    const formValue = this.feedbackForm.value;
    if (formValue.newStatus === 'Approved' && formValue.availablePeriodStart && formValue.availablePeriodEnd) {
      const start = new Date(formValue.availablePeriodStart);
      const end = new Date(formValue.availablePeriodEnd);
      if (start > end) {
        this.error = 'La date de debut ne peut pas etre superieure a la date de fin.';
        return;
      }
    }

    this.isSubmitting = true;
    this.error = null;

    const feedbackData = {
      feedback: formValue.feedback,
      newStatus: formValue.newStatus,
      availablePeriodStart: formValue.newStatus === 'Approved' ? formValue.availablePeriodStart : null,
      availablePeriodEnd: formValue.newStatus === 'Approved' ? formValue.availablePeriodEnd : null,
    };

    this.symptomService.addFeedback(this.selectedItemId, feedbackData).subscribe({
      next: () => {
        this.success = 'Le retour du chef a été envoyé avec succès';
        this.feedbackForm.reset({ newStatus: 'Approved' });
        this.showFeedbackModal = false;
        this.selectedItemId = null;
        this.loadInbox();
        this.isSubmitting = false;
        setTimeout(() => (this.success = null), 3000);
      },
      error: (err) => {
        this.error = err.error?.message || "Erreur lors de l'envoi du retour";
        this.isSubmitting = false;
      },
    });
  }

  closeModal(): void {
    this.showFeedbackModal = false;
    this.feedbackForm.reset({ newStatus: 'Approved' });
    this.error = null;
    this.selectedItemId = null;
  }

  getSelectedItem(): ChefInboxItemDto | undefined {
    return this.inbox.find((item) => item.id === this.selectedItemId);
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
}

