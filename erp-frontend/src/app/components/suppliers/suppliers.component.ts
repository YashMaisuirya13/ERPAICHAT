import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ErpService } from '../../services/erp.service';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './suppliers.component.html',
  styleUrls: ['./suppliers.component.css']
})
export class SuppliersComponent implements OnInit {
  suppliers: any[] = [];
  newSupplier = { name: '', supplierCode: '', email: '', phone: '', address: '' };

  constructor(private erpService: ErpService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadSuppliers();
  }

  loadSuppliers() {
    this.erpService.getSuppliers().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.suppliers = [...res];
        } else if (res?.data && Array.isArray(res.data)) {
          this.suppliers = [...res.data];
        } else if (res?.items && Array.isArray(res.items)) {
          this.suppliers = res.items;
        } else {
          this.suppliers = [];
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.suppliers = [];
        this.cdr.detectChanges();
      }
    });
  }

  createSupplier() {
    const payload = {
      name: this.newSupplier.name,
      supplierCode: this.newSupplier.supplierCode,
      email: this.newSupplier.email,
      phone: this.newSupplier.phone,
      address: this.newSupplier.address || 'N/A'
    };
    this.erpService.createSupplier(payload).subscribe({
      next: () => {
        this.newSupplier = { name: '', supplierCode: '', email: '', phone: '', address: '' };
        this.loadSuppliers();
      },
      error: (err) => {
        console.error('Error creating supplier:', err);
      }
    });
  }
}
