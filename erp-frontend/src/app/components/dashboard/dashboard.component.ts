import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ErpService } from '../../services/erp.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  productsCount = 0;
  inventoryCount = 0;
  lowStockCount = 0;
  suppliersCount = 0;
  poCount = 0;

  constructor(private erpService: ErpService) {}

  ngOnInit() {
    this.erpService.getProducts().subscribe({ next: (res: any) => {
      const arr = Array.isArray(res) ? res : (res?.data || res?.items || []);
      this.productsCount = arr.length;
    }});
    this.erpService.getInventory().subscribe({ next: (res: any) => {
      const arr = Array.isArray(res) ? res : (res?.data || res?.items || []);
      this.inventoryCount = arr.length;
    }});
    this.erpService.getLowStockInventory().subscribe({ next: (res: any) => {
      const arr = Array.isArray(res) ? res : (res?.data || res?.items || []);
      this.lowStockCount = arr.length;
    }});
    this.erpService.getSuppliers().subscribe({ next: (res: any) => {
      const arr = Array.isArray(res) ? res : (res?.data || res?.items || []);
      this.suppliersCount = arr.length;
    }});
    this.erpService.getPurchaseOrders().subscribe({ next: (res: any) => {
      const arr = Array.isArray(res) ? res : (res?.data || res?.items || []);
      this.poCount = arr.length;
    }});
  }
}
