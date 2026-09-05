import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ErpService } from '../../services/erp.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.css']
})
export class ProductsComponent implements OnInit {
  products: any[] = [];
  newProduct = { name: '', sku: '', unitPrice: 0, minimumStock: 10 };

  constructor(private erpService: ErpService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.erpService.getProducts().subscribe({
      next: (res: any) => {
        console.log('Products API response:', res);
        if (Array.isArray(res)) {
          this.products = [...res];
        } else if (res?.data && Array.isArray(res.data)) {
          this.products = [...res.data];
        } else if (res?.items && Array.isArray(res.items)) {
          this.products = [...res.items];
        } else if (res && typeof res === 'object') {
          // If it's an object with product properties or values
          const possibleArray = Object.values(res).find(val => Array.isArray(val));
          this.products = possibleArray ? [...(possibleArray as any[])] : [];
        } else {
          this.products = [];
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading products:', err);
        this.products = [];
        this.cdr.detectChanges();
      }
    });
  }

  createProduct() {
    this.erpService.createProduct(this.newProduct).subscribe({
      next: () => {
        this.newProduct = { name: '', sku: '', unitPrice: 0, minimumStock: 10 };
        this.loadProducts();
      },
      error: (err) => {
        console.error('Error creating product:', err);
      }
    });
  }
}
