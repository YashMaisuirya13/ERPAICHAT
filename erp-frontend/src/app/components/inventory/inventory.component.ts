import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ErpService } from '../../services/erp.service';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.css']
})
export class InventoryComponent implements OnInit {
  inventory: any[] = [];
  products: any[] = [];
  stockForm = { productId: 1, quantity: 10, reference: 'INIT' };

  constructor(private erpService: ErpService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadInventory();
    this.loadProducts();
  }

  loadInventory() {
    this.erpService.getInventory().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.inventory = [...res];
        } else if (res?.data && Array.isArray(res.data)) {
          this.inventory = [...res.data];
        } else if (res?.items && Array.isArray(res.items)) {
          this.inventory = [...res.items];
        } else {
          this.inventory = [];
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.inventory = [];
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
        if (this.products.length > 0 && !this.stockForm.productId) {
          this.stockForm.productId = this.products[0].productId || this.products[0].id || 1;
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.products = [];
        this.cdr.detectChanges();
      }
    });
  }

  doStockIn() {
    this.erpService.stockIn(this.stockForm).subscribe({
      next: () => this.loadInventory()
    });
  }

  doStockOut() {
    this.erpService.stockOut(this.stockForm).subscribe({
      next: () => this.loadInventory()
    });
  }
}
