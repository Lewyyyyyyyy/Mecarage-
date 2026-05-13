import { Component, ElementRef, OnDestroy, OnInit, QueryList, ViewChildren, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { AdminService } from '../../core/services/admin.service';
import { PublicTrustKpis } from '../../core/models/admin.models';
import { InterventionLifecycleService, SymptomReportService } from '../../core/services/workshop.service';
import { InterventionSummaryDto, SymptomReportDto } from '../../core/models/workshop.models';
import { catchError, forkJoin, of } from 'rxjs';

type Feature = { title: string; desc: string; icon: string };
type Step = { title: string; desc: string };
type Testimonial = { name: string; role: string; text: string };

type Slide = { src: string; alt: string; caption: string };
type LiveRequestCard = {
  issue: string;
  eta: string;
  mechanic: string;
  route: string;
  statusLabel: string;
};
type AppRole = 'Client' | 'Mecanicien' | 'ChefAtelier' | 'AdminEntreprise' | 'SuperAdmin' | 'Guest';
type QuickAction = {
  label: string;
  route: string | any[];
  tone: 'primary' | 'secondary';
};
type RoleExperience = {
  role: AppRole;
  badge: string;
  title: string;
  subtitle: string;
  actions: QuickAction[];
};

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.html',
})
export class HomeComponent implements OnInit, OnDestroy {
  authService = inject(AuthService);
  adminService = inject(AdminService);
  symptomService = inject(SymptomReportService);
  interventionService = inject(InterventionLifecycleService);
  isAuthenticated = this.authService.isAuthenticated;
  user = this.authService.user;

  trustKpis = signal<PublicTrustKpis | null>(null);
  trustLoading = signal(false);
  trustError = signal<string | null>(null);

  completionRate = computed(() => this.trustKpis()?.successRate ?? 0);
  liveRequest = signal<LiveRequestCard | null>(null);
  liveRequestLoading = signal(false);
  showLiveRequestCard = computed(() => {
    return this.isAuthenticated() && this.user()?.role === 'Client' && !!this.liveRequest();
  });

  readonly roleExperiences: RoleExperience[] = [
    {
      role: 'Client',
      badge: 'Client',
      title: 'Planifiez vos interventions en quelques clics',
      subtitle: 'Déclarez vos symptômes, suivez vos interventions et consultez votre historique véhicule.',
      actions: [
        { label: 'Prendre un rendez-vous', route: '/appointments', tone: 'primary' },
        { label: 'Mes interventions', route: '/interventions', tone: 'secondary' },
      ],
    },
    {
      role: 'Mecanicien',
      badge: 'Mécanicien',
      title: 'Restez focus sur vos tâches atelier',
      subtitle: 'Consultez vos missions, mettez à jour les statuts et collaborez avec le chef d\'atelier.',
      actions: [
        { label: 'Mes tâches', route: '/mechanic/tasks', tone: 'primary' },
        { label: 'Messagerie', route: '/inbox', tone: 'secondary' },
      ],
    },
    {
      role: 'ChefAtelier',
      badge: 'Chef d\'atelier',
      title: 'Pilotez l\'exécution des interventions',
      subtitle: 'Validez les examens, supervisez les réparations et gardez une vue complète sur le garage.',
      actions: [
        { label: 'Dashboard Garage', route: '/admin', tone: 'primary' },
        { label: 'Revue examens', route: '/admin', tone: 'secondary' },
      ],
    },
    {
      role: 'AdminEntreprise',
      badge: 'Admin Garage',
      title: 'Administrez votre activité garage',
      subtitle: 'Suivez clients, stock, interventions et performance de votre entreprise depuis un seul espace.',
      actions: [
        { label: 'Dashboard Garage', route: '/admin', tone: 'primary' },
        { label: 'Stock & inventaire', route: '/inventory', tone: 'secondary' },
      ],
    },
    {
      role: 'SuperAdmin',
      badge: 'Super Admin',
      title: 'Supervisez la plateforme multi-tenant',
      subtitle: 'Contrôlez les tenants, la gouvernance globale et la santé opérationnelle du système.',
      actions: [
        { label: 'Super Admin Dashboard', route: '/super-admin', tone: 'primary' },
        { label: 'Tenants', route: '/tenants', tone: 'secondary' },
      ],
    },
  ];

