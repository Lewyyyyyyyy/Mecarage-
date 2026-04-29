import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../auth/auth.service';
import { GaragesService } from '../services/garages.service';
import { SymptomReportService, RepairTaskService, NotificationService, AppointmentService } from '../services/workshop.service';
import { StaffService } from '../services/staff.service';
import { ChefInboxItemDto, ClientNotificationDto, MechanicTaskDto, PendingAppointmentDto } from '../models/workshop.models';
import { StaffDto } from '../models/staff.models';
import { ChefRepairManagementComponent } from '../../admin/chef-repair-management/chef-repair-management';

@Component({
  selector: 'app-inbox',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ChefRepairManagementComponent],
  templateUrl: './inbox.html',
})
export class InboxComponent implements OnInit {
  private auth = inject(AuthService);
  private garagesService = inject(GaragesService);
  private symptomService = inject(SymptomReportService);
  private repairTaskService = inject(RepairTaskService);
  private notificationService = inject(NotificationService);
  private appointmentService = inject(AppointmentService);
  private staffService = inject(StaffService);
  private fb = inject(FormBuilder);

  // ── Shared state ──────────────────────────────────────────────────────────
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  role = computed(() => this.auth.user()?.role ?? '');
  isChef = computed(() => this.role() === 'ChefAtelier' || this.role() === 'AdminEntreprise');
  isClient = computed(() => this.role() === 'Client');
  isMechanic = computed(() => this.role() === 'Mecanicien');

  // ── Chef tabs ──────────────────────────────────────────────────────────────
  chefTab = signal<'reports' | 'appointments' | 'repairs'>('reports');

  // ── Chef inbox (symptom reports) ───────────────────────────────────────────
  chefInbox = signal<ChefInboxItemDto[]>([]);
  garageId = signal('');
  showFeedbackModal = signal(false);
  selectedItemId = signal<string | null>(null);
  isSubmitting = signal(false);
  feedbackForm: FormGroup;

  // ── Appointments ───────────────────────────────────────────────────────────
  appointments = signal<PendingAppointmentDto[]>([]);
  mechanics = signal<StaffDto[]>([]);
  showTaskModal = signal(false);
  selectedAppointment = signal<PendingAppointmentDto | null>(null);
  taskForm: FormGroup;
  isCreatingTask = signal(false);

  // ── Client notifications ───────────────────────────────────────────────────
  notifications = signal<ClientNotificationDto[]>([]);
  unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  // ── Mechanic tasks ─────────────────────────────────────────────────────────
  tasks = signal<MechanicTaskDto[]>([]);

  constructor() {
    this.feedbackForm = this.fb.group({
      feedback: ['', [Validators.required, Validators.minLength(20)]],
      newStatus: ['Approved', Validators.required],
      availablePeriodStart: [null],
      availablePeriodEnd: [null],
    });
    this.taskForm = this.fb.group({
      taskTitle: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      estimatedMinutes: [null],
      mechanicIds: [[], Validators.required],
    });
  }

  ngOnInit(): void {
    if (this.isChef()) {
      this.initChefInbox();
    } else if (this.isClient()) {
      this.loadNotifications();
    } else if (this.isMechanic()) {
      this.loadTasks();
    }
  }

  isReviewed(item: ChefInboxItemDto): boolean {
    return ['Approved', 'Reviewed', 'Declined'].includes(item.status);
  }

  statusBadge(status: string): { label: string; css: string } {
    const map: Record<string, { label: string; css: string }> = {
      Submitted:    { label: 'En attente',   css: 'bg-amber-900/30 text-amber-400 border-amber-800' },
      PendingReview:{ label: 'En attente',   css: 'bg-amber-900/30 text-amber-400 border-amber-800' },
      Approved:     { label: '✅ Approuvé',  css: 'bg-emerald-900/30 text-emerald-400 border-emerald-800' },
      Reviewed:     { label: '📋 Examiné',  css: 'bg-blue-900/30 text-blue-400 border-blue-800' },
      Declined:     { label: '❌ Refusé',   css: 'bg-rose-900/30 text-rose-400 border-rose-800' },
    };
    return map[status] ?? { label: status, css: 'bg-gray-800 text-gray-400 border-gray-700' };
  }

