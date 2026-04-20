import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { AddCartItemDto, CartDto, UpdateCartItemDto } from '../models/api.models';

/**
 * Carrinho do cliente (`/cart`): leitura, itens, limpeza. Estado local em `cart$` após mutações.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly cartSubject = new BehaviorSubject<CartDto | null>(null);
  readonly cart$ = this.cartSubject.asObservable();

  getCart(): Observable<CartDto> {
    return this.http
      .get<ApiResponse<CartDto>>(`${environment.apiUrl}/cart`)
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Carrinho inválido');
          }
          return r.data;
        }),
        tap((c) => this.cartSubject.next(c))
      );
  }

  addItem(dto: AddCartItemDto): Observable<CartDto> {
    return this.http
      .post<ApiResponse<CartDto>>(`${environment.apiUrl}/cart/items`, dto)
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Erro ao adicionar');
          }
          return r.data;
        }),
        tap((c) => this.cartSubject.next(c))
      );
  }

  updateItem(productId: string, dto: UpdateCartItemDto): Observable<CartDto> {
    return this.http
      .put<ApiResponse<CartDto>>(`${environment.apiUrl}/cart/items/${productId}`, dto)
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Erro ao atualizar');
          }
          return r.data;
        }),
        tap((c) => this.cartSubject.next(c))
      );
  }

  removeItem(productId: string): Observable<CartDto> {
    return this.http
      .delete<ApiResponse<CartDto>>(`${environment.apiUrl}/cart/items/${productId}`)
      .pipe(
        map((r) => {
          if (!r.data) {
            throw new Error(r.message ?? 'Erro ao remover');
          }
          return r.data;
        }),
        tap((c) => this.cartSubject.next(c))
      );
  }

  clearCart(): Observable<void> {
    return this.http
      .delete(`${environment.apiUrl}/cart`, { observe: 'response' })
      .pipe(
        tap(() => this.cartSubject.next({ items: [], totalPrice: 0 })),
        map(() => undefined)
      );
  }

  refreshFromServer(): Observable<CartDto> {
    return this.getCart();
  }
}