  currentRole = computed<AppRole>(() => {
    const rawRole = this.user()?.role;
    const allowedRoles: AppRole[] = ['Client', 'Mecanicien', 'ChefAtelier', 'AdminEntreprise', 'SuperAdmin'];
    return allowedRoles.includes(rawRole as AppRole) ? (rawRole as AppRole) : 'Guest';
  });

  activeExperience = computed<RoleExperience>(() => {
    const role = this.currentRole();
    if (role === 'Guest') {
      return {
        role: 'Guest',
        badge: 'Visiteur',
        title: 'Une plateforme unique pour clients, mécaniciens et gestionnaires',
        subtitle: 'Créez votre compte pour réserver, suivre et gérer les interventions en temps réel.',
        actions: [
          { label: 'Créer un compte', route: '/signup', tone: 'primary' },
          { label: 'Se connecter', route: '/login', tone: 'secondary' },
        ],
      };
    }

    const experience = this.roleExperiences.find(exp => exp.role === role) ?? this.roleExperiences[0];

    if (role === 'ChefAtelier') {
      return {
        ...experience,
        actions: [
          { label: 'Dashboard Garage', route: this.garageDashboardRoute(), tone: 'primary' },
          { label: 'Revue examens', route: this.garageExamRoute(), tone: 'secondary' },
        ],
      };
    }

    if (role === 'AdminEntreprise') {
      return {
        ...experience,
        actions: [
          { label: 'Dashboard Garage', route: this.garageDashboardRoute(), tone: 'primary' },
          { label: 'Stock & inventaire', route: '/inventory', tone: 'secondary' },
        ],
      };
    }

    return experience;
  });

  garageDashboardRoute() {
    const garageId = this.user()?.garageId;
    return garageId ? ['/garage-admin', garageId] : '/admin';
  }

  garageExamRoute() {
    const garageId = this.user()?.garageId;
    return garageId ? ['/garage-admin', garageId, 'examination-reviews'] : '/admin';
  }

  // ── Carousel ──────────────────────────────────────────────
  slides: Slide[] = [
    { src: '/images/pexels-cottonbro-4481954.jpg',               alt: 'Mechanic at work',        caption: 'Expert mechanics ready for you' },
    { src: '/images/pexels-cottonbro-4489704.jpg',               alt: 'Garage service',          caption: 'Full garage service experience' },
    { src: '/images/pexels-drmkhawarnazir-8815796.jpg',          alt: 'Car diagnostics',         caption: 'Precision diagnostics, fast results' },
    { src: '/images/pexels-esmihel-36281956.jpg',                alt: 'Modern workshop',         caption: 'Modern workshops across the city' },
    { src: '/images/pexels-gratisography-474.jpg',               alt: 'Engine repair',           caption: 'From oil changes to engine overhauls' },
    { src: '/images/pexels-lumierestudiomx-4116205.jpg',         alt: 'Tire service',            caption: 'Quick tire & wheel service' },
    { src: '/images/pexels-renee-razumov-2155050841-33814734.jpg', alt: 'Road assistance',       caption: '24/7 roadside assistance' },
  ];

  activeSlide = signal(0);
  private carouselTimer?: ReturnType<typeof setInterval>;

  goTo(index: number) {
    this.activeSlide.set((index + this.slides.length) % this.slides.length);
    this.resetTimer();
  }
  prev() { this.goTo(this.activeSlide() - 1); }
  next() { this.goTo(this.activeSlide() + 1); }

