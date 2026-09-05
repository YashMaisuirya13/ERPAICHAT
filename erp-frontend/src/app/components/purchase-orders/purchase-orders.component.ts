import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ErpService } from '../../services/erp.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-purchase-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './purchase-orders.component.html',
  styleUrls: ['./purchase-orders.component.css']
})
export class PurchaseOrdersComponent implements OnInit {
  purchaseOrders: any[] = [];
  suppliers: any[] = [];
  products: any[] = [];
  userRole = '';
  newPo = { supplierId: 1 };
  newItem = { productId: 1, quantity: 50, unitPrice: 10.0 };

  constructor(
    private erpService: ErpService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.userRole = this.authService.getRole() || 'Admin';
    this.loadPurchaseOrders();
    this.loadSuppliers();
    this.loadProducts();
  }

  loadPurchaseOrders() {
    this.erpService.getPurchaseOrders().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.purchaseOrders = [...res];
        } else if (res?.data && Array.isArray(res.data)) {
          this.purchaseOrders = [...res.data];
        } else if (res?.items && Array.isArray(res.items)) {
          this.purchaseOrders = [...res.items];
        } else {
          this.purchaseOrders = [];
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.purchaseOrders = [];
        this.cdr.detectChanges();
      }
    });
  }

  loadSuppliers() {
    this.erpService.getSuppliers().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.suppliers = [...res];
        } else if (res?.data && Array.isArray(res.data)) {
          this.suppliers = [...res.data];
        } else if (res?.items && Array.isArray(res.items)) {
          this.suppliers = [...res.items];
        } else {
          this.suppliers = [];
        }
        if (this.suppliers.length > 0) {
          this.newPo.supplierId = this.suppliers[0].supplierGuid || this.suppliers[0].id || 1;
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.suppliers = [];
        this.cdr.detectChanges();
      }
    });
  }

  loadProducts() {
    this.erpService.getProducts().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.products = [...res];
        } else if (res?.data && Array.isArray(res.data)) {
          this.products = [...res.data];
        } else if (res?.items && Array.isArray(res.items)) {
          this.products = [...res.items];
        } else {
          this.products = [];
        }
        if (this.products.length > 0) {
          this.newItem.productId = this.products[0].productId || this.products[0].id || 1;
          if (this.products[0].unitPrice !== undefined) {
            this.newItem.unitPrice = this.products[0].unitPrice;
          }
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.products = [];
        this.cdr.detectChanges();
      }
    });
  }

  createPO() {
    const payload = {
      supplierId: this.newPo.supplierId,
      items: [this.newItem]
    };
    this.erpService.createPurchaseOrder(payload).subscribe({
      next: () => this.loadPurchaseOrders()
    });
  }

  submit(id: string) {
    this.erpService.submitPO(id).subscribe({ next: () => this.loadPurchaseOrders() });
  }

  approve(id: string) {
    this.erpService.approvePO(id).subscribe({ next: () => this.loadPurchaseOrders() });
  }

  reject(id: string) {
    this.erpService.rejectPO(id, 'Rejected by Manager').subscribe({ next: () => this.loadPurchaseOrders() });
  }

  receive(id: string) {
    this.erpService.receivePO(id).subscribe({ next: () => this.loadPurchaseOrders() });
  }
}
