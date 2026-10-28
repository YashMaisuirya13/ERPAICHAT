import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ErpService } from '../../services/erp.service';

export interface SupplierItem {
  id?: number;
  supplierGuid?: string;
  name: string;
  supplierCode: string;
  email: string;
  phone: string;
  address: string;
  status?: string;
}

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './suppliers.component.html',
  styleUrls: ['./suppliers.component.css']
})
export class SuppliersComponent implements OnInit {
  suppliers: SupplierItem[] = [];
  filteredSuppliers: SupplierItem[] = [];

  searchTerm = '';
  sortBy = 'name-asc';
  viewMode: 'grid' | 'table' = 'grid';

  isLoading = true;
  isModalOpen = false;
  isSaving = false;

  newSupplier: SupplierItem = {
    name: '',
    supplierCode: '',
    email: '',
    phone: '',
    address: ''
  };

  notification: { message: string; type: 'success' | 'error' } | null = null;

  constructor(
    private erpService: ErpService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadSuppliers();
  }

  loadSuppliers() {
    this.isLoading = true;
    this.erpService.getSuppliers().subscribe({
      next: (res: any) => {
        let items: any[] = [];
        if (Array.isArray(res)) {
          items = [...res];
        } else if (res?.data && Array.isArray(res.data)) {
          items = [...res.data];
        } else if (res?.items && Array.isArray(res.items)) {
          items = [...res.items];
        } else if (res && typeof res === 'object') {
          const arr = Object.values(res).find(v => Array.isArray(v));
          items = arr ? [...(arr as any[])] : [];
        }

        this.suppliers = items.map(s => ({
          id: s.id ?? s.Id,
          supplierGuid: s.supplierGuid || s.SupplierGuid || s.guid || '',
          name: s.name || s.Name || 'Unnamed Vendor',
          supplierCode: s.supplierCode || s.SupplierCode || 'SUP-000',
          email: s.email || s.Email || 'contact@supplier.com',
          phone: s.phone || s.Phone || '+1 (555) 000-0000',
          address: s.address || s.Address || 'Primary Distribution Hub',
          status: 'Active'
        }));

        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading suppliers:', err);
        this.suppliers = [];
        this.filteredSuppliers = [];
        this.isLoading = false;
        this.showToast('Unable to connect to Supplier Service.', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    let list = [...this.suppliers];

    if (this.searchTerm.trim()) {
      const q = this.searchTerm.toLowerCase().trim();
      list = list.filter(s =>
        s.name.toLowerCase().includes(q) ||
        s.supplierCode.toLowerCase().includes(q) ||
        s.email.toLowerCase().includes(q) ||
        s.phone.toLowerCase().includes(q)
      );
    }

    list.sort((a, b) => {
      switch (this.sortBy) {
        case 'name-asc': return a.name.localeCompare(b.name);
        case 'name-desc': return b.name.localeCompare(a.name);
        case 'code-asc': return a.supplierCode.localeCompare(b.supplierCode);
        default: return 0;
      }
    });

    this.filteredSuppliers = list;
  }

  onSearchChange() {
    this.applyFilters();
  }

  onSortChange(event: any) {
    this.sortBy = event.target.value;
    this.applyFilters();
  }

  openCreateModal() {
    this.newSupplier = {
      name: '',
      supplierCode: this.generateSupplierCode(''),
      email: '',
      phone: '',
      address: ''
    };
    this.isModalOpen = true;
  }

  closeCreateModal() {
    this.isModalOpen = false;
  }

  onNameChange() {
    if (!this.newSupplier.supplierCode || this.newSupplier.supplierCode.startsWith('SUP-')) {
      this.newSupplier.supplierCode = this.generateSupplierCode(this.newSupplier.name);
    }
  }

  generateSupplierCode(name: string): string {
    if (!name.trim()) {
      return `SUP-${Math.floor(1000 + Math.random() * 9000)}`;
    }
    const clean = name.replace(/[^a-zA-Z0-9]/g, '').toUpperCase();
    const prefix = clean.length >= 3 ? clean.substring(0, 3) : 'SUP';
    const rand = Math.floor(100 + Math.random() * 900);
    return `SUP-${prefix}-${rand}`;
  }

  createSupplier() {
    if (!this.newSupplier.name.trim() || !this.newSupplier.supplierCode.trim()) {
      this.showToast('Please provide a vendor name and supplier code.', 'error');
      return;
    }

    this.isSaving = true;
    const payload = {
      name: this.newSupplier.name,
      supplierCode: this.newSupplier.supplierCode,
      email: this.newSupplier.email || `${this.newSupplier.supplierCode.toLowerCase()}@partner.com`,
      phone: this.newSupplier.phone || '+1 (555) 019-2831',
      address: this.newSupplier.address || 'Corporate Headquarters'
    };

    this.erpService.createSupplier(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeCreateModal();
        this.showToast(`Supplier "${this.newSupplier.name}" registered successfully!`, 'success');
        this.loadSuppliers();
      },
      error: (err) => {
        this.isSaving = false;
        this.showToast(err?.error?.message || 'Failed to register supplier.', 'error');
      }
    });
  }

  askCopilotAboutSupplier(supplier: SupplierItem) {
    this.router.navigate(['/ai-assistant'], {
      queryParams: {
        prompt: `Show vendor performance, contact history, and purchase orders for ${supplier.name} (Code: ${supplier.supplierCode})`
      }
    });
  }

  createPoForSupplier(supplier: SupplierItem) {
    this.router.navigate(['/purchase-orders'], {
      queryParams: {
        supplierId: supplier.supplierGuid || supplier.id
      }
    });
  }

  copyCode(code: string, event: Event) {
    event.stopPropagation();
    navigator.clipboard.writeText(code).then(() => {
      this.showToast(`Copied code: ${code}`, 'success');
    });
  }

  // Analytics
  get totalSuppliersCount(): number {
    return this.suppliers.length;
  }

  get verifiedCount(): number {
    return this.suppliers.length;
  }

  getSupplierAvatarColor(name: string): string {
    const colors = [
      'linear-gradient(135deg, #6366f1 0%, #4338ca 100%)',
      'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
      'linear-gradient(135deg, #10b981 0%, #047857 100%)',
      'linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%)',
      'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)'
    ];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  }

  private showToast(message: string, type: 'success' | 'error' = 'success') {
    this.notification = { message, type };
    setTimeout(() => {
      this.notification = null;
      this.cdr.detectChanges();
    }, 3500);
  }
}
