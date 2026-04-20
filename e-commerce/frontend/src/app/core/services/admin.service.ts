import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, UserDto } from '../models/auth.models';
import {
  AdminOrderQueryParams,
  AdminUserQueryParams,
  CreateProductDto,
  OrderSummaryDto,
  PagedResult,
  ProductDto
} from '../models/api.models';

/**
 * Operações administrativas: produtos, pedidos e usuários (rotas `/admin/...`).
 */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  createProduct(dto: CreateProductDto): Observable<ProductDto> {
    return this.http.post<ApiResponse<ProductDto>>(`${environment.apiUrl}/admin/products`, dto).pipe(
      map((r) => {
        if (!r.data) {
          throw new Error(r.message ?? 'Não foi possível criar o produto');
        }
        return r.data;
      })
    );
  }

  getOrders(query: AdminOrderQueryParams = {}): Observable<PagedResult<OrderSummaryDto>> {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', String(query.page));
    if (query.pageSize != null) params = params.set('pageSize', String(query.pageSize));
    if (query.userId) params = params.set('userId', query.userId);
    if (query.status != null && query.status !== undefined) params = params.set('status', String(query.status));
    return this.http
      .get<ApiResponse<PagedResult<OrderSummaryDto>>>(`${environment.apiUrl}/admin/orders`, { params })
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Erro ao listar pedidos');
          }
          return r.data;
        })
      );
  }

  getUsers(query: AdminUserQueryParams = {}): Observable<PagedResult<UserDto>> {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', String(query.page));
    if (query.pageSize != null) params = params.set('pageSize', String(query.pageSize));
    return this.http.get<ApiResponse<PagedResult<UserDto>>>(`${environment.apiUrl}/admin/users`, { params }).pipe(
      map((r) => {
        if (!r.data) {
          throw new Error(r.message ?? 'Erro ao listar usuários');
        }
        return r.data;
      })
    );
  }
}
