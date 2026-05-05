import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { InterventionLifecycleService } from '../../core/services/workshop.service';
import { InterventionDetailDto, InterventionSummaryDto } from '../../core/models/workshop.models';

const STEPS = [
  { key: 'Created',              label: 'Ouvert',              icon: '📋' },
  { key: 'UnderExamination',     label: 'Examen en cours',     icon: '🔍' },
  { key: 'ExaminationReviewed',  label: 'Examen validé',       icon: '✅' },
  { key: 'InvoicePending',       label: 'Devis en attente',    icon: '💰' },
  { key: 'Approved',             label: 'Approuvé',            icon: '👍' },
  { key: 'RepairInProgress',     label: 'Réparation en cours', icon: '🔧' },
  { key: 'RepairCompleted',      label: 'Réparation terminée', icon: '🏁' },
  { key: 'ReadyForPickup',       label: 'Prêt à récupérer',   icon: '🚗' },
  { key: 'Closed',               label: 'Clôturé / Payé',      icon: '💳' },
];

const TERMINAL_REJECTED = 'Rejected';

@Component({
  selector: 'app-interventions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './interventions.html',
})
export class InterventionsComponent implements OnInit {
  private svc = inject(InterventionLifecycleService);

  interventions = signal<InterventionSummaryDto[]>([]);
  selected      = signal<InterventionDetailDto | null>(null);
  loading       = signal(false);
  detailLoading = signal(false);
  error         = signal<string | null>(null);

  steps = STEPS;

  ngOnInit(): void {
    this.loading.set(true);
    this.svc.getMyInterventions().subscribe({
      next: items => { this.interventions.set(items); this.loading.set(false); },
      error: ()    => { this.error.set('Erreur de chargement'); this.loading.set(false); },
    });
  }

  openDetail(id: string): void {
    this.detailLoading.set(true);
    this.selected.set(null);
    this.svc.getById(id).subscribe({
      next: d  => { this.selected.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); },
    });
  }

  closeDetail(): void { this.selected.set(null); }

  stepIndex(status: string): number {
    return STEPS.findIndex(s => s.key === status);
  }

  isRejected(status: string): boolean { return status === TERMINAL_REJECTED; }

  statusLabel(status: string): string {
    if (status === 'Rejected') return '❌ Refusé';
    return STEPS.find(s => s.key === status)?.label ?? status;
  }

  statusCss(status: string): string {
    const map: Record<string, string> = {
      Created:             'bg-gray-800 text-gray-400 border-gray-700',
      UnderExamination:    'bg-blue-900/30 text-blue-400 border-blue-800',
      ExaminationReviewed: 'bg-indigo-900/30 text-indigo-400 border-indigo-700',
      InvoicePending:      'bg-amber-900/30 text-amber-400 border-amber-800',
      Approved:            'bg-emerald-900/30 text-emerald-400 border-emerald-800',
      RepairInProgress:    'bg-violet-900/30 text-violet-400 border-violet-800',
      RepairCompleted:     'bg-cyan-900/30 text-cyan-400 border-cyan-800',
      ReadyForPickup:      'bg-teal-900/30 text-teal-400 border-teal-800',
      Closed:              'bg-blue-900/30 text-blue-400 border-blue-700',
      Rejected:            'bg-rose-900/30 text-rose-400 border-rose-800',
    };
    return map[status] ?? 'bg-gray-800 text-gray-400 border-gray-700';
  }

  parseParts(json: string | null): { name: string; quantity: number; estimatedPrice?: number }[] {
    if (!json) return [];
    try { return JSON.parse(json); } catch { return []; }
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('fr-FR', {
      year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }
}

