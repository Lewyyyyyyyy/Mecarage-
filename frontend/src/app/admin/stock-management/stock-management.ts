import { Component, OnInit, signal, computed, input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { SparePartsService } from '../../core/services/workshop.service';
import { SparePartDto } from '../../core/models/workshop.models';

export const PART_CATEGORIES = [
  'Moteur', 'Transmission', 'Freinage', 'Suspension', 'Direction',
  'Électrique', 'Carrosserie', 'Climatisation', 'Échappement', 'Autre',
];

@Component({
  selector: 'app-stock-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './stock-management.html',
})
export class StockManagementComponent implements OnInit {
  garageId = input.required<string>();
  /** When true, hides all add/edit/restock/delete controls (read-only view for ChefAtelier) */
  isReadOnly = input<boolean>(false);

  private sparePartsService = inject(SparePartsService);
  private fb = inject(FormBuilder);

  // ── State ──────────────────────────────────────────────────────────────────
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  parts = signal<SparePartDto[]>([]);

  searchQuery = signal('');
  selectedCategory = signal('');

  // Modals
  showAddModal = signal(false);
  showEditModal = signal(false);
  showRestockModal = signal(false);
  selectedPart = signal<SparePartDto | null>(null);
  isSubmitting = signal(false);

  // Forms
  partForm: FormGroup;
  restockForm: FormGroup;

  categories = PART_CATEGORIES;

  // ── Computed ───────────────────────────────────────────────────────────────
  filteredParts = computed(() => {
    let list = this.parts();
    const q = this.searchQuery().trim().toLowerCase();
    const cat = this.selectedCategory();
    if (q) list = list.filter(p =>
      p.name.toLowerCase().includes(q) ||
      p.code.toLowerCase().includes(q) ||
      (p.partNumber?.toLowerCase().includes(q) ?? false)
    );
    if (cat) list = list.filter(p => p.category === cat);
    return list;
  });

  lowStockCount = computed(() => this.parts().filter(p => p.isLowStock && p.isActive).length);

  stats = computed(() => ({
    total: this.parts().length,
    active: this.parts().filter(p => p.isActive).length,
    lowStock: this.lowStockCount(),
    totalValue: this.parts().reduce((sum, p) => sum + p.unitPrice * p.stockQuantity, 0),
  }));

  constructor() {
    this.partForm = this.fb.group({
      code: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', Validators.required],
      category: ['', Validators.required],
      unitPrice: [0, [Validators.required, Validators.min(0.01)]],
      stockQuantity: [0, [Validators.required, Validators.min(0)]],
      reorderLevel: [5, [Validators.required, Validators.min(1)]],
      manufacturer: [''],
      partNumber: [''],
      isActive: [true],
    });

    this.restockForm = this.fb.group({
      quantityToAdd: [1, [Validators.required, Validators.min(1)]],
    });
  }

  ngOnInit(): void {
    this.loadStock();
  }

  loadStock(): void {
    this.loading.set(true);
    this.error.set(null);
    this.sparePartsService.getStock(this.garageId()).subscribe({
      next: (parts) => { this.parts.set(parts); this.loading.set(false); },
      error: () => { this.error.set('Erreur lors du chargement du stock.'); this.loading.set(false); },
    });
  }

  // ── Add part ───────────────────────────────────────────────────────────────
  openAddModal(): void {
    this.partForm.reset({ stockQuantity: 0, reorderLevel: 5, unitPrice: 0, isActive: true });
    this.showAddModal.set(true);
    this.error.set(null);
  }

  submitAdd(): void {
    if (!this.partForm.valid) { this.partForm.markAllAsTouched(); return; }
    this.isSubmitting.set(true);
    const v = this.partForm.value;
    this.sparePartsService.createPart(this.garageId(), {
      code: v.code, name: v.name, description: v.description, category: v.category,
      unitPrice: +v.unitPrice, stockQuantity: +v.stockQuantity, reorderLevel: +v.reorderLevel,
      manufacturer: v.manufacturer || null, partNumber: v.partNumber || null,
    }).subscribe({
      next: () => {
        this.flashSuccess('Pièce ajoutée avec succès.');
        this.showAddModal.set(false);
        this.isSubmitting.set(false);
        this.loadStock();
      },
      error: (err) => {
        this.error.set(err.error?.message || "Erreur lors de l'ajout.");
        this.isSubmitting.set(false);
      },
    });
  }

  // ── Edit part ──────────────────────────────────────────────────────────────
  openEditModal(part: SparePartDto): void {
    this.selectedPart.set(part);
    this.partForm.patchValue({
      code: part.code, name: part.name, description: part.description,
      category: part.category, unitPrice: part.unitPrice,
      stockQuantity: part.stockQuantity, reorderLevel: part.reorderLevel,
      manufacturer: part.manufacturer ?? '', partNumber: part.partNumber ?? '',
      isActive: part.isActive,
    });
    this.showEditModal.set(true);
    this.error.set(null);
  }

  submitEdit(): void {
    if (!this.partForm.valid || !this.selectedPart()) { this.partForm.markAllAsTouched(); return; }
    this.isSubmitting.set(true);
    const v = this.partForm.value;
    this.sparePartsService.updatePart(this.garageId(), this.selectedPart()!.id, {
      name: v.name, description: v.description, category: v.category,
      unitPrice: +v.unitPrice, reorderLevel: +v.reorderLevel,
      manufacturer: v.manufacturer || null, partNumber: v.partNumber || null,
      isActive: v.isActive,
    }).subscribe({
      next: () => {
        this.flashSuccess('Pièce mise à jour.');
        this.showEditModal.set(false);
        this.selectedPart.set(null);
        this.isSubmitting.set(false);
        this.loadStock();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors de la mise à jour.');
        this.isSubmitting.set(false);
      },
    });
  }

  // ── Restock ────────────────────────────────────────────────────────────────
  openRestockModal(part: SparePartDto): void {
    this.selectedPart.set(part);
    this.restockForm.reset({ quantityToAdd: 1 });
    this.showRestockModal.set(true);
    this.error.set(null);
  }

  submitRestock(): void {
    if (!this.restockForm.valid || !this.selectedPart()) return;
    this.isSubmitting.set(true);
    const qty = +this.restockForm.value.quantityToAdd;
    this.sparePartsService.restock(this.garageId(), this.selectedPart()!.id, qty).subscribe({
      next: (res) => {
        this.flashSuccess(res.message ?? `Stock mis à jour.`);
        this.showRestockModal.set(false);
        this.selectedPart.set(null);
        this.isSubmitting.set(false);
        this.loadStock();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Erreur lors du réapprovisionnement.');
        this.isSubmitting.set(false);
      },
    });
  }

  // ── Delete ─────────────────────────────────────────────────────────────────
  deletePart(part: SparePartDto): void {
    if (!confirm(`Supprimer "${part.name}" ? Cette action est irréversible.`)) return;
    this.sparePartsService.deletePart(this.garageId(), part.id).subscribe({
      next: () => { this.flashSuccess('Pièce supprimée.'); this.loadStock(); },
      error: (err) => this.error.set(err.error?.message || 'Erreur lors de la suppression.'),
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  closeModals(): void {
    this.showAddModal.set(false);
    this.showEditModal.set(false);
    this.showRestockModal.set(false);
    this.selectedPart.set(null);
    this.error.set(null);
  }

  private flashSuccess(msg: string): void {
    this.success.set(msg);
    setTimeout(() => this.success.set(null), 3000);
  }

  stockColor(part: SparePartDto): string {
    if (!part.isActive) return 'text-gray-500';
    if (part.stockQuantity === 0) return 'text-rose-400 font-black';
    if (part.isLowStock) return 'text-amber-400 font-bold';
    return 'text-emerald-400 font-bold';
  }

  categoryColor(cat: string): string {
    const map: Record<string, string> = {
      'Moteur': 'bg-red-900/30 text-red-400 border-red-800',
      'Transmission': 'bg-orange-900/30 text-orange-400 border-orange-800',
      'Freinage': 'bg-yellow-900/30 text-yellow-400 border-yellow-800',
      'Suspension': 'bg-lime-900/30 text-lime-400 border-lime-800',
      'Direction': 'bg-green-900/30 text-green-400 border-green-800',
      'Électrique': 'bg-blue-900/30 text-blue-400 border-blue-800',
      'Carrosserie': 'bg-indigo-900/30 text-indigo-400 border-indigo-800',
      'Climatisation': 'bg-cyan-900/30 text-cyan-400 border-cyan-800',
      'Échappement': 'bg-purple-900/30 text-purple-400 border-purple-800',
    };
    return map[cat] ?? 'bg-gray-800 text-gray-400 border-gray-700';
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('fr-TN', { style: 'currency', currency: 'TND', minimumFractionDigits: 2 }).format(val);
  }
}

