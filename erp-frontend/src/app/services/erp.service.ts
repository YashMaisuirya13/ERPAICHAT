import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ErpService {
  private gatewayUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  getProducts(): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/products`);
  }

  createProduct(product: any): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/products`, product);
  }

  getInventory(): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/inventory`);
  }

  getLowStockInventory(): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/inventory/low-stock`);
  }

  getStockTransactions(): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/inventory/transactions`);
  }

  stockIn(request: any): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/inventory/stock-in`, request);
  }

  stockOut(request: any): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/inventory/stock-out`, request);
  }

  getSuppliers(): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/suppliers`);
  }

  createSupplier(supplier: any): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/suppliers`, supplier);
  }

  getPurchaseOrders(): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/purchaseorders`);
  }

  createPurchaseOrder(po: any): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/purchaseorders`, po);
  }

  submitPO(id: string): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/purchaseorders/${id}/submit`, {});
  }

  approvePO(id: string): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/purchaseorders/${id}/approve`, {});
  }

  reject(id: string, reason: string): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/purchaseorders/${id}/reject`, { reason });
  }

  rejectPO(id: string, reason: string): Observable<any> {
    return this.reject(id, reason);
  }

  receivePO(id: string): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/purchaseorders/${id}/receive`, {});
  }

  cancelPO(id: string): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/purchaseorders/${id}/cancel`, {});
  }

  askAi(prompt: string, conversationGuid?: string): Observable<any> {
    const payload: any = { prompt };
    if (conversationGuid) {
      payload.conversationGuid = conversationGuid;
    }
    return this.http.post(`${this.gatewayUrl}/ai/chat`, payload);
  }

  getAiConversations(): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/ai/conversations`);
  }

  getAiConversation(guid: string): Observable<any> {
    return this.http.get(`${this.gatewayUrl}/ai/conversations/${guid}`);
  }

  deleteAiConversation(guid: string): Observable<any> {
    return this.http.delete(`${this.gatewayUrl}/ai/conversations/${guid}`);
  }
}