  private startTimer() {
    this.carouselTimer = setInterval(() => {
      this.activeSlide.update(i => (i + 1) % this.slides.length);
    }, 4500);
  }
  private resetTimer() {
    clearInterval(this.carouselTimer);
    this.startTimer();
  }

  // ── existing data ──────────────────────────────────────────
  features: Feature[] = [
    { icon: '⚡', title: 'Réservation ultra-rapide', desc: 'Créez une demande en moins d’une minute avec un parcours guidé et simple.' },
    { icon: '🧰', title: 'Mécaniciens vérifiés', desc: 'Profils qualifiés, historique d’interventions et notation pour renforcer la confiance.' },
    { icon: '💬', title: 'Suivi en temps réel', desc: 'Recevez les statuts clés de votre intervention du diagnostic jusqu’à la clôture.' },
    { icon: '🧾', title: 'Tarification transparente', desc: 'Devis détaillé et validation client avant tout engagement de réparation.' },
    { icon: '📍', title: 'Ateliers proches de vous', desc: 'Affectation intelligente vers les garages disponibles les plus pertinents.' },
    { icon: '🔒', title: 'Comptes sécurisés', desc: 'Accès protégé et permissions par rôle (Client, Mécanicien, Chef, Admin).' },
  ];

  steps: Step[] = [
    { title: 'Expliquez votre besoin', desc: 'Choisissez votre véhicule, décrivez la panne et ajoutez un symptôme clair en moins d’une minute.' },
    { title: 'Validation atelier rapide', desc: 'Le garage confirme la prise en charge, propose un créneau, puis lance le diagnostic.' },
    { title: 'Suivi, accord et clôture', desc: 'Vous suivez chaque statut, validez le devis, puis récupérez votre véhicule avec paiement sécurisé.' },
  ];

  testimonials: Testimonial[] = [
    { name: 'Sarra',   role: 'Client',    text: 'Booked help in 5 minutes. The mechanic arrived quickly and explained everything.' },
    { name: 'Yassine', role: 'Mechanic',  text: 'I get organized requests and can manage my schedule easily.' },
    { name: 'Amal',    role: 'Client',    text: 'Transparent pricing and great communication. Super smooth experience.' },
  ];

  // ── Scroll reveal ──────────────────────────────────────────
  @ViewChildren('reveal') revealEls!: QueryList<ElementRef<HTMLElement>>;
  private observer?: IntersectionObserver;
  prefersReducedMotion = signal(false);

  ngOnInit() {
    this.prefersReducedMotion.set(
      typeof window !== 'undefined' &&
      window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
    );
    if (!this.prefersReducedMotion()) {
      this.observer = new IntersectionObserver(
        entries => {
          for (const entry of entries) {
            if (entry.isIntersecting) {
              entry.target.classList.add('reveal-in');
              this.observer?.unobserve(entry.target);
            }
          }
        },
        { threshold: 0.12 }
      );
    }
    this.loadTrustKpis();
    this.loadClientLiveRequest();
    this.startTimer();
  }

  ngAfterViewInit() {
    if (this.prefersReducedMotion()) return;
    queueMicrotask(() => {
      this.revealEls?.forEach(el => this.observer?.observe(el.nativeElement));
    });
  }

  ngOnDestroy() {
    this.observer?.disconnect();
    clearInterval(this.carouselTimer);
  }

  private loadTrustKpis() {
    this.trustLoading.set(true);
    this.trustError.set(null);

    this.adminService.getPublicTrustKpis().subscribe({
      next: (kpis) => {
        this.trustKpis.set(kpis);
        this.trustLoading.set(false);
      },
      error: () => {
        this.trustError.set('Impossible de charger les indicateurs en temps reel.');
        this.trustLoading.set(false);
      }
    });
  }

