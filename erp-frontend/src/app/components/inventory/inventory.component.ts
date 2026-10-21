import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ErpService } from '../../services/erp.service';

export interface InventoryItem {
  id?: number;
  productId: number;
  productGuid?: string;
  productName?: string;
  sku?: string;
  quantityAvailable: number;
  reorderLevel: number;
  lastUpdated?: string;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.css']
})
export class InventoryComponent implements OnInit {
  inventory: InventoryItem[] = [];
  filteredInventory: InventoryItem[] = [];
  products: any[] = [];

  searchTerm = '';
  statusFilter: 'all' | 'low' | 'healthy' = 'all';
  sortBy = 'qty-asc';

  isLoading = true;
  isModalOpen = false;
  isSaving = false;

  stockForm = {
    productId: 1,
    quantity: 10,
    reference: 'RESTOCK-AUDIT'
  };

  notification: { message: string; type: 'success' | 'error' } | null = null;

  constructor(
    private erpService: ErpService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadAllData();
  }

  loadAllData() {
    this.isLoading = true;
    this.erpService.getProducts().subscribe({
      next: (prodRes: any) => {
        this.products = this.extractArray(prodRes);
        this.loadInventory();
      },
      error: () => {
        this.products = [];
        this.loadInventory();
      }
    });
  }

  loadInventory() {
    this.erpService.getInventory().subscribe({
      next: (res: any) => {
        const rawItems = this.extractArray(res);

        this.inventory = rawItems.map((item: any) => {
          const prod = this.products.find(p =>
            (p.productId && p.productId === item.productId) ||
            (p.id && p.id === item.productId)
          );

          return {
            id: item.id || item.productId,
            productId: item.productId,
            productGuid: item.productGuid || prod?.productGuid || '',
            productName: prod?.name || prod?.Name || `Product #${item.productId}`,
            sku: prod?.sku || prod?.SKU || 'N/A',
            quantityAvailable: Number(item.quantityAvailable !== undefined ? item.quantityAvailable : item.quantity || 0),
            reorderLevel: Number(item.reorderLevel !== undefined ? item.reorderLevel : item.minimumStock || 10)
          };
        });

        if (this.products.length > 0 && !this.stockForm.productId) {
          this.stockForm.productId = this.products[0].productId || this.products[0].id || 1;
        }

        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading inventory:', err);
        this.inventory = [];
        this.filteredInventory = [];
        this.isLoading = false;
        this.showToast('Failed to load inventory from InventoryService.', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    let list = [...this.inventory];

    if (this.searchTerm.trim()) {
      const q = this.searchTerm.toLowerCase().trim();
      list = list.filter(item =>
        item.productName?.toLowerCase().includes(q) ||
        item.sku?.toLowerCase().includes(q) ||
        item.productId.toString().includes(q)
      );
    }

    if (this.statusFilter === 'low') {
      list = list.filter(item => item.quantityAvailable <= item.reorderLevel);
    } else if (this.statusFilter === 'healthy') {
      list = list.filter(item => item.quantityAvailable > item.reorderLevel);
    }

    list.sort((a, b) => {
      switch (this.sortBy) {
        case 'qty-asc': return a.quantityAvailable - b.quantityAvailable;
        case 'qty-desc': return b.quantityAvailable - a.quantityAvailable;
        case 'name-asc': return (a.productName || '').localeCompare(b.productName || '');
        case 'threshold-desc': return b.reorderLevel - a.reorderLevel;
        default: return 0;
      }
    });

    this.filteredInventory = list;
  }

  onSearchChange() {
    this.applyFilters();
  }

  onFilterChange(status: 'all' | 'low' | 'healthy') {
    this.statusFilter = status;
    this.applyFilters();
  }

  onSortChange(event: any) {
    this.sortBy = event.target.value;
    this.applyFilters();
  }

  openStockModal(defaultProductId?: number) {
    if (defaultProductId) {
      this.stockForm.productId = defaultProductId;
    } else if (this.products.length > 0) {
      this.stockForm.productId = this.products[0].productId || this.products[0].id || 1;
    }
    this.stockForm.quantity = 25;
    this.stockForm.reference = 'MANUAL-AUDIT';
    this.isModalOpen = true;
  }

  closeStockModal() {
    this.isModalOpen = false;
  }

  doStockIn() {
    if (!this.stockForm.quantity || this.stockForm.quantity <= 0) {
      this.showToast('Please specify a positive quantity.', 'error');
      return;
    }
    this.isSaving = true;
    this.erpService.stockIn(this.stockForm).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeStockModal();
        this.showToast(`Stock In: +${this.stockForm.quantity} units recorded!`, 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.isSaving = false;
        this.showToast(err?.error?.message || 'Stock In failed.', 'error');
      }
    });
  }

  doStockOut() {
    if (!this.stockForm.quantity || this.stockForm.quantity <= 0) {
      this.showToast('Please specify a positive quantity.', 'error');
      return;
    }
    this.isSaving = true;
    this.erpService.stockOut(this.stockForm).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeStockModal();
        this.showToast(`Stock Out: -${this.stockForm.quantity} units recorded!`, 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.isSaving = false;
        this.showToast(err?.error?.message || 'Stock Out failed. Insufficient stock.', 'error');
      }
    });
  }

  askCopilotAboutInventory(item: InventoryItem) {
    this.router.navigate(['/ai-assistant'], {
      queryParams: {
        prompt: `Audit stock availability and reorder requirements for ${item.productName} (SKU: ${item.sku})`
      }
    });
  }

  // Analytics Helpers
  get totalWarehousedUnits(): number {
    return this.inventory.reduce((sum, item) => sum + item.quantityAvailable, 0);
  }

  get lowStockAlertsCount(): number {
    return this.inventory.filter(item => item.quantityAvailable <= item.reorderLevel).length;
  }

  get healthyStockCount(): number {
    return this.inventory.filter(item => item.quantityAvailable > item.reorderLevel).length;
  }

  get totalItemsTracked(): number {
    return this.inventory.length;
  }

  getSelectedProduct(): any {
    return this.products.find(p => (p.productId || p.id) === Number(this.stockForm.productId));
  }

  private extractArray(res: any): any[] {
    if (Array.isArray(res)) return res;
    if (res?.data && Array.isArray(res.data)) return res.data;
    if (res?.items && Array.isArray(res.items)) return res.items;
    if (res && typeof res === 'object') {
      const arr = Object.values(res).find(v => Array.isArray(v));
      if (arr) return arr as any[];
    }
    return [];
  }

  private showToast(message: string, type: 'success' | 'error' = 'success') {
    this.notification = { message, type };
    setTimeout(() => {
      this.notification = null;
      this.cdr.detectChanges();
    }, 3500);
  }
}
