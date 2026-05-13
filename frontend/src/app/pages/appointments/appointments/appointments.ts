import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AppointmentService, InvoiceService, SymptomReportService } from '../../../core/services/workshop.service';
import { AppointmentDto, SymptomReportDto, InvoiceDto } from '../../../core/models/workshop.models';

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './appointments.html',
  styles: ``,
})
export class AppointmentsComponent implements OnInit {
  appointments = signal<AppointmentDto[]>([]);
  approvedReports = signal<SymptomReportDto[]>([]);
  myInvoices = signal<InvoiceDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  isSubmitting = signal(false);

  showBookingModal = signal(false);
  selectedReport: SymptomReportDto | null = null;
  bookingForm: FormGroup;

  activeTab = signal<'appointments' | 'book'>('appointments');
  selectedDayKey = signal<string | null>(null);

  constructor(
    private appointmentService: AppointmentService,
    private invoiceService: InvoiceService,
    private symptomService: SymptomReportService,
    private fb: FormBuilder,
    private route: ActivatedRoute
  ) {
    this.bookingForm = this.fb.group({
      vehicleId: ['', Validators.required],
      preferredDate: ['', Validators.required],
      preferredTime: ['09:00', Validators.required],
      specialRequests: [''],
    });
  }

  ngOnInit(): void {
    this.loadData();

    // Check if came from notifications or symptom-reports with a book intent
    this.route.queryParams.subscribe(params => {
      if (params['book'] || params['symptomReportId']) {
        this.activeTab.set('book');
      }
    });
  }

  readonly weekLabel = computed(() => {
    const today = new Date();
    const start = this.getWeekStart(today);
    const end = new Date(start);
    end.setDate(start.getDate() + 6);
    return `${start.toLocaleDateString('fr-FR')} - ${end.toLocaleDateString('fr-FR')}`;
  });

  readonly currentWeekDays = computed(() => {
    const start = this.getWeekStart(new Date());

    const days: Array<{
      date: Date;
      dayNumber: number;
      key: string;
      hasAppointments: boolean;
      count: number;
    }> = [];

    for (let i = 0; i < 7; i++) {
      const date = new Date(start);
      date.setDate(start.getDate() + i);
      const key = this.dateKey(date);
      const count = this.getAppointmentsByDay(key).length;
      days.push({
        date,
        dayNumber: date.getDate(),
        key,
        hasAppointments: count > 0,
        count,
      });
    }

    return days;
  });

  readonly selectedDayAppointments = computed(() => {
    const key = this.selectedDayKey();
    if (!key) return [];
    return this.getAppointmentsByDay(key).sort((a, b) => this.toDateTime(a).getTime() - this.toDateTime(b).getTime());
  });

  readonly nextAppointment = computed(() => {
    const now = new Date();
    return this.appointments()
      .filter(a => !['Cancelled', 'Declined', 'Completed'].includes(a.status))
      .map(a => ({ apt: a, at: this.toDateTime(a) }))
      .filter(item => item.at.getTime() >= now.getTime())
      .sort((a, b) => a.at.getTime() - b.at.getTime())[0]?.apt ?? null;
  });

  readonly nextAppointmentMessage = computed(() => {
    const next = this.nextAppointment();
    if (!next) return null;
    return `Prochain RDV: ${new Date(next.preferredDate).toLocaleDateString('fr-FR')} a ${this.formatTime(next.preferredTime)}`;
  });

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    // Load appointments
    this.appointmentService.getMyAppointments().subscribe({
      next: (apts) => {
        this.appointments.set(apts || []);
        this.bootstrapSelectedDate(apts || []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des rendez-vous');
        console.error(err);
        this.loading.set(false);
      },
    });

    // Load approved reports for booking
    this.symptomService.getMyReports().subscribe({
      next: (reports) => {
        this.approvedReports.set((reports || []).filter(r => r.status === 'Approved'));
      },
      error: () => {},
    });

    // Load my invoices
    this.invoiceService.getMyInvoices().subscribe({
      next: (invoices) => {
        this.myInvoices.set(invoices || []);
      },
      error: () => {},
    });
  }

  setActiveTab(tab: 'appointments' | 'book'): void {
    this.activeTab.set(tab);
  }

  selectDay(dayKey: string): void {
    this.selectedDayKey.set(dayKey);
  }

  isToday(day: Date): boolean {
    return this.dateKey(day) === this.dateKey(new Date());
  }

  isSelected(dayKey: string): boolean {
    return this.selectedDayKey() === dayKey;
  }

  formatTime(time: string): string {
    const [h = '00', m = '00'] = (time || '').split(':');
    return `${h.padStart(2, '0')}:${m.padStart(2, '0')}`;
  }

  formatCompactDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      weekday: 'short',
      day: '2-digit',
      month: 'short',
    });
  }

  hasBookedAppointmentForReport(reportId: string): boolean {
    return this.appointments().some(a => a.symptomReportId === reportId);
  }

  getReportBookingLabel(reportId: string): string {
    const appointment = this.appointments().find(a => a.symptomReportId === reportId);
    if (!appointment) return '';
    return `RDV deja cree (${this.getStatusBadge(appointment.status).label})`;
  }

  openBookingModal(report: SymptomReportDto): void {
    this.selectedReport = report;
    const minDate = this.getInputDate(report.availablePeriodStart);
    const maxDate = this.getInputDate(report.availablePeriodEnd);
    const today = this.toInputDate(new Date());

    let preferredDate = today;
    if (minDate && preferredDate < minDate) preferredDate = minDate;
    if (maxDate && preferredDate > maxDate) preferredDate = maxDate;

    this.bookingForm.patchValue({
      vehicleId: report.vehicleId,
      preferredDate,
    });
    this.showBookingModal.set(true);
  }

  getBookingMinDate(): string {
    return this.getInputDate(this.selectedReport?.availablePeriodStart ?? null);
  }

  getBookingMaxDate(): string {
    return this.getInputDate(this.selectedReport?.availablePeriodEnd ?? null);
  }

  submitBooking(): void {
    if (!this.bookingForm.valid || !this.selectedReport) return;

    const form = this.bookingForm.value;
    const timeParts = form.preferredTime.split(':');
    const formattedTime = `${timeParts[0].padStart(2,'0')}:${(timeParts[1] || '00').padStart(2,'0')}:00`;

    const data = {
      vehicleId: form.vehicleId,
      garageId: this.selectedReport.garageId!,
      preferredDate: new Date(form.preferredDate).toISOString(),
      preferredTime: formattedTime,
      symptomReportId: this.selectedReport.id,
      specialRequests: form.specialRequests || undefined,
    };

    this.isSubmitting.set(true);
    this.appointmentService.createAppointment(data).subscribe({
      next: () => {
        this.success.set('Rendez-vous demandé avec succès! Le chef d\'atelier confirmera votre demande.');
        this.showBookingModal.set(false);
        this.bookingForm.reset({ preferredTime: '09:00' });
        this.selectedReport = null;
        this.isSubmitting.set(false);
        this.loadData();
        setTimeout(() => this.success.set(null), 4000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la demande de rendez-vous');
        this.isSubmitting.set(false);
      },
    });
  }

  approveInvoice(invoiceId: string): void {
    this.invoiceService.approveInvoice(invoiceId).subscribe({
      next: () => {
        this.success.set('Devis approuvé ! Les réparations vont commencer.');
        this.loadData();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de l\'approbation');
      },
    });
  }

  rejectInvoice(invoiceId: string): void {
    if (!confirm('Êtes-vous sûr de vouloir refuser ce devis ?')) return;
    this.invoiceService.rejectInvoice(invoiceId).subscribe({
      next: () => {
        this.success.set('Devis refusé.');
        this.loadData();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors du refus');
      },
    });
  }

  getStatusBadge(status: string): { bg: string; text: string; label: string } {
    const map: Record<string, { bg: string; text: string; label: string }> = {
      Pending: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-300', label: 'En attente' },
      Approved: { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: 'Confirmé' },
      InProgress: { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-300', label: 'En cours' },
      Completed: { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: 'Terminé' },
      Declined: { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-800 dark:text-red-300', label: 'Refusé' },
      Cancelled: { bg: 'bg-gray-100 dark:bg-gray-900/30', text: 'text-gray-800 dark:text-gray-300', label: 'Annulé' },
    };
    return map[status] || map['Pending'];
  }


  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('fr-FR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  formatPeriod(start: string | null, end: string | null): string {
    if (!start || !end) return 'Période non spécifiée';
    return `du ${this.formatDate(start)} au ${this.formatDate(end)}`;
  }

  get pendingInvoices(): InvoiceDto[] {
    return this.myInvoices().filter(i => i.status === 'AwaitingApproval');
  }

  private dateKey(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private toDateTime(apt: AppointmentDto): Date {
    const base = new Date(apt.preferredDate);
    const [h = '00', m = '00'] = (apt.preferredTime || '').split(':');
    base.setHours(Number(h), Number(m), 0, 0);
    return base;
  }

  private getAppointmentsByDay(dayKey: string): AppointmentDto[] {
    return this.appointments().filter(a => this.dateKey(new Date(a.preferredDate)) === dayKey);
  }

  private bootstrapSelectedDate(apts: AppointmentDto[]): void {
    const next = apts
      .map(a => ({ apt: a, at: this.toDateTime(a) }))
      .filter(item => item.at.getTime() >= Date.now())
      .sort((a, b) => a.at.getTime() - b.at.getTime())[0]?.apt;

    if (next) {
      const date = new Date(next.preferredDate);
      this.selectedDayKey.set(this.dateKey(date));
      return;
    }

    const today = new Date();
    this.selectedDayKey.set(this.dateKey(today));
  }

  private getWeekStart(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = day === 0 ? -6 : 1 - day; // Monday
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private getInputDate(raw: string | null): string {
    if (!raw) return '';
    const parsed = new Date(raw);
    if (Number.isNaN(parsed.getTime())) {
      return raw.includes('T') ? raw.split('T')[0] : raw;
    }
    return this.toInputDate(parsed);
  }

  private toInputDate(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