  private loadClientLiveRequest() {
    if (!(this.isAuthenticated() && this.user()?.role === 'Client')) {
      this.liveRequest.set(null);
      return;
    }

    this.liveRequestLoading.set(true);

    forkJoin({
      interventions: this.interventionService.getMyInterventions().pipe(catchError(() => of([] as InterventionSummaryDto[]))),
      symptoms: this.symptomService.getMyReports().pipe(catchError(() => of([] as SymptomReportDto[])))
    }).subscribe(({ interventions, symptoms }) => {
      const openIntervention = this.pickLatestOpenIntervention(interventions);
      if (openIntervention) {
        this.liveRequest.set({
          issue: openIntervention.vehicleInfo,
          eta: this.interventionEta(openIntervention.status),
          mechanic: `${openIntervention.garageName}`,
          route: '/interventions',
          statusLabel: this.interventionStatusLabel(openIntervention.status),
        });
        this.liveRequestLoading.set(false);
        return;
      }

      const openSymptom = this.pickLatestOpenSymptom(symptoms);
      if (openSymptom) {
        this.liveRequest.set({
          issue: this.toShortText(openSymptom.symptomsDescription, 72),
          eta: this.symptomEta(openSymptom),
          mechanic: openSymptom.aiPredictedIssue ? `IA: ${openSymptom.aiPredictedIssue}` : 'En attente de revue atelier',
          route: '/symptoms',
          statusLabel: `Symptome: ${openSymptom.status}`,
        });
      } else {
        this.liveRequest.set(null);
      }

      this.liveRequestLoading.set(false);
    });
  }

  private pickLatestOpenIntervention(items: InterventionSummaryDto[]) {
    return [...items]
      .filter(i => this.isInterventionOpen(i.status))
      .sort((a, b) => new Date(b.updatedAt ?? b.createdAt).getTime() - new Date(a.updatedAt ?? a.createdAt).getTime())[0];
  }

  private pickLatestOpenSymptom(items: SymptomReportDto[]) {
    return [...items]
      .filter(r => this.isSymptomOpen(r.status))
      .sort((a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime())[0];
  }

  private isInterventionOpen(status: string) {
    return status !== 'Closed' && status !== 'Rejected';
  }

  private isSymptomOpen(status: string) {
    return status !== 'Declined';
  }

  private interventionEta(status: string) {
    const map: Record<string, string> = {
      Created: 'Prise en charge en cours',
      UnderExamination: 'Diagnostic en cours',
      ExaminationReviewed: 'Validation atelier effectuee',
      InvoicePending: 'En attente de votre validation devis',
      Approved: 'Demarrage reparation imminent',
      RepairInProgress: 'Reparation en cours',
      RepairCompleted: 'Controle final en cours',
      ReadyForPickup: 'Vehicule pret a recuperer',
    };
    return map[status] ?? 'Mise a jour en cours';
  }

  private interventionStatusLabel(status: string) {
    const map: Record<string, string> = {
      Created: 'Intervention ouverte',
      UnderExamination: 'Examen en cours',
      ExaminationReviewed: 'Examen valide',
      InvoicePending: 'Devis en attente',
      Approved: 'Intervention approuvee',
      RepairInProgress: 'Reparation en cours',
      RepairCompleted: 'Reparation terminee',
      ReadyForPickup: 'Pret pour recuperation',
    };
    return map[status] ?? status;
  }

  private symptomEta(report: SymptomReportDto) {
    if (report.availablePeriodStart && report.availablePeriodEnd) {
      return `Disponibilite: ${new Date(report.availablePeriodStart).toLocaleDateString('fr-FR')} - ${new Date(report.availablePeriodEnd).toLocaleDateString('fr-FR')}`;
    }
    return 'En attente de retour atelier';
  }

  private toShortText(value: string, maxLength: number) {
    if (value.length <= maxLength) return value;
    return `${value.slice(0, maxLength).trim()}...`;
  }

  formatCompact(value: number | undefined) {
    return new Intl.NumberFormat('fr-FR').format(value ?? 0);
  }
}
