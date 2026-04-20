import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { OrderDto, OrderQueryParams, OrderSummaryDto, PagedResult } from '../models/api.models';

/**
 * Pedidos do cliente: checkout, listagem paginada e detalhe (`/orders`).
 */
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  checkout(): Observable<OrderDto> {
    return this.http
      .post<ApiResponse<OrderDto>>(`${environment.apiUrl}/orders/checkout`, {})
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Checkout falhou');
          }
          return r.data;
        })
      );
  }

  getOrders(params: OrderQueryParams = {}): Observable<PagedResult<OrderSummaryDto>> {
    let httpParams = new HttpParams();
    if (params.page != null) httpParams = httpParams.set('page', String(params.page));
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', String(params.pageSize));
    return this.http
      .get<ApiResponse<PagedResult<OrderSummaryDto>>>(`${environment.apiUrl}/orders`, {
        params: httpParams
      })
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Erro ao listar pedidos');
          }
          return r.data;
        })
      );
  }

  getOrderById(id: string): Observable<OrderDto> {
    return this.http
      .get<ApiResponse<OrderDto>>(`${environment.apiUrl}/orders/${id}`)
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Pedido não encontrado');
          }
          return r.data;
        })
      );
  }
}
