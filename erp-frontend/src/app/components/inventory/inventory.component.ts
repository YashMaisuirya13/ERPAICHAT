import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ErpService } from '../../services/erp.service';
import { PaginationComponent } from '../shared/pagination/pagination.component';

export interface InventoryItem {
  id?: number;
  productId: number;
  productGuid?: string;
  productName?: string;
  sku?: string;
  warehouseCode?: string;
  quantityAvailable: number;
  reorderLevel: number;
  lastUpdated?: string;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.css']
})
export class InventoryComponent implements OnInit, OnDestroy {
  inventory: InventoryItem[] = [];
  filteredInventory: InventoryItem[] = [];
  products: any[] = [];

  searchTerm = '';
  statusFilter: 'all' | 'low' | 'healthy' = 'all';
  sortBy = 'qty-asc';

  currentPage = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  isLoading = true;
  isModalOpen = false;
  isSaving = false;

  stockForm = {
    productId: 1,
    quantity: 10,
    reference: 'RESTOCK-AUDIT',
    warehouseCode: 'WH-CENTRAL'
  };

  notification: { message: string; type: 'success' | 'error' } | null = null;

  constructor(
    private erpService: ErpService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) { }

  ngOnInit() {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadInventory();
    });

    this.loadAllData();
  }

  ngOnDestroy() {
    this.searchSub?.unsubscribe();
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
    this.isLoading = true;
    const lowStockOnly = this.statusFilter === 'low' ? true : undefined;

    this.erpService.getInventory(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      undefined,
      lowStockOnly
    ).subscribe({
      next: (res: any) => {
        let rawItems: any[] = [];
        if (res && res.items && Array.isArray(res.items)) {
          rawItems = res.items;
          this.totalCount = res.totalCount !== undefined ? res.totalCount : rawItems.length;
          this.totalPages = res.totalPages || Math.ceil(this.totalCount / this.pageSize);
        } else {
          const allItems = this.extractArray(res);
          this.totalCount = res?.totalCount || allItems.length;
          this.totalPages = Math.ceil(this.totalCount / this.pageSize);

          const startIndex = (this.currentPage - 1) * this.pageSize;
          rawItems = allItems.length > this.pageSize ? allItems.slice(startIndex, startIndex + this.pageSize) : allItems;
        }

        this.inventory = rawItems.map((item: any) => {
          const prod = this.products.find(p =>
            (item.productId && (p.productId === item.productId || p.id === item.productId)) ||
            (item.productGuid && p.productGuid === item.productGuid) ||
            (item.sku && (p.sku === item.sku || p.SKU === item.sku))
          );

          const productId = item.productId || item.ProductId || prod?.id || prod?.productId;

          return {
            id: item.id || item.Id || item.inventoryGuid || productId,
            productId: productId,
            productGuid: item.productGuid || prod?.productGuid || '',
            productName: item.productName || item.ProductName || prod?.name || prod?.Name || (productId ? `Product #${productId}` : 'Unnamed Product'),
            sku: item.sku || item.SKU || prod?.sku || prod?.SKU || 'N/A',
            warehouseCode: item.warehouseCode || item.WarehouseCode || 'WH-CENTRAL',
            quantityAvailable: Number(item.quantityAvailable !== undefined ? item.quantityAvailable : (item.quantity !== undefined ? item.quantity : 0)),
            reorderLevel: Number(item.reorderLevel !== undefined ? item.reorderLevel : (item.minimumStock !== undefined ? item.minimumStock : 10))
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

    if (this.statusFilter === 'healthy') {
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
    this.searchSubject.next(this.searchTerm);
  }

  onFilterChange(status: 'all' | 'low' | 'healthy') {
    this.statusFilter = status;
    this.currentPage = 1;
    this.loadInventory();
  }

  onSortChange(event: any) {
    this.sortBy = event.target.value;
    this.applyFilters();
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadInventory();
  }

  onPageSizeChange(size: number) {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadInventory();
  }

  openStockModal(defaultProductId?: number) {
    if (defaultProductId) {
      this.stockForm.productId = defaultProductId;
      const matched = this.inventory.find(i => i.productId === defaultProductId);
      if (matched?.warehouseCode) {
        this.stockForm.warehouseCode = matched.warehouseCode;
      }
    } else if (this.products.length > 0) {
      this.stockForm.productId = this.products[0].productId || this.products[0].id || 1;
    }
    this.stockForm.quantity = 25;
    this.stockForm.reference = 'MANUAL-AUDIT';
    if (!this.stockForm.warehouseCode) {
      this.stockForm.warehouseCode = 'WH-CENTRAL';
    }
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
    const payload = {
      productId: Number(this.stockForm.productId),
      quantity: Number(this.stockForm.quantity),
      warehouseCode: this.stockForm.warehouseCode || 'WH-CENTRAL',
      referenceNumber: this.stockForm.reference || 'MANUAL-AUDIT'
    };
    this.erpService.stockIn(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeStockModal();
        this.showToast(`Stock In: +${this.stockForm.quantity} units recorded for ${payload.warehouseCode}!`, 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.isSaving = false;
        const errMsg = err?.error?.message || err?.error?.title || (err?.error?.errors ? Object.values(err.error.errors).flat().join(', ') : 'Stock In failed.');
        this.showToast(errMsg, 'error');
      }
    });
  }

  doStockOut() {
    if (!this.stockForm.quantity || this.stockForm.quantity <= 0) {
      this.showToast('Please specify a positive quantity.', 'error');
      return;
    }
    this.isSaving = true;
    const payload = {
      productId: Number(this.stockForm.productId),
      quantity: Number(this.stockForm.quantity),
      warehouseCode: this.stockForm.warehouseCode || 'WH-CENTRAL',
      referenceNumber: this.stockForm.reference || 'MANUAL-AUDIT'
    };
    this.erpService.stockOut(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeStockModal();
        this.showToast(`Stock Out: -${this.stockForm.quantity} units recorded from ${payload.warehouseCode}!`, 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.isSaving = false;
        const errMsg = err?.error?.message || err?.error?.title || (err?.error?.errors ? Object.values(err.error.errors).flat().join(', ') : 'Stock Out failed. Insufficient stock.');
        this.showToast(errMsg, 'error');
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
    return this.totalCount || this.inventory.length;
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
