import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CartService } from '../../core/services/cart.service';
import { CartDto } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-cart',
  imports: [CommonModule, RouterLink, MatButtonModule, MatSnackBarModule],
  template: `
    <div class="wrap">
      <h1>Carrinho</h1>
      @if (!cart || cart.items.length === 0) {
        <p>Seu carrinho está vazio.</p>
        <a mat-button color="primary" routerLink="/catalog">Continuar comprando</a>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Produto</th>
              <th>Preço</th>
              <th>Qtd</th>
              <th>Subtotal</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of cart.items; track item.productId) {
              <tr>
                <td>{{ item.productName }}</td>
                <td>{{ item.unitPrice | number : '1.2-2' }}</td>
                <td class="qty">
                  <button mat-stroked-button type="button" (click)="changeQty(item.productId, item.quantity - 1)" [disabled]="item.quantity <= 1">−</button>
                  <span class="qty-num">{{ item.quantity }}</span>
                  <button mat-stroked-button type="button" (click)="changeQty(item.productId, item.quantity + 1)">+</button>
                </td>
                <td>{{ item.subtotal | number : '1.2-2' }}</td>
                <td>
                  <button mat-button color="warn" type="button" (click)="remove(item.productId)">Remover</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
        <p class="total"><strong>Total: {{ cart.totalPrice | number : '1.2-2' }}</strong></p>
        <div class="actions">
          <a mat-stroked-button routerLink="/catalog">Continuar comprando</a>
          <a mat-flat-button color="primary" routerLink="/checkout">Finalizar compra</a>
          <button mat-button type="button" (click)="clear()">Limpar carrinho</button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .wrap {
        max-width: 900px;
        margin: 0 auto;
        padding: 16px;
      }
      .table {
        width: 100%;
        border-collapse: collapse;
      }
      .table th,
      .table td {
        border-bottom: 1px solid #eee;
        padding: 8px;
        text-align: left;
      }
      .total {
        margin-top: 16px;
      }
      .actions {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;
        margin-top: 16px;
      }
      .qty {
        display: flex;
        align-items: center;
        gap: 8px;
      }
      .qty-num {
        min-width: 24px;
        text-align: center;
      }
    `
  ]
})
export class CartComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly snack = inject(MatSnackBar);

  cart: CartDto | null = null;

  ngOnInit(): void {
    this.cartService.getCart().subscribe({
      next: (c) => (this.cart = c),
      error: () => (this.cart = { items: [], totalPrice: 0 })
    });
  }

  changeQty(productId: string, qty: number): void {
    if (qty < 1) return;
    this.cartService.updateItem(productId, { quantity: qty }).subscribe({
      next: (c) => (this.cart = c),
      error: (err: unknown) => this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 })
    });
  }

  remove(productId: string): void {
    this.cartService.removeItem(productId).subscribe({
      next: (c) => (this.cart = c),
      error: (err: unknown) => this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 })
    });
  }

  clear(): void {
    this.cartService.clearCart().subscribe({
      next: () => {
        this.cart = { items: [], totalPrice: 0 };
        this.snack.open('Carrinho limpo', 'Fechar', { duration: 2500 });
      },
      error: (err: unknown) =>
        this.snack.open(getApiErrorMessage(err, 'Não foi possível limpar o carrinho.'), 'Fechar', { duration: 3000 })
    });
  }
}
