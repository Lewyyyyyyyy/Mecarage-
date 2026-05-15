import { Component, Input, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InterventionLifecycleService } from '../../core/services/workshop.service';
import { InterventionDetailDto, InterventionSummaryDto, RegisterPaymentDto } from '../../core/models/workshop.models';

const STEPS = [
  { key: 'Created',              label: 'Ouvert',              icon: '📋' },
  { key: 'UnderExamination',     label: 'Examen en cours',     icon: '🔍' },
  { key: 'ExaminationReviewed',  label: 'Examen validé',       icon: '✅' },
  { key: 'InvoicePending',       label: 'Devis en attente',    icon: '💰' },
  { key: 'RepairInProgress',     label: 'Réparation en cours', icon: '🔧' },
  { key: 'RepairCompleted',      label: 'Réparation terminée', icon: '🏁' },
  { key: 'ReadyForPickup',       label: 'Prêt à récupérer',   icon: '🚗' },
  { key: 'Closed',               label: 'Clôturé / Payé',      icon: '💳' },
];

@Component({
  selector: 'app-intervention-tracker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './intervention-tracker.html',
})
export class InterventionTrackerComponent implements OnInit {
  @Input() garageId!: string;

  private svc = inject(InterventionLifecycleService);

  interventions = signal<InterventionSummaryDto[]>([]);
  selected      = signal<InterventionDetailDto | null>(null);
  loading       = signal(false);
  detailLoading = signal(false);
  paymentLoading = signal(false);
  error         = signal<string | null>(null);
  paymentMsg    = signal<{ ok: boolean; text: string } | null>(null);
  steps = STEPS;

  // Payment form
  laborCost  = 0;
  payMethod  = 'Cash';
  showPaymentForm = false;

  get partsTotal(): number {
    const parts = this.parseParts(this.selected()?.partsUsedJson ?? null);
    return parts.reduce((sum, p) => sum + (p.estimatedPrice ?? 0) * p.quantity, 0);
  }

  get payTotal(): number {
    return this.partsTotal + (this.laborCost ?? 0);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.svc.getGarageInterventions(this.garageId).subscribe({
      next:  items => { this.interventions.set(items); this.loading.set(false); },
      error: ()    => { this.error.set('Erreur de chargement'); this.loading.set(false); },
    });
  }

  openDetail(id: string): void {
    this.detailLoading.set(true);
    this.selected.set(null);
    this.showPaymentForm = false;
    this.paymentMsg.set(null);
    this.svc.getById(id).subscribe({
      next: d  => {
        this.selected.set(d);
        this.laborCost = 0;
        this.detailLoading.set(false);
      },
      error: () => this.detailLoading.set(false),
    });
  }

  closeDetail(): void { this.selected.set(null); }

  registerPayment(): void {
    const d = this.selected();
    if (!d || this.payTotal <= 0) return;
    this.paymentLoading.set(true);
    const payload: RegisterPaymentDto = {
      paymentAmount: this.payTotal,
      paymentMethod: this.payMethod,
    };
    this.svc.registerPayment(d.id, payload).subscribe({
      next: () => {
        this.paymentMsg.set({ ok: true, text: 'Paiement enregistré. Intervention clôturée ✅' });
        this.paymentLoading.set(false);
        this.showPaymentForm = false;
        // Refresh list and detail
        this.load();
        this.svc.getById(d.id).subscribe({ next: nd => this.selected.set(nd) });
      },
      error: () => {
        this.paymentMsg.set({ ok: false, text: 'Erreur lors de l\'enregistrement du paiement' });
        this.paymentLoading.set(false);
      },
    });
  }

  stepIndex(status: string): number {
    return STEPS.findIndex(s => s.key === status);
  }

  isRejected(status: string): boolean { return status === 'Rejected'; }

  statusLabel(status: string): string {
    if (status === 'Rejected') return '❌ Refusé';
    if (status === 'Approved') return '👍 Approuvé';
    return STEPS.find(s => s.key === status)?.label ?? status;
  }

  statusCss(status: string): string {
    const map: Record<string, string> = {
      Created:             'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-700',
      UnderExamination:    'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 border-blue-300 dark:border-blue-800',
      ExaminationReviewed: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400 border-indigo-300 dark:border-indigo-700',
      InvoicePending:      'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-amber-300 dark:border-amber-800',
      Approved:            'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800',
      RepairInProgress:    'bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-400 border-violet-300 dark:border-violet-800',
      RepairCompleted:     'bg-cyan-100 dark:bg-cyan-900/30 text-cyan-700 dark:text-cyan-400 border-cyan-300 dark:border-cyan-800',
      ReadyForPickup:      'bg-teal-100 dark:bg-teal-900/30 text-teal-700 dark:text-teal-400 border-teal-300 dark:border-teal-800',
      Closed:              'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 border-blue-300 dark:border-blue-700',
      Rejected:            'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 border-rose-300 dark:border-rose-800',
    };
    return map[status] ?? 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-700';
  }

  parseParts(json: string | null): { name: string; quantity: number; estimatedPrice?: number }[] {
    if (!json) return [];
    try { return JSON.parse(json); } catch { return []; }
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('fr-FR', {
      year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }

  searchQuery = '';
  get filtered(): InterventionSummaryDto[] {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.interventions();
    return this.interventions().filter(i =>
      i.clientName.toLowerCase().includes(q) ||
      i.vehicleInfo.toLowerCase().includes(q) ||
      i.status.toLowerCase().includes(q)
    );
  }
}

