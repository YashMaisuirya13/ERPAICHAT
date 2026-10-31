import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ErpService } from '../../services/erp.service';
import { AuthService } from '../../services/auth.service';
import { PaginationComponent } from '../shared/pagination/pagination.component';

export interface PurchaseOrderItem {
  id?: number;
  purchaseOrderGuid?: string;
  poNumber?: string;
  supplierId: any;
  supplierName?: string;
  status: string;
  totalAmount: number;
  items?: any[];
  createdDate?: string;
}

@Component({
  selector: 'app-purchase-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './purchase-orders.component.html',
  styleUrls: ['./purchase-orders.component.css']
})
export class PurchaseOrdersComponent implements OnInit, OnDestroy {
  purchaseOrders: PurchaseOrderItem[] = [];
  filteredPOs: PurchaseOrderItem[] = [];
  suppliers: any[] = [];
  products: any[] = [];

  userRole = '';
  searchTerm = '';
  statusFilter = 'all';
  sortBy = 'date-desc';

  currentPage = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  isLoading = true;
  isModalOpen = false;
  isSaving = false;

  newPo = { supplierId: '' as any };
  newItem = { productId: 1, quantity: 20, unitPrice: 999.0 };

  notification: { message: string; type: 'success' | 'error' } | null = null;

