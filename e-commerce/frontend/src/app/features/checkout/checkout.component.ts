import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { CartDto } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-checkout',
  imports: [CommonModule, MatButtonModule, MatSnackBarModule],
  template: `
    <div class="wrap">
      <h1>Checkout</h1>
      @if (!cart || cart.items.length === 0) {
        <p>Não há itens para finalizar.</p>
        <button mat-stroked-button type="button" (click)="goCatalog()">Ir ao catálogo</button>
      } @else {
        <p>Confira o resumo e confirme o pedido.</p>
        <ul class="list">
          @for (item of cart.items; track item.productId) {
            <li>{{ item.productName }} × {{ item.quantity }} — {{ item.subtotal | number : '1.2-2' }}</li>
          }
        </ul>
        <p class="total"><strong>Total: {{ cart.totalPrice | number : '1.2-2' }}</strong></p>
        <button mat-flat-button color="primary" type="button" [disabled]="loading" (click)="confirm()">
          {{ loading ? 'Processando…' : 'Confirmar pedido' }}
        </button>
      }
    </div>
  `,
  styles: [
    `
      .wrap {
        max-width: 640px;
        margin: 0 auto;
        padding: 16px;
      }
      .list {
        padding-left: 20px;
      }
      .total {
        margin: 16px 0;
      }
    `
  ]
})
export class CheckoutComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  cart: CartDto | null = null;
  loading = false;

  ngOnInit(): void {
    this.cartService.getCart().subscribe({
      next: (c) => (this.cart = c),
      error: () => (this.cart = { items: [], totalPrice: 0 })
    });
  }

  confirm(): void {
    this.loading = true;
    this.orderService.checkout().subscribe({
      next: (order) => {
        this.loading = false;
        this.snack.open('Pedido realizado com sucesso', 'Fechar', { duration: 3500 });
        void this.router.navigate(['/orders', order.id]);
      },
      error: (err: unknown) => {
        this.loading = false;
        this.snack.open(getApiErrorMessage(err, 'Não foi possível finalizar o pedido.'), 'Fechar', { duration: 5000 });
      }
    });
  }

  goCatalog(): void {
    void this.router.navigate(['/catalog']);
  }
}
