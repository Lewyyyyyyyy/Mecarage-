import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import {
  InvoiceService,
  AppointmentService,
  SymptomReportService,
} from '../../core/services/workshop.service';
import { InvoiceDto, AppointmentDto, SymptomReportDto } from '../../core/models/workshop.models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './history.html',
})
export class HistoryComponent implements OnInit {
  private invoiceService = inject(InvoiceService);
  private appointmentService = inject(AppointmentService);
  private symptomService = inject(SymptomReportService);
  private route = inject(ActivatedRoute);

  tab = signal<'invoices' | 'appointments' | 'reports'>('invoices');

  invoices = signal<InvoiceDto[]>([]);
  appointments = signal<AppointmentDto[]>([]);
  reports = signal<SymptomReportDto[]>([]);

  loading = signal(false);
  error = signal<string | null>(null);
  invoiceActionId = signal<string | null>(null);
  success = signal<string | null>(null);

  ngOnInit(): void {
    // Read ?tab= query param to select the active tab
    this.route.queryParams.subscribe(params => {
      const t = params['tab'];
      if (t === 'appointments' || t === 'reports' || t === 'invoices') {
        this.tab.set(t);
      }
    });

    this.loading.set(true);
    let done = 0;
    const tick = () => { if (++done === 3) this.loading.set(false); };

    this.invoiceService.getMyInvoices().subscribe({
      next: items => {
        this.invoices.set(
          items.sort((a, b) => {
            if (a.status === 'AwaitingApproval' && b.status !== 'AwaitingApproval') return -1;
            if (b.status === 'AwaitingApproval' && a.status !== 'AwaitingApproval') return 1;
            return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
          })
        );
        tick();
      },
      error: () => tick(),
    });

    this.appointmentService.getMyAppointments().subscribe({
      next: items => {
        this.appointments.set(items.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
        tick();
      },
      error: () => tick(),
    });

    this.symptomService.getMyReports().subscribe({
      next: items => {
        this.reports.set(items.sort((a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime()));
        tick();
      },
      error: () => tick(),
    });
  }

  approveInvoice(invoiceId: string): void {
    this.invoiceActionId.set(invoiceId);
    this.invoiceService.approveInvoice(invoiceId).subscribe({
      next: () => {
        this.success.set('✅ Devis accepté ! Les réparations vont commencer.');
        this.invoiceActionId.set(null);
        this.invoiceService.getMyInvoices().subscribe({ next: i => this.invoices.set(i) });
        setTimeout(() => this.success.set(null), 4000);
      },
      error: err => { this.error.set(err.error?.message || 'Erreur'); this.invoiceActionId.set(null); },
    });
  }

  rejectInvoice(invoiceId: string): void {
    if (!confirm('Refuser ce devis ?')) return;
    this.invoiceActionId.set(invoiceId);
    this.invoiceService.rejectInvoice(invoiceId).subscribe({
      next: () => {
        this.success.set('Devis refusé.');
        this.invoiceActionId.set(null);
        this.invoiceService.getMyInvoices().subscribe({ next: i => this.invoices.set(i) });
        setTimeout(() => this.success.set(null), 3000);
      },
      error: err => { this.error.set(err.error?.message || 'Erreur'); this.invoiceActionId.set(null); },
    });
  }

  invoiceBadge(status: string): { label: string; css: string } {
    const map: Record<string, { label: string; css: string }> = {
      Draft:            { label: 'Brouillon',      css: 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-700' },
      AwaitingApproval: { label: '⏳ En attente',  css: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-amber-300 dark:border-amber-800' },
      Approved:         { label: '✅ Accepté',     css: 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' },
      Rejected:         { label: '❌ Refusé',      css: 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 border-rose-300 dark:border-rose-800' },
      Paid:             { label: '💳 Payé',        css: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 border-blue-300 dark:border-blue-800' },
    };
    return map[status] ?? { label: status, css: 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-700' };
  }

  appointmentBadge(status: string): { label: string; css: string } {
    const map: Record<string, { label: string; css: string }> = {
      Pending:    { label: '⏳ En attente',  css: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-amber-300 dark:border-amber-800' },
      Approved:   { label: '✅ Confirmé',   css: 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' },
      Declined:   { label: '❌ Refusé',     css: 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 border-rose-300 dark:border-rose-800' },
      Completed:  { label: '🏁 Terminé',   css: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 border-blue-300 dark:border-blue-800' },
      Cancelled:  { label: 'Annulé',        css: 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-700' },
    };
    return map[status] ?? { label: status, css: 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-700' };
  }

  reportBadge(status: string): { label: string; css: string } {
    const map: Record<string, { label: string; css: string }> = {
      Submitted:    { label: 'Soumis',       css: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-amber-300 dark:border-amber-800' },
      PendingReview:{ label: 'En attente',   css: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-amber-300 dark:border-amber-800' },
      Approved:     { label: '✅ Approuvé', css: 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' },
      Reviewed:     { label: '📋 Examiné', css: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 border-blue-300 dark:border-blue-800' },
      Declined:     { label: '❌ Refusé',  css: 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 border-rose-300 dark:border-rose-800' },
    };
    return map[status] ?? { label: status, css: 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-700' };
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('fr-FR', {
      year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }
}

