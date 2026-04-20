import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedResult, ProductDto, ProductQueryParams } from '../models/api.models';

/**
 * Catálogo público: listagem paginada e detalhe por id (`GET /products`).
 */
@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  getPaged(params: ProductQueryParams = {}): Observable<PagedResult<ProductDto>> {
    let httpParams = new HttpParams();
    if (params.page != null) httpParams = httpParams.set('page', String(params.page));
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', String(params.pageSize));
    if (params.category) httpParams = httpParams.set('category', params.category);
    if (params.search) httpParams = httpParams.set('search', params.search);
    return this.http
      .get<ApiResponse<PagedResult<ProductDto>>>(`${environment.apiUrl}/products`, { params: httpParams })
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Erro ao carregar produtos');
          }
          return r.data;
        })
      );
  }

  getById(id: string): Observable<ProductDto> {
    return this.http.get<ApiResponse<ProductDto>>(`${environment.apiUrl}/products/${id}`).pipe(
      map((r) => {
        if (!r.data) {
          throw new Error(r.message ?? 'Produto não encontrado');
        }
        return r.data;
      })
    );
  }
}
