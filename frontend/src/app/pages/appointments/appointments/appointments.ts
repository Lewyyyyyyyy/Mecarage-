import { Component, OnInit, signal } from '@angular/core';
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

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    // Load appointments
    this.appointmentService.getMyAppointments().subscribe({
      next: (apts) => {
        this.appointments.set(apts || []);
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

  openBookingModal(report: SymptomReportDto): void {
    this.selectedReport = report;
    this.bookingForm.patchValue({ vehicleId: report.vehicleId });
    this.showBookingModal.set(true);
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
}
