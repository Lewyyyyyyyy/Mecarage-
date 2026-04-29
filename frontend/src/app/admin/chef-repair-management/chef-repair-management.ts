import { Component, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { RepairTaskService } from '../../core/services/workshop.service';
import { RepairReadyTaskDto } from '../../core/models/workshop.models';
import { StaffService } from '../../core/services/staff.service';
import { StaffDto } from '../../core/models/staff.models';

@Component({
  selector: 'app-chef-repair-management',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chef-repair-management.html',
})
export class ChefRepairManagementComponent implements OnInit {
  @Input() garageId = '';

  repairTasks = signal<RepairReadyTaskDto[]>([]);
  mechanics = signal<StaffDto[]>([]);
  loading = signal(false);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  showAssignModal = signal(false);
  selectedTask = signal<RepairReadyTaskDto | null>(null);
  selectedMechanicIds: string[] = [];

  constructor(
    private taskService: RepairTaskService,
    private staffService: StaffService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (!this.garageId) {
      this.route.params.subscribe(params => {
        if (params['garageId']) {
          this.garageId = params['garageId'];
          this.loadData();
        }
      });
    } else {
      this.loadData();
    }
  }

  loadData(): void {
    this.loading.set(true);
    this.taskService.getRepairReadyTasks(this.garageId).subscribe({
      next: (tasks) => {
        this.repairTasks.set(tasks || []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Erreur lors du chargement des réparations');
        this.loading.set(false);
      },
    });
    this.staffService.getGarageStaff(this.garageId).subscribe({
      next: (staff) => this.mechanics.set(staff.filter(s => s.role === 'Mecanicien')),
      error: () => {},
    });
  }

  openAssignModal(task: RepairReadyTaskDto): void {
    this.selectedTask.set(task);
    this.selectedMechanicIds = [];
    this.error.set(null);
    this.showAssignModal.set(true);
  }

  toggleMechanic(id: string): void {
    if (this.selectedMechanicIds.includes(id)) {
      this.selectedMechanicIds = this.selectedMechanicIds.filter(x => x !== id);
    } else {
      this.selectedMechanicIds = [...this.selectedMechanicIds, id];
    }
  }

  isMechanicSelected(id: string): boolean {
    return this.selectedMechanicIds.includes(id);
  }

  assignMechanics(): void {
    const task = this.selectedTask();
    if (!task || this.selectedMechanicIds.length === 0) {
      this.error.set('Sélectionnez au moins un mécanicien');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    // Assign each mechanic one by one
    const assigns = this.selectedMechanicIds.map(mechanicId =>
      this.taskService.assignMechanic(task.taskId, mechanicId).toPromise()
    );

    Promise.allSettled(assigns).then(results => {
      const failed = results.filter(r => r.status === 'rejected').length;
      if (failed === 0) {
        this.success.set(`${this.selectedMechanicIds.length} mécanicien(s) assigné(s) pour la réparation !`);
      } else {
        this.success.set(`Assignation partielle : ${results.length - failed} réussi(s), ${failed} échoué(s).`);
      }
      this.showAssignModal.set(false);
      this.isSubmitting.set(false);
      this.loadData();
      setTimeout(() => this.success.set(null), 4000);
    });
  }

  markTested(taskId: string): void {
    if (!confirm('Confirmer que le véhicule a passé le test de qualité ?')) return;
    this.taskService.markTaskTested(taskId).subscribe({
      next: () => {
        this.success.set('Tâche marquée comme testée. Vous pouvez maintenant signaler la disponibilité au client.');
        this.loadData();
        setTimeout(() => this.success.set(null), 4000);
      },
      error: (err) => this.error.set(err.error?.message || 'Erreur lors du marquage'),
    });
  }

  signalReadyForPickup(taskId: string): void {
    if (!confirm('Confirmer que le véhicule est prêt ? Le client sera notifié.')) return;
    this.taskService.signalReadyForPickup(taskId).subscribe({
      next: () => {
        this.success.set('Client notifié ! Le véhicule est prêt à être récupéré.');
        this.loadData();
        setTimeout(() => this.success.set(null), 4000);
      },
      error: (err) => this.error.set(err.error?.message || 'Erreur lors de la notification'),
    });
  }

  getStatusBadge(status: string): { bg: string; text: string; label: string } {
    const map: Record<string, { bg: string; text: string; label: string }> = {
      Assigned:   { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-300', label: '📋 Assigné' },
      InProgress: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-300', label: '🔧 En cours' },
      Fixed:      { bg: 'bg-purple-100 dark:bg-purple-900/30', text: 'text-purple-800 dark:text-purple-300', label: '✅ Réparé' },
      Tested:     { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: '🧪 Testé' },
      Done:       { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: '✔ Terminé' },
      OnHold:     { bg: 'bg-orange-100 dark:bg-orange-900/30', text: 'text-orange-800 dark:text-orange-300', label: '⏸ En suspens' },
    };
    return map[status] || map['Assigned'];
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      year: 'numeric', month: 'long', day: 'numeric',
    });
  }

  getPendingTasks(): RepairReadyTaskDto[] {
    return this.repairTasks().filter(t => t.taskStatus === 'Assigned' || t.taskStatus === 'InProgress' || t.taskStatus === 'OnHold');
  }

  getFixedTasks(): RepairReadyTaskDto[] {
    return this.repairTasks().filter(t => t.taskStatus === 'Fixed');
  }

  getTestedTasks(): RepairReadyTaskDto[] {
    return this.repairTasks().filter(t => t.taskStatus === 'Tested');
  }
}

