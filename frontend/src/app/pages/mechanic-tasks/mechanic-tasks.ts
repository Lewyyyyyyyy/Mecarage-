import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { Subject, debounceTime, switchMap, distinctUntilChanged, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { RepairTaskService, SparePartsService } from '../../core/services/workshop.service';
import { MechanicTaskDto, SparePartDto, SelectedSparePartDto } from '../../core/models/workshop.models';

@Component({
  selector: 'app-mechanic-tasks',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './mechanic-tasks.html',
  styleUrls: ['./mechanic-tasks.css'],
})
export class MechanicTasksComponent implements OnInit, OnDestroy {
  tasks = signal<MechanicTaskDto[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  submitMode = signal<'save' | 'submit'>('save');
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  // ── Unified task modal ───────────────────────────────────────────────────
  showTaskModal = signal(false);
  selectedTaskId: string | null = null;
  taskForm!: FormGroup;

  // ── Repair completion modal (post-invoice phase) ─────────────────────────
  showRepairModal = signal(false);
  repairForm!: FormGroup;
  selectedRepairTaskId: string | null = null;

  // ── File upload ──────────────────────────────────────────────────────────
  selectedFile = signal<File | null>(null);
  isUploadingFile = signal(false);

  // ── Parts autocomplete ───────────────────────────────────────────────────
  partSearchInput$ = new Subject<string>();
  partSearchResults = signal<SparePartDto[]>([]);
  showPartDropdown = signal(false);
  selectedParts = signal<SelectedSparePartDto[]>([]);
  partSearchTerm = signal('');

  private destroy$ = new Subject<void>();

  constructor(
    private readonly taskService: RepairTaskService,
    private readonly sparePartsService: SparePartsService,
    private readonly fb: FormBuilder
  ) {
    this.taskForm = this.fb.group({
      mechanicNotes: [''],
    });
    this.repairForm = this.fb.group({
      completionNotes: [''],
    });
  }

  ngOnInit(): void {
    this.loadTasks();

    this.partSearchInput$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => {
        const task = this.getSelectedTask();
        if (!term || term.length < 2 || !task) return of([]);
        return this.sparePartsService.getStock(task.garageId, undefined, term);
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: results => {
        this.partSearchResults.set(results);
        this.showPartDropdown.set(results.length > 0);
      },
      error: () => this.partSearchResults.set([])
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.taskService.getMyTasks().subscribe({
      next: tasks => { this.tasks.set(tasks); this.loading.set(false); },
      error: err => { this.error.set('Erreur lors du chargement des tâches'); console.error(err); this.loading.set(false); },
    });
  }

  // ── Modal open/close ─────────────────────────────────────────────────────
  openTaskModal(taskId: string): void {
    this.selectedTaskId = taskId;
    this.taskForm.reset({ mechanicNotes: '' });
    this.selectedFile.set(null);
    this.selectedParts.set([]);
    this.partSearchTerm.set('');
    this.partSearchResults.set([]);
    this.showPartDropdown.set(false);
    this.error.set(null);
    this.showTaskModal.set(true);
  }

  closeTaskModal(): void {
    this.showTaskModal.set(false);
    this.selectedTaskId = null;
    this.error.set(null);
    this.selectedParts.set([]);
    this.showPartDropdown.set(false);
  }

  // ── File ──────────────────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.selectedFile.set(input.files[0]);
  }

  // ── Parts autocomplete ────────────────────────────────────────────────────
  onPartSearchChange(value: string): void {
    this.partSearchTerm.set(value);
    this.partSearchInput$.next(value);
    if (!value) this.showPartDropdown.set(false);
  }

  selectPart(part: SparePartDto): void {
    const already = this.selectedParts().find(p => p.sparePartId === part.id);
    if (!already) {
      this.selectedParts.update(list => [...list, {
        sparePartId: part.id,
        name: part.name,
        quantity: 1,
        unitPrice: part.unitPrice,
      }]);
    }
    this.partSearchTerm.set('');
    this.partSearchResults.set([]);
    this.showPartDropdown.set(false);
  }

  updatePartQty(index: number, qty: number): void {
    this.selectedParts.update(list => {
      const updated = [...list];
      updated[index] = { ...updated[index], quantity: Math.max(1, qty) };
      return updated;
    });
  }

  removePart(index: number): void {
    this.selectedParts.update(list => list.filter((_, i) => i !== index));
  }

  getPartsTotal(): number {
    return this.selectedParts().reduce((s, p) => s + p.unitPrice * p.quantity, 0);
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  saveProgress(): void {
    this.submitMode.set('save');
    this.doSubmit(false);
  }

  submitToChef(): void {
    this.submitMode.set('submit');
    this.doSubmit(true);
  }

  private doSubmit(submitToChef: boolean): void {
    if (!this.selectedTaskId) return;

    this.isSubmitting.set(true);
    this.error.set(null);

    const execute = (fileUrl: string | null) => {
      const data = {
        submitToChef,
        mechanicNotes: this.taskForm.value.mechanicNotes || null,
        fileUrl,
        parts: this.selectedParts().length ? this.selectedParts() : undefined,
      };

      this.taskService.updateMechanicTask(this.selectedTaskId!, data).subscribe({
        next: () => {
          this.success.set(submitToChef
            ? '✅ Rapport soumis au chef pour validation !'
            : '💾 Progression sauvegardée');
          this.closeTaskModal();
          this.loadTasks();
          this.isSubmitting.set(false);
          setTimeout(() => this.success.set(null), 3500);
        },
        error: err => {
          this.error.set(err.error?.message || 'Erreur lors de la mise à jour');
          this.isSubmitting.set(false);
        },
      });
    };

    const file = this.selectedFile();
    if (file) {
      this.isUploadingFile.set(true);
      this.taskService.uploadExaminationFile(this.selectedTaskId!, file).subscribe({
        next: res => { this.isUploadingFile.set(false); execute(res.fileUrl); },
        error: err => {
          this.isUploadingFile.set(false);
          this.error.set(err.error?.message || 'Erreur lors du téléversement');
          this.isSubmitting.set(false);
        },
      });
    } else {
      execute(null);
    }
  }

  // ── Repair completion (post-invoice) ─────────────────────────────────────
  openRepairModal(taskId: string): void {
    this.selectedRepairTaskId = taskId;
    this.repairForm.reset({ completionNotes: '' });
    this.selectedFile.set(null);
    this.error.set(null);
    this.showRepairModal.set(true);
  }

  closeRepairModal(): void {
    this.showRepairModal.set(false);
    this.selectedRepairTaskId = null;
    this.error.set(null);
  }

  submitRepairToChef(): void {
    if (!this.selectedRepairTaskId) return;
    this.isSubmitting.set(true);
    this.error.set(null);

    const execute = (fileUrl: string | null) => {
      this.taskService.submitRepairCompletion(this.selectedRepairTaskId!, {
        completionNotes: this.repairForm.value.completionNotes || null,
        fileUrl,
      }).subscribe({
        next: () => {
          this.success.set('✅ Réparation soumise au chef pour validation !');
          this.closeRepairModal();
          this.loadTasks();
          this.isSubmitting.set(false);
          setTimeout(() => this.success.set(null), 4000);
        },
        error: err => {
          this.error.set(err.error?.message || 'Erreur lors de la soumission');
          this.isSubmitting.set(false);
        },
      });
    };

    const file = this.selectedFile();
    if (file) {
      this.isUploadingFile.set(true);
      this.taskService.uploadExaminationFile(this.selectedRepairTaskId!, file).subscribe({
        next: res => { this.isUploadingFile.set(false); execute(res.fileUrl); },
        error: err => {
          this.isUploadingFile.set(false);
          this.error.set(err.error?.message || 'Erreur lors du téléversement');
          this.isSubmitting.set(false);
        },
      });
    } else {
      execute(null);
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  getSelectedTask(): MechanicTaskDto | undefined {
    return this.tasks().find(t => t.id === this.selectedTaskId);
  }

  canOpenModal(status: string): boolean {
    return !['Done', 'Cancelled', 'Fixed'].includes(status);
  }

  canSubmitRepair(task: MechanicTaskDto): boolean {
    return task.invoiceApproved &&
      !['Done', 'Cancelled', 'Fixed'].includes(task.status);
  }

  getStatusBadge(status: string): { bg: string; text: string; label: string } {
    const map: Record<string, { bg: string; text: string; label: string }> = {
      Assigned:   { bg: 'bg-blue-100 dark:bg-blue-900/30',   text: 'text-blue-800 dark:text-blue-300',   label: 'Assigné' },
      InProgress: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-300', label: 'En cours' },
      Fixed:      { bg: 'bg-purple-100 dark:bg-purple-900/30', text: 'text-purple-800 dark:text-purple-300', label: 'Réparé' },
      Tested:     { bg: 'bg-teal-100 dark:bg-teal-900/30',   text: 'text-teal-800 dark:text-teal-300',   label: 'Testé' },
      Done:       { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: 'Terminé' },
      OnHold:     { bg: 'bg-orange-100 dark:bg-orange-900/30', text: 'text-orange-800 dark:text-orange-300', label: 'En suspens' },
      Cancelled:  { bg: 'bg-red-100 dark:bg-red-900/30',    text: 'text-red-800 dark:text-red-300',    label: 'Annulé' },
    };
    return map[status] ?? map['Assigned'];
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }

  getActiveTasks(): MechanicTaskDto[] {
    return this.tasks().filter(t => t.status !== 'Done' && t.status !== 'Cancelled');
  }

  getCompletedTasks(): MechanicTaskDto[] {
    return this.tasks().filter(t => t.status === 'Done' || t.status === 'Cancelled');
  }
}

