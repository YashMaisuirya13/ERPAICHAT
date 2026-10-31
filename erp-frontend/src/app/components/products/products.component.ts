import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ErpService } from '../../services/erp.service';
import { PaginationComponent } from '../shared/pagination/pagination.component';

export interface ProductItem {
  id?: number;
  productId?: number;
  productGuid?: string;
  name: string;
  sku: string;
  unitPrice: number;
  minimumStock: number;
  category?: string;
  createdDate?: string;
}

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.css']
})
export class ProductsComponent implements OnInit, OnDestroy {
  products: ProductItem[] = [];
  filteredProducts: ProductItem[] = [];

  searchTerm = '';
  selectedCategory = 'all';
  sortBy = 'name-asc';
  viewMode: 'grid' | 'table' = 'grid';

  currentPage = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  isModalOpen = false;
  isLoading = true;
  isSaving = false;

  notification: { message: string; type: 'success' | 'error' } | null = null;

  newProduct: ProductItem = {
    name: '',
    sku: '',
    unitPrice: 0,
    minimumStock: 10
  };

  constructor(
    private erpService: ErpService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit() {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadProducts();
    });

    this.loadProducts();
  }

  ngOnDestroy() {
    this.searchSub?.unsubscribe();
  }

  loadProducts() {
    this.isLoading = true;
    this.erpService.getProducts(this.currentPage, this.pageSize, this.searchTerm.trim() || undefined).subscribe({
      next: (res: any) => {
        let items: any[] = [];
        if (res && res.items && Array.isArray(res.items)) {
          items = res.items;
          this.totalCount = res.totalCount !== undefined ? res.totalCount : items.length;
          this.totalPages = res.totalPages || Math.ceil(this.totalCount / this.pageSize);
        } else {
          const allItems: any[] = Array.isArray(res)
            ? res
            : (res?.data && Array.isArray(res.data) ? res.data : (Object.values(res || {}).find(v => Array.isArray(v)) as any[] || []));

          this.totalCount = res?.totalCount || allItems.length;
          this.totalPages = Math.ceil(this.totalCount / this.pageSize);

          const startIndex = (this.currentPage - 1) * this.pageSize;
          items = allItems.length > this.pageSize ? allItems.slice(startIndex, startIndex + this.pageSize) : allItems;
        }

        this.products = items.map(p => ({
          id: p.productId || p.id || p.Id || 0,
          productId: p.productId || p.id || p.Id || 0,
          productGuid: p.productGuid || p.ProductGuid || p.guid || '',
          name: p.name || p.Name || 'Unnamed Product',
          sku: p.sku || p.SKU || 'N/A',
          unitPrice: Number(p.unitPrice !== undefined ? p.unitPrice : p.UnitPrice || 0),
          minimumStock: Number(p.minimumStock !== undefined ? p.minimumStock : p.MinimumStock || 0),
          category: this.detectCategory(p.name || p.Name || '')
        }));

        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading products:', err);
        this.products = [];
        this.filteredProducts = [];
        this.isLoading = false;
        this.showToast('Unable to connect to Inventory Service.', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    let result = [...this.products];

    // Category filter (client-side for category pills)
    if (this.selectedCategory !== 'all') {
      result = result.filter(p => p.category === this.selectedCategory);
    }

    // Sorting
    result.sort((a, b) => {
      switch (this.sortBy) {
        case 'name-asc':
          return a.name.localeCompare(b.name);
        case 'name-desc':
          return b.name.localeCompare(a.name);
        case 'price-asc':
          return a.unitPrice - b.unitPrice;
        case 'price-desc':
          return b.unitPrice - a.unitPrice;
        case 'stock-desc':
          return b.minimumStock - a.minimumStock;
        case 'stock-asc':
          return a.minimumStock - b.minimumStock;
        default:
          return 0;
      }
    });

    this.filteredProducts = result;
  }

  onSearchChange() {
    this.searchSubject.next(this.searchTerm);
  }

  onCategoryChange(cat: string) {
    this.selectedCategory = cat;
    this.applyFilters();
  }

  onSortChange(event: any) {
    this.sortBy = event.target.value;
    this.applyFilters();
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadProducts();
  }

  onPageSizeChange(size: number) {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadProducts();
  }

  openCreateModal() {
    this.newProduct = {
      name: '',
      sku: this.generateSkuSuggestion(''),
      unitPrice: 99.99,
      minimumStock: 10
    };
    this.isModalOpen = true;
  }

  closeCreateModal() {
    this.isModalOpen = false;
  }

  onNameChange() {
    if (!this.newProduct.sku || this.newProduct.sku.startsWith('SKU-')) {
      this.newProduct.sku = this.generateSkuSuggestion(this.newProduct.name);
    }
  }

  generateSkuSuggestion(name: string): string {
    if (!name.trim()) {
      return `SKU-${Math.floor(1000 + Math.random() * 9000)}`;
    }
    const clean = name.replace(/[^a-zA-Z0-9]/g, '').toUpperCase();
    const prefix = clean.length >= 3 ? clean.substring(0, 3) : 'PRD';
    const rand = Math.floor(100 + Math.random() * 900);
    return `${prefix}-${rand}`;
  }

  createProduct() {
    if (!this.newProduct.name.trim() || !this.newProduct.sku.trim()) {
      this.showToast('Please provide a product name and SKU.', 'error');
      return;
    }

    this.isSaving = true;
    this.erpService.createProduct(this.newProduct).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeCreateModal();
        this.showToast(`Product "${this.newProduct.name}" created successfully!`, 'success');
        this.loadProducts();
      },
      error: (err) => {
        this.isSaving = false;
        console.error('Error creating product:', err);
        this.showToast(err?.error?.message || 'Failed to create product. Check SKU uniqueness.', 'error');
      }
    });
  }

  askCopilotAboutProduct(product: ProductItem) {
    this.router.navigate(['/ai-assistant'], {
      queryParams: { prompt: `Check inventory and warehouse stock for product ${product.name} (SKU: ${product.sku})` }
    });
  }

  // Analytics Helpers
  get totalProducts(): number {
    return this.totalCount || this.products.length;
  }

  get averagePrice(): number {
    if (this.products.length === 0) return 0;
    const sum = this.products.reduce((acc, p) => acc + p.unitPrice, 0);
    return sum / this.products.length;
  }

  get highValueCount(): number {
    return this.products.filter(p => p.unitPrice >= 500).length;
  }

  get highestMinStock(): number {
    if (this.products.length === 0) return 0;
    return Math.max(...this.products.map(p => p.minimumStock));
  }

  detectCategory(name: string): string {
    const n = name.toLowerCase();
    if (n.includes('laptop') || n.includes('computer') || n.includes('pc') || n.includes('notebook')) return 'Computing';
    if (n.includes('monitor') || n.includes('screen') || n.includes('display')) return 'Displays';
    if (n.includes('keyboard') || n.includes('mouse') || n.includes('headset') || n.includes('cable')) return 'Peripherals';
    if (n.includes('server') || n.includes('switch') || n.includes('router')) return 'Networking';
    if (n.includes('chair') || n.includes('desk') || n.includes('table')) return 'Furniture';
    return 'General';
  }

  getProductIcon(category?: string): string {
    switch (category) {
      case 'Computing': return '💻';
      case 'Displays': return '🖥️';
      case 'Peripherals': return '⌨️';
      case 'Networking': return '🌐';
      case 'Furniture': return '🪑';
      default: return '📦';
    }
  }

  copySku(sku: string, event: Event) {
    event.stopPropagation();
    navigator.clipboard.writeText(sku).then(() => {
      this.showToast(`Copied SKU: ${sku}`, 'success');
    });
  }

  private showToast(message: string, type: 'success' | 'error' = 'success') {
    this.notification = { message, type };
    setTimeout(() => {
      this.notification = null;
      this.cdr.detectChanges();
    }, 3500);
  }
}
