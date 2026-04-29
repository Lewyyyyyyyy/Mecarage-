import { Component, ElementRef, OnDestroy, OnInit, QueryList, ViewChildren, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../auth/auth.service';

type Feature = { title: string; desc: string; icon: string };
type Step = { title: string; desc: string };
type Testimonial = { name: string; role: string; text: string };

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.html',
})
export class HomeComponent implements OnInit, OnDestroy {
  authService = inject(AuthService);
  isAuthenticated = this.authService.isAuthenticated;
  user = this.authService.user;

  features: Feature[] = [
    { icon: '⚡', title: 'Fast booking', desc: 'Request a mechanic in minutes with a smooth, guided flow.' },
    { icon: '🧰', title: 'Verified mechanics', desc: 'Profiles, ratings, and verification to build trust.' },
    { icon: '💬', title: 'Live updates', desc: 'Status updates from request to completion.' },
    { icon: '🧾', title: 'Transparent pricing', desc: 'Clear quotes and service details before you confirm.' },
    { icon: '📍', title: 'Location-based', desc: 'Find help near you with map-ready matching.' },
    { icon: '🔒', title: 'Secure accounts', desc: 'Role-based access (Client / Mechanic / Admin).' },
  ];

  steps: Step[] = [
    { title: 'Describe the issue', desc: 'Tell us what’s wrong—battery, flat tire, engine, etc.' },
    { title: 'Get matched', desc: 'We connect you with available mechanics nearby.' },
    { title: 'Track & pay', desc: 'Follow progress, confirm the job, and pay securely.' },
  ];

  testimonials: Testimonial[] = [
    { name: 'Sarra', role: 'Client', text: 'Booked help in 5 minutes. The mechanic arrived quickly and explained everything.' },
    { name: 'Yassine', role: 'Mechanic', text: 'I get organized requests and can manage my schedule easily.' },
    { name: 'Amal', role: 'Client', text: 'Transparent pricing and great communication. Super smooth experience.' },
  ];

  // Scroll reveal
  @ViewChildren('reveal') revealEls!: QueryList<ElementRef<HTMLElement>>;
  private observer?: IntersectionObserver;

  prefersReducedMotion = signal(false);

  ngOnInit() {
    this.prefersReducedMotion.set(
      typeof window !== 'undefined' &&
      window.matchMedia &&
      window.matchMedia('(prefers-reduced-motion: reduce)').matches
    );

    if (this.prefersReducedMotion()) return;

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

  ngAfterViewInit() {
    if (this.prefersReducedMotion()) return;
    queueMicrotask(() => {
      this.revealEls?.forEach(el => this.observer?.observe(el.nativeElement));
    });
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }
}
