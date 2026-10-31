import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.css']
})
export class PaginationComponent {
  @Input() totalCount: number = 0;
  @Input() pageNumber: number = 1;
  @Input() pageSize: number = 25;
  @Input() pageSizeOptions: number[] = [10, 25, 50, 100];

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get startItem(): number {
    if (this.totalCount === 0) return 0;
    return (this.pageNumber - 1) * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalCount);
  }

  get pages(): (number | string)[] {
    const total = this.totalPages;
    const current = this.pageNumber;
    const pages: (number | string)[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) {
        pages.push(i);
      }
      return pages;
    }

    pages.push(1);

    if (current > 3) {
      pages.push('...');
    }

    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    if (current < total - 2) {
      pages.push('...');
    }

    pages.push(total);
    return pages;
  }

  goToPage(page: number | string): void {
    if (typeof page === 'string') return;
    if (page >= 1 && page <= this.totalPages && page !== this.pageNumber) {
      this.pageChange.emit(page);
    }
  }

  onPageSizeChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const newSize = Number(target.value);
    if (newSize > 0 && newSize !== this.pageSize) {
      this.pageSizeChange.emit(newSize);
    }
  }
}