  // ── Chef ───────────────────────────────────────────────────────────────────

  private initChefInbox(): void {
    const jwtGarageId = this.auth.user()?.garageId;
    if (jwtGarageId) {
      this.garageId.set(jwtGarageId);
      this.loadChefData(jwtGarageId);
      return;
    }

    this.loading.set(true);
    const userId = this.auth.user()?.id;
    this.garagesService.getMyGarages().subscribe({
      next: (garages) => {
        if (!garages?.length) {
          this.error.set('Aucun garage associé à votre compte.');
          this.loading.set(false);
          return;
        }
        const mine = garages.find(g => g.adminId === userId) ?? garages[0];
        this.garageId.set(mine.id);
        this.loadChefData(mine.id);
      },
      error: () => {
        this.error.set('Impossible de déterminer votre garage.');
        this.loading.set(false);
      },
    });
  }

  loadChefData(garageId: string): void {
    this.loadChefInbox(garageId);
    this.loadAppointments(garageId);
    this.loadMechanics(garageId);
  }

  loadChefInbox(garageId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.symptomService.getPendingReviews(garageId).subscribe({
      next: (items) => {
        this.chefInbox.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set("Erreur lors du chargement de l'inbox.");
        this.loading.set(false);
      },
    });
  }

  openReview(id: string): void {
    this.selectedItemId.set(id);
    this.showFeedbackModal.set(true);
    this.error.set(null);

    // Pre-fill with existing data if already reviewed
    const item = this.chefInbox().find(i => i.id === id);
    this.feedbackForm.reset({
      feedback: item?.chefFeedback ?? '',
      newStatus: item?.status === 'Declined' ? 'Declined' : 'Approved',
      availablePeriodStart: item?.availablePeriodStart
        ? item.availablePeriodStart.substring(0, 16) : null,
      availablePeriodEnd: item?.availablePeriodEnd
        ? item.availablePeriodEnd.substring(0, 16) : null,
    });
  }

  getSelectedItem(): ChefInboxItemDto | undefined {
    return this.chefInbox().find(i => i.id === this.selectedItemId());
  }

  submitFeedback(): void {
    if (!this.feedbackForm.valid || !this.selectedItemId()) return;
    this.isSubmitting.set(true);
    this.error.set(null);
    const v = this.feedbackForm.value;
    this.symptomService
      .addFeedback(this.selectedItemId()!, {
        feedback: v.feedback,
        newStatus: v.newStatus,
        availablePeriodStart: v.newStatus === 'Approved' ? v.availablePeriodStart : null,
        availablePeriodEnd: v.newStatus === 'Approved' ? v.availablePeriodEnd : null,
      })
      .subscribe({
        next: () => {
          this.success.set('Retour envoyé avec succès.');
          this.showFeedbackModal.set(false);
          this.selectedItemId.set(null);
          this.isSubmitting.set(false);
          this.loadChefInbox(this.garageId());
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(err.error?.message || "Erreur lors de l'envoi du retour.");
          this.isSubmitting.set(false);
        },
      });
  }

  closeModal(): void {
    this.showFeedbackModal.set(false);
    this.selectedItemId.set(null);
    this.feedbackForm.reset({ newStatus: 'Approved' });
    this.error.set(null);
  }

  // ── Appointments ──────────────────────────────────────────────────────────

  loadAppointments(garageId: string): void {
    this.appointmentService.getPendingAppointments(garageId).subscribe({
      next: (items) => this.appointments.set(items),
      error: () => {},
    });
  }

  loadMechanics(garageId: string): void {
    this.staffService.getGarageStaff(garageId).subscribe({
      next: (staff) => this.mechanics.set(staff.filter(s => s.role === 'Mecanicien')),
      error: () => {},
    });
  }

  openTaskModal(appointment: PendingAppointmentDto): void {
    this.selectedAppointment.set(appointment);
    this.showTaskModal.set(true);
    this.taskForm.reset({ mechanicIds: [] });
    this.error.set(null);
  }

