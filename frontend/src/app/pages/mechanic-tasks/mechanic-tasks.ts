import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { RepairTaskService } from '../../core/services/workshop.service';
import { MechanicTaskDto } from '../../core/models/workshop.models';

interface TaskWithDetails extends MechanicTaskDto {
  details?: any;
}

@Component({
  selector: 'app-mechanic-tasks',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './mechanic-tasks.html',
  styleUrls: ['./mechanic-tasks.css'],
})
export class MechanicTasksComponent implements OnInit {
  tasks = signal<MechanicTaskDto[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  selectedTaskId: string | null = null;
  statusForm!: FormGroup;
  showStatusModal = signal(false);
  showExaminationModal = signal(false);
  examinationForm!: FormGroup;

  // File upload state
  selectedFile = signal<File | null>(null);
  isUploadingFile = signal(false);
  uploadedFileUrl = signal<string | null>(null);

  validStatuses: Record<string, string[]> = {
    Assigned: ['InProgress'],
    InProgress: ['Fixed'],
    Fixed: ['Tested'],
    Tested: ['Done', 'InProgress'],
    OnHold: ['InProgress'],
    Done: [],
    Cancelled: [],
  };

  constructor(
    private readonly taskService: RepairTaskService,
    private readonly fb: FormBuilder
  ) {
    this.statusForm = this.fb.group({
      newStatus: ['', Validators.required],
      completionNotes: [''],
    });
    this.examinationForm = this.fb.group({
      examinationObservations: ['', [Validators.required, Validators.minLength(20)]],
      partsNeeded: this.fb.array([]),
    });
  }

  get partsArray(): FormArray {
    return this.examinationForm.get('partsNeeded') as FormArray;
  }

  addPart(): void {
    this.partsArray.push(this.fb.group({
      name: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      estimatedPrice: [0, [Validators.required, Validators.min(0)]],
    }));
  }

  removePart(index: number): void {
    this.partsArray.removeAt(index);
  }

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.taskService.getMyTasks().subscribe({
      next: (tasks) => {
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des tâches');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  openStatusModal(taskId: string): void {
    this.selectedTaskId = taskId;
    const task = this.tasks().find((t) => t.id === taskId);
    if (task) {
      const validNextStatuses = this.validStatuses[task.status] || [];
      this.statusForm.patchValue({
        newStatus: validNextStatuses[0] || task.status,
      });
      this.showStatusModal.set(true);
    }
  }

  onSubmitStatus(): void {
    if (!this.statusForm.valid || !this.selectedTaskId) {
      this.error.set('Veuillez sélectionner un statut');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    const request = {
      newStatus: this.statusForm.get('newStatus')?.value,
      completionNotes: this.statusForm.get('completionNotes')?.value,
    };

    this.taskService.updateTaskStatus(this.selectedTaskId, request).subscribe({
      next: () => {
        this.success.set('Tâche mise à jour avec succès');
        this.statusForm.reset();
        this.showStatusModal.set(false);
        this.selectedTaskId = null;
        this.loadTasks();
        this.isSubmitting.set(false);
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la mise à jour');
        this.isSubmitting.set(false);
      },
    });
  }

  closeModal(): void {
    this.showStatusModal.set(false);
    this.statusForm.reset();
    this.error.set(null);
    this.selectedTaskId = null;
  }

  openExaminationModal(taskId: string): void {
    this.selectedTaskId = taskId;
    this.examinationForm.reset();
    this.partsArray.clear();
    this.selectedFile.set(null);
    this.uploadedFileUrl.set(null);
    this.showExaminationModal.set(true);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile.set(input.files[0]);
      this.uploadedFileUrl.set(null);
    }
  }

  submitExaminationReport(): void {
    if (!this.examinationForm.valid || !this.selectedTaskId) {
      this.error.set('Veuillez remplir tous les champs');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    const doSubmit = (fileUrl: string | null) => {
      const data = {
        examinationObservations: this.examinationForm.value.examinationObservations,
        partsNeeded: this.partsArray.value,
        fileUrl,
      };

      this.taskService.submitExaminationReport(this.selectedTaskId!, data).subscribe({
        next: () => {
          this.success.set('Rapport d\'examen soumis avec succès');
          this.showExaminationModal.set(false);
          this.selectedTaskId = null;
          this.loadTasks();
          this.isSubmitting.set(false);
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Erreur lors de la soumission');
          this.isSubmitting.set(false);
        },
      });
    };

    const file = this.selectedFile();
    if (file && this.selectedTaskId) {
      this.isUploadingFile.set(true);
      this.taskService.uploadExaminationFile(this.selectedTaskId, file).subscribe({
        next: (res) => {
          this.isUploadingFile.set(false);
          this.uploadedFileUrl.set(res.fileUrl);
          doSubmit(res.fileUrl);
        },
        error: (err) => {
          this.isUploadingFile.set(false);
          this.error.set(err.error?.message || 'Erreur lors du téléversement du fichier');
          this.isSubmitting.set(false);
        },
      });
    } else {
      doSubmit(null);
    }
  }

  closeExaminationModal(): void {
    this.showExaminationModal.set(false);
    this.examinationForm.reset();
    this.partsArray.clear();
    this.selectedFile.set(null);
    this.uploadedFileUrl.set(null);
    this.error.set(null);
    this.selectedTaskId = null;
  }

  getSelectedTask(): MechanicTaskDto | undefined {
    return this.tasks().find((t) => t.id === this.selectedTaskId);
  }

  getValidNextStatuses(status: string): string[] {
    return this.validStatuses[status] || [];
  }

  getStatusBadge(status: string): { bg: string; text: string; label: string } {
    const statusMap: Record<string, { bg: string; text: string; label: string }> = {
      Assigned: { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-300', label: 'Assigné' },
      InProgress: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-300', label: 'En cours' },
      Fixed: { bg: 'bg-purple-100 dark:bg-purple-900/30', text: 'text-purple-800 dark:text-purple-300', label: 'Réparé' },
      Tested: { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: 'Testé' },
      Done: { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: 'Fait' },
      OnHold: { bg: 'bg-orange-100 dark:bg-orange-900/30', text: 'text-orange-800 dark:text-orange-300', label: 'En suspens' },
      Cancelled: { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-800 dark:text-red-300', label: 'Annulé' },
    };
    return statusMap[status] || statusMap['Assigned'];
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

  getActiveTasks(): MechanicTaskDto[] {
    return this.tasks().filter((t) => t.status !== 'Done' && t.status !== 'Cancelled');
  }

  getCompletedTasks(): MechanicTaskDto[] {
    return this.tasks().filter((t) => t.status === 'Done' || t.status === 'Cancelled');
  }
}

