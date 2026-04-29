import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SymptomReportService } from '../../core/services/workshop.service';
import { SymptomReportDto } from '../../core/models/workshop.models';

@Component({
  selector: 'app-my-symptom-reports',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-symptom-reports.html',
  styleUrls: ['./my-symptom-reports.css'],
})
export class MySymptomReportsComponent implements OnInit {
  reports = signal<SymptomReportDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  constructor(private readonly symptomService: SymptomReportService) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.loading.set(true);
    this.symptomService.getMyReports().subscribe({
      next: (reports) => {
        this.reports.set(reports || []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement des rapports');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  getStatusBadge(status: string): { bg: string; text: string; label: string } {
    const statusMap: Record<string, { bg: string; text: string; label: string }> = {
      Submitted: { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-300', label: 'Soumis' },
      PendingReview: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-300', label: 'En attente' },
      Reviewed: { bg: 'bg-purple-100 dark:bg-purple-900/30', text: 'text-purple-800 dark:text-purple-300', label: 'Examiné' },
      Approved: { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-300', label: 'Approuvé' },
      Declined: { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-800 dark:text-red-300', label: 'Rejeté' },
    };
    return statusMap[status] || statusMap['Submitted'];
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
}

