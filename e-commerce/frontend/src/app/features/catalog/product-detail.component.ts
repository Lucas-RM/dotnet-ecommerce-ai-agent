import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { ProductService } from '../../core/services/product.service';
import { ProductDto } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-product-detail',
  imports: [CommonModule, RouterLink, MatButtonModule, MatSnackBarModule],
  template: `
    <div class="wrap">
      <a mat-button routerLink="/catalog">← Voltar ao catálogo</a>
      @if (loading) {
        <p>Carregando…</p>
      } @else if (error) {
        <p class="err">{{ error }}</p>
      } @else if (product) {
        <h1>{{ product.name }}</h1>
        <p class="muted">{{ product.category }}</p>
        <p class="price">{{ product.price | number : '1.2-2' }}</p>
        <p class="stock">Estoque: {{ product.stockQuantity }}</p>
        <p class="desc">{{ product.description }}</p>
        @if (auth.isCustomer()) {
          <button mat-flat-button color="primary" type="button" (click)="add()" [disabled]="!product.isActive || product.stockQuantity <= 0">
            Adicionar ao carrinho
          </button>
        } @else if (!auth.isAuthenticated()) {
          <a mat-button routerLink="/login">Entre para comprar</a>
        } @else {
          <p class="hint">Conta administrativa não utiliza carrinho.</p>
        }
      }
    </div>
  `,
  styles: [
    `
      .wrap {
        max-width: 720px;
        margin: 0 auto;
      }
      .muted {
        color: #666;
      }
      .price {
        font-size: 1.4rem;
        font-weight: 600;
      }
      .stock {
        font-size: 0.9rem;
      }
      .desc {
        line-height: 1.5;
      }
      .err {
        color: #b00020;
      }
      .hint {
        color: #666;
      }
    `
  ]
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly products = inject(ProductService);
  private readonly cart = inject(CartService);
  readonly auth = inject(AuthService);
  private readonly snack = inject(MatSnackBar);

  product: ProductDto | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'Produto inválido';
      return;
    }
    this.loading = true;
    this.products.getById(id).subscribe({
      next: (p) => {
        this.product = p;
        this.loading = false;
      },
      error: (err: unknown) => {
        this.loading = false;
        this.error = getApiErrorMessage(err);
      }
    });
  }

  add(): void {
    if (!this.product) return;
    this.cart.addItem({ productId: this.product.id, quantity: 1 }).subscribe({
      next: () => this.snack.open('Adicionado ao carrinho', 'Fechar', { duration: 2500 }),
      error: (err: unknown) => this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 })
    });
  }
}
