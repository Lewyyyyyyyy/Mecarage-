import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../auth/auth.service';
import { StockManagementComponent } from '../../../admin/stock-management/stock-management';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, StockManagementComponent],
  templateUrl: './inventory.html',
})
export class InventoryComponent {
  private auth = inject(AuthService);

  garageId = computed(() => this.auth.user()?.garageId ?? null);
  role = computed(() => this.auth.user()?.role ?? '');

  canAccessStock = computed(() =>
    this.role() === 'AdminEntreprise' || this.role() === 'ChefAtelier'
  );

  /** Only AdminEntreprise can create/edit/delete/restock parts */
  isReadOnly = computed(() => this.role() !== 'AdminEntreprise');
}
