import { Component, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RepairTaskService, SparePartsService } from '../../core/services/workshop.service';
import { PendingExaminationDto, ReviewPartDto } from '../../core/models/workshop.models';
import { Subject, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SparePartDto } from '../../core/models/workshop.models';

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

  // ── Editable parts in review modal ───────────────────────────────────────
  editableParts = signal<ReviewPartDto[]>([]);
  partSearchInput$ = new Subject<string>();
  partSearchResults = signal<SparePartDto[]>([]);
  showPartDropdown = signal(false);
  partSearchTerm = signal('');

  constructor(
    private taskService: RepairTaskService,
    private sparePartsService: SparePartsService,
    private fb: FormBuilder
  ) {
    this.reviewForm = this.fb.group({
      isApproved:           [true, Validators.required],
      serviceFee:           [50, [Validators.required, Validators.min(0)]],
      declineReason:        [''],
      updatedObservations:  [''],
    });

    // Autocomplete wiring
    this.partSearchInput$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => {
        if (!term || term.length < 2 || !this.garageId) return of([]);
        return this.sparePartsService.getStock(this.garageId, undefined, term);
      }),
      takeUntilDestroyed()
    ).subscribe({
      next: results => {
        this.partSearchResults.set(results);
        this.showPartDropdown.set(results.length > 0);
      },
      error: () => this.partSearchResults.set([])
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
      next: (exams) => { this.examinations.set(exams || []); this.loading.set(false); },
      error: (err) => { this.error.set('Erreur lors du chargement des rapports d\'examen'); console.error(err); this.loading.set(false); },
    });
  }

  openReviewModal(exam: PendingExaminationDto): void {
    this.selectedExamination = exam;
    // Pre-fill form with mechanic's data
    this.reviewForm.reset({
      isApproved:          true,
      serviceFee:          50,
      declineReason:       '',
      updatedObservations: exam.examinationObservations || '',
    });
    // Pre-fill parts from mechanic's list
    this.editableParts.set(
      exam.partsNeeded.map(p => ({
        sparePartId: p.sparePartId || null as any,  // null if empty — not a valid Guid
        name:        p.name,
        quantity:    p.quantity,
        unitPrice:   p.estimatedPrice,
      }))
    );
    this.partSearchTerm.set('');
    this.partSearchResults.set([]);
    this.showPartDropdown.set(false);
    this.showReviewModal.set(true);
  }

  // ── Parts editing ─────────────────────────────────────────────────────────
  onPartSearchChange(value: string): void {
    this.partSearchTerm.set(value);
    this.partSearchInput$.next(value);
    if (!value) this.showPartDropdown.set(false);
  }

  selectPart(part: SparePartDto): void {
    const already = this.editableParts().find(p => p.sparePartId === part.id);
    if (!already) {
      this.editableParts.update(list => [...list, {
        sparePartId: part.id,   // valid Guid from stock
        name:        part.name,
        quantity:    1,
        unitPrice:   part.unitPrice,
      }]);
    }
    this.partSearchTerm.set('');
    this.partSearchResults.set([]);
    this.showPartDropdown.set(false);
  }

  updatePartQty(index: number, qty: number): void {
    this.editableParts.update(list => {
      const updated = [...list];
      updated[index] = { ...updated[index], quantity: Math.max(1, qty) };
      return updated;
    });
  }

  updatePartPrice(index: number, price: number): void {
    this.editableParts.update(list => {
      const updated = [...list];
      updated[index] = { ...updated[index], unitPrice: Math.max(0, price) };
      return updated;
    });
  }

  removePart(index: number): void {
    this.editableParts.update(list => list.filter((_, i) => i !== index));
  }

  get partsTotal(): number {
    return this.editableParts().reduce((s, p) => s + p.unitPrice * p.quantity, 0);
  }

  submitReview(): void {
    if (!this.reviewForm.valid || !this.selectedExamination) return;

    const form = this.reviewForm.value;
    // <select> bound with [value]="true/false" returns a string after user interaction — coerce to real boolean
    const isApproved: boolean = form.isApproved === true || form.isApproved === 'true';
    this.isSubmitting.set(true);
    this.error.set(null);

    const updatedParts = this.editableParts().length > 0 ? this.editableParts() : undefined;

    this.taskService.reviewExamination(
      this.selectedExamination.repairTaskId,
      isApproved,
      form.serviceFee,
      form.declineReason || undefined,
      form.updatedObservations || undefined,
      updatedParts
    ).subscribe({
      next: () => {
        this.success.set(isApproved
          ? '✅ Rapport approuvé ! Devis envoyé au client.'
          : '❌ Rapport refusé. Facture d\'examen créée.');
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