  closeTaskModal(): void {
    this.showTaskModal.set(false);
    this.selectedAppointment.set(null);
    this.taskForm.reset({ mechanicIds: [] });
    this.error.set(null);
  }

  toggleMechanic(id: string): void {
    const current: string[] = this.taskForm.get('mechanicIds')!.value ?? [];
    const updated = current.includes(id) ? current.filter(x => x !== id) : [...current, id];
    this.taskForm.get('mechanicIds')!.setValue(updated);
  }

  isMechanicSelected(id: string): boolean {
    return (this.taskForm.get('mechanicIds')!.value ?? []).includes(id);
  }

  approveAndAssign(): void {
    if (!this.taskForm.valid || !this.selectedAppointment()) return;
    const appt = this.selectedAppointment()!;
    this.isCreatingTask.set(true);
    this.error.set(null);

    // Step 1: approve the appointment
    this.appointmentService.approveAppointment(appt.id).subscribe({
      next: () => {
        // Step 2: create repair task with mechanic assignment
        const v = this.taskForm.value;
        this.repairTaskService.createTask({
          appointmentId: appt.id,
          taskTitle: v.taskTitle,
          description: v.description,
          estimatedMinutes: v.estimatedMinutes || undefined,
          mechanicIds: v.mechanicIds,
        }).subscribe({
          next: () => {
            this.success.set(`Rendez-vous confirmé et tâche assignée à ${v.mechanicIds.length} mécanicien(s).`);
            this.closeTaskModal();
            this.isCreatingTask.set(false);
            this.loadAppointments(this.garageId());
            setTimeout(() => this.success.set(null), 4000);
          },
          error: (err) => {
            this.error.set(err.error?.message || 'Erreur lors de la création de la tâche.');
            this.isCreatingTask.set(false);
          },
        });
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la confirmation du rendez-vous.');
        this.isCreatingTask.set(false);
      },
    });
  }

  declineAppointment(id: string): void {
    if (!confirm('Refuser ce rendez-vous ?')) return;
    this.appointmentService.declineAppointment(id, 'Créneau non disponible').subscribe({
      next: () => {
        this.success.set('Rendez-vous refusé.');
        this.loadAppointments(this.garageId());
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => this.error.set(err.error?.message || 'Erreur lors du refus.'),
    });
  }

  // ── Client notifications ───────────────────────────────────────────────────

  loadNotifications(): void {
    this.loading.set(true);
    this.notificationService.getMyNotifications().subscribe({
      next: (items) => {
        this.notifications.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Erreur lors du chargement des notifications.');
        this.loading.set(false);
      },
    });
  }

  markRead(id: string): void {
    this.notificationService.markAsRead(id).subscribe({
      next: () => {
        this.notifications.update(list =>
          list.map(n => n.id === id ? { ...n, isRead: true } : n)
        );
      },
    });
  }

  notifLink(n: ClientNotificationDto): string {
    if (n.symptomReportId) return `/symptoms/${n.symptomReportId}`;
    if (n.appointmentId) return `/appointments`;
    if (n.invoiceId) return `/appointments`;
    return '/symptoms';
  }

  // ── Mechanic tasks ─────────────────────────────────────────────────────────

  loadTasks(): void {
    this.loading.set(true);
    this.repairTaskService.getMyTasks().subscribe({
      next: (items) => {
        this.tasks.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Erreur lors du chargement des tâches.');
        this.loading.set(false);
      },
    });
  }

  taskStatusColor(status: string): string {
    const map: Record<string, string> = {
      Assigned: 'bg-blue-100 text-blue-800',
      InProgress: 'bg-yellow-100 text-yellow-800',
      Fixed: 'bg-green-100 text-green-800',
      WorkReadyForTest: 'bg-purple-100 text-purple-800',
    };
    return map[status] ?? 'bg-gray-100 text-gray-700';
  }

  // ── Shared helpers ─────────────────────────────────────────────────────────

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('fr-FR', {
      year: 'numeric', month: 'long', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  aiPercent(score: number | null): string {
    return score != null ? `${Math.round(score * 100)}%` : '—';
  }
}

