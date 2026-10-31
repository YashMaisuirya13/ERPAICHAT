import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class ErpService {
  private gatewayUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  getProducts(page?: number, pageSize?: number, search?: string): Observable<any> {
    let params = new HttpParams();
    if (page !== undefined) params = params.set('page', page.toString());
    if (pageSize !== undefined) params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get(`${this.gatewayUrl}/products`, { params });
  }

  createProduct(product: any): Observable<any> {
    return this.http.post(`${this.gatewayUrl}/products`, product);
  }

  getInventory(page?: number, pageSize?: number, search?: string, warehouseCode?: string, lowStockOnly?: boolean): Observable<any> {
    let params = new HttpParams();
    if (page !== undefined) params = params.set('page', page.toString());
    if (pageSize !== undefined) params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (warehouseCode) params = params.set('warehouseCode', warehouseCode);
    if (lowStockOnly !== undefined) params = params.set('lowStockOnly', lowStockOnly.toString());
    return this.http.get(`${this.gatewayUrl}/inventory`, { params });
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

  getPurchaseOrders(page?: number, pageSize?: number, search?: string, status?: string): Observable<any> {
    let params = new HttpParams();
    if (page !== undefined) params = params.set('page', page.toString());
    if (pageSize !== undefined) params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (status && status !== 'all') params = params.set('status', status);
    return this.http.get(`${this.gatewayUrl}/purchaseorders`, { params });
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

  askAiStream(prompt: string, conversationGuid?: string): Observable<any> {
    return new Observable(observer => {
      const payload: any = { prompt };
      if (conversationGuid) {
        payload.conversationGuid = conversationGuid;
      }

      const controller = new AbortController();

      fetch(`${this.gatewayUrl}/ai/chat/stream`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'text/event-stream'
        },
        body: JSON.stringify(payload),
        signal: controller.signal
      })
      .then(async response => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        if (!response.body) {
          throw new Error('Response body is null');
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');
        let buffer = '';

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n\n');
          buffer = lines.pop() || '';

          for (const line of lines) {
            const trimmed = line.trim();
            if (trimmed.startsWith('data: ')) {
              try {
                const json = JSON.parse(trimmed.slice(6));
                observer.next(json);
              } catch (e) {
                console.warn('Failed to parse SSE JSON:', trimmed);
              }
            }
          }
        }

        observer.complete();
      })
      .catch(err => {
        if (err.name !== 'AbortError') {
          observer.error(err);
        }
      });

      return () => {
        controller.abort();
      };
    });
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
