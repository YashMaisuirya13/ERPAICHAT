import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProductsComponent } from './components/products/products.component';
import { InventoryComponent } from './components/inventory/inventory.component';
import { SuppliersComponent } from './components/suppliers/suppliers.component';
import { PurchaseOrdersComponent } from './components/purchase-orders/purchase-orders.component';
import { AiAssistantComponent } from './components/ai-assistant/ai-assistant.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'products', component: ProductsComponent },
  { path: 'inventory', component: InventoryComponent },
  { path: 'suppliers', component: SuppliersComponent },
  { path: 'purchase-orders', component: PurchaseOrdersComponent },
  { path: 'ai-assistant', component: AiAssistantComponent },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];