  constructor(
    private erpService: ErpService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    this.userRole = this.authService.getRole() || 'Admin';

    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadPurchaseOrders();
    });

    this.loadAllData();

    this.route.queryParams.subscribe(params => {
      if (params['supplierId']) {
        setTimeout(() => {
          this.newPo.supplierId = params['supplierId'];
          this.openCreateModal();
        }, 500);
      }
    });
  }

  ngOnDestroy() {
    this.searchSub?.unsubscribe();
  }

  loadAllData() {
    this.isLoading = true;
    this.erpService.getSuppliers().subscribe({
      next: (supRes: any) => {
        this.suppliers = this.extractArray(supRes);
        if (this.suppliers.length > 0 && !this.newPo.supplierId) {
          this.newPo.supplierId = this.suppliers[0].supplierGuid || this.suppliers[0].SupplierGuid || this.suppliers[0].id;
        }

        this.erpService.getProducts().subscribe({
          next: (prodRes: any) => {
            this.products = this.extractArray(prodRes);
            if (this.products.length > 0) {
              this.newItem.productId = this.products[0].productId || this.products[0].id || 1;
              if (this.products[0].unitPrice !== undefined) {
                this.newItem.unitPrice = this.products[0].unitPrice;
              }
            }
            this.loadPurchaseOrders();
          },
          error: () => {
            this.products = [];
            this.loadPurchaseOrders();
          }
        });
      },
      error: () => {
        this.suppliers = [];
        this.loadPurchaseOrders();
      }
    });
  }

  loadPurchaseOrders() {
    this.isLoading = true;
    const filterStatus = this.statusFilter !== 'all' ? this.statusFilter : undefined;

    this.erpService.getPurchaseOrders(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      filterStatus
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

        this.purchaseOrders = rawItems.map((po: any) => {
          const rawSupplierGuid = po.supplierGuid || po.SupplierGuid || po.supplierId || po.SupplierId;
          const supplierGuidStr = rawSupplierGuid ? String(rawSupplierGuid).trim().toLowerCase() : '';

          const supplier = this.suppliers.find(s => {
            const sGuid = (s.supplierGuid || s.SupplierGuid) ? String(s.supplierGuid || s.SupplierGuid).trim().toLowerCase() : '';
            const sId = (s.id !== undefined && s.id !== null) ? String(s.id).trim().toLowerCase() : '';
            return (sGuid && sGuid === supplierGuidStr) || (sId && sId === supplierGuidStr);
          });

          return {
            id: po.id,
            purchaseOrderGuid: po.purchaseOrderGuid || po.PurchaseOrderGuid || po.guid || `PO-${po.id}`,
            poNumber: po.poNumber || po.PONumber || '',
            supplierId: rawSupplierGuid,
            supplierName: supplier?.name || supplier?.Name || po.supplierName || po.SupplierName || (rawSupplierGuid ? `Supplier (${String(rawSupplierGuid).substring(0, 8)}...)` : 'Unknown Supplier'),
            status: po.status || po.Status || 'Draft',
            totalAmount: Number(po.totalAmount ?? po.TotalAmount ?? 0),
            items: po.items || po.Items || [],
            createdDate: po.orderDate || po.OrderDate || po.createdDate || po.createdAt || new Date().toISOString()
          };
        });

        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading purchase orders:', err);
        this.purchaseOrders = [];
        this.filteredPOs = [];
        this.isLoading = false;
        this.showToast('Unable to connect to Procurement Service.', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    let list = [...this.purchaseOrders];

    list.sort((a, b) => {
      switch (this.sortBy) {
        case 'amount-desc': return b.totalAmount - a.totalAmount;
        case 'amount-asc': return a.totalAmount - b.totalAmount;
        case 'status': return a.status.localeCompare(b.status);
        default: return 0;
      }
    });

    this.filteredPOs = list;
  }

  onSearchChange() {
    this.searchSubject.next(this.searchTerm);
  }

  onFilterChange(status: string) {
    this.statusFilter = status;
    this.currentPage = 1;
    this.loadPurchaseOrders();
  }

  onSortChange(event: any) {
    this.sortBy = event.target.value;
    this.applyFilters();
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadPurchaseOrders();
  }

  onPageSizeChange(size: number) {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadPurchaseOrders();
  }

  onProductSelectChange() {
    const selectedProd = this.products.find(p => (p.productId || p.id) === Number(this.newItem.productId));
    if (selectedProd && selectedProd.unitPrice !== undefined) {
      this.newItem.unitPrice = selectedProd.unitPrice;
    }
  }

  openCreateModal() {
    if (this.suppliers.length > 0 && !this.newPo.supplierId) {
      this.newPo.supplierId = this.suppliers[0].supplierGuid || this.suppliers[0].SupplierGuid || this.suppliers[0].id;
    }
    this.isModalOpen = true;
  }

  closeCreateModal() {
    this.isModalOpen = false;
  }

  createPO() {
    if (!this.newPo.supplierId) {
      this.showToast('Please select a supplier.', 'error');
      return;
    }

    const selectedProduct = this.products.find(p =>
      (p.productId || p.id) === Number(this.newItem.productId) ||
      p.productGuid === String(this.newItem.productId)
    );
    const productGuid = selectedProduct?.productGuid || selectedProduct?.ProductGuid || '';

    if (!productGuid) {
      this.showToast('Selected product has no GUID. Please select a valid product.', 'error');
      return;
    }

    this.isSaving = true;
    const payload = {
      supplierGuid: this.newPo.supplierId,
      items: [
        {
          productGuid: productGuid,
          quantity: Number(this.newItem.quantity),
          unitPrice: Number(this.newItem.unitPrice)
        }
      ]
    };

    this.erpService.createPurchaseOrder(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeCreateModal();
        this.showToast('Draft Purchase Order created successfully!', 'success');
        this.loadPurchaseOrders();
      },
      error: (err) => {
        this.isSaving = false;
        this.showToast(err?.error?.message || 'Failed to create purchase order.', 'error');
      }
    });
  }

  submit(id: string) {
    this.erpService.submitPO(id).subscribe({
      next: () => {
        this.showToast('Purchase Order submitted for Manager approval! 🚀', 'success');
        this.loadPurchaseOrders();
      },
      error: (err) => this.showToast(err?.error?.message || 'Submission failed.', 'error')
    });
  }

  approve(id: string) {
    this.erpService.approvePO(id).subscribe({
      next: () => {
        this.showToast('Purchase Order Approved successfully! ✅', 'success');
        this.loadPurchaseOrders();
      },
      error: (err) => this.showToast(err?.error?.message || 'Approval failed.', 'error')
    });
  }

  reject(id: string) {
    this.erpService.rejectPO(id, 'Rejected by Purchasing Manager').subscribe({
      next: () => {
        this.showToast('Purchase Order Rejected. ❌', 'error');
        this.loadPurchaseOrders();
      },
      error: (err) => this.showToast(err?.error?.message || 'Rejection failed.', 'error')
    });
  }

  receive(id: string) {
    this.erpService.receivePO(id).subscribe({
      next: () => {
        this.showToast('Stock Received & restocked into Inventory! 📥', 'success');
        this.loadPurchaseOrders();
      },
      error: (err) => this.showToast(err?.error?.message || 'Receipt failed.', 'error')
    });
  }

  askCopilotAboutPO(po: PurchaseOrderItem) {
    this.router.navigate(['/ai-assistant'], {
      queryParams: {
        prompt: `Audit approval status, vendor contract, and delivery terms for Purchase Order ${po.purchaseOrderGuid}`
      }
    });
  }

  // Analytics Helpers
  get totalPOCount(): number {
    return this.totalCount || this.purchaseOrders.length;
  }

  get pendingApprovalCount(): number {
    return this.purchaseOrders.filter(po => po.status === 'Submitted' || po.status === 'PendingApproval').length;
  }

  get approvedCount(): number {
    return this.purchaseOrders.filter(po => po.status === 'Approved').length;
  }

  get receivedCount(): number {
    return this.purchaseOrders.filter(po => po.status === 'Received').length;
  }

  get totalSpend(): number {
    return this.purchaseOrders.reduce((sum, po) => sum + (po.totalAmount || 0), 0);
  }

  get calculatedTotal(): number {
    return (this.newItem.quantity || 0) * (this.newItem.unitPrice || 0);
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
