import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TenantsService, TenantDto, CreateTenantRequest } from '../../core/services/tenants.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-tenants-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './tenants-list.html',
  styleUrls: ['./tenants-list.css'],
})
export class TenantsListComponent implements OnInit {
  tenants = signal<TenantDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  searchQuery = signal('');

  showCreateModal = signal(false);
  createForm!: FormGroup;
  isSubmitting = signal(false);

  constructor(
    private tenantsService: TenantsService,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.createForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      slug: ['', [Validators.required, Validators.minLength(3), Validators.pattern(/^[a-z0-9-]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[\d\s\-\+\(\)]{8,}$/)]],
    });
  }

  ngOnInit() {
    this.loadTenants();
  }

  loadTenants() {
    this.loading.set(true);
    this.tenantsService.getAll().subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Error loading tenants');
        console.error(err);
        this.loading.set(false);
      },
    });
  }

  manageTenant(tenantId: string) {
    this.router.navigate(['/manage-tenant', tenantId]);
  }

  openCreateModal() {
    this.showCreateModal.set(true);
    this.createForm.reset();
  }

  closeCreateModal() {
    this.showCreateModal.set(false);
  }

  createTenant() {
    if (!this.createForm.valid) {
      return;
    }

    this.isSubmitting.set(true);
    const request: CreateTenantRequest = {
      name: this.createForm.get('name')?.value?.trim() || '',
      slug: this.createForm.get('slug')?.value?.trim() || '',
      email: this.createForm.get('email')?.value?.trim() || '',
      phone: this.createForm.get('phone')?.value?.trim() || '',
    };

    this.tenantsService.createTenant(request)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.success.set('Tenant created successfully');
          this.loadTenants();
          this.closeCreateModal();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Error creating tenant');
          console.error(err);
        },
      });
  }

  get filteredTenants() {
    const query = this.searchQuery().toLowerCase();
    return this.tenants().filter(
      (t) =>
        t.name.toLowerCase().includes(query) ||
        t.slug.toLowerCase().includes(query) ||
        t.email.toLowerCase().includes(query)
    );
  }

  onSearchChange(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchQuery.set(target.value);
  }
}

