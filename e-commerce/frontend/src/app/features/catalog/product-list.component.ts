import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { ProductService } from '../../core/services/product.service';
import { PagedResult, ProductDto } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-product-list',
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule
  ],
  template: `
    <div class="wrap">
      <h1>Catálogo</h1>
      <form [formGroup]="filterForm" class="filters" (ngSubmit)="applyFilters()">
        <mat-form-field appearance="outline">
          <mat-label>Busca</mat-label>
          <input matInput formControlName="search" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Categoria</mat-label>
          <input matInput formControlName="category" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit">Filtrar</button>
      </form>

      @if (loading) {
        <p>Carregando…</p>
      } @else if (error) {
        <p class="err">{{ error }}</p>
      } @else {
        <div class="grid">
          @for (p of items; track p.id) {
            <article class="card">
              <h2>
                <a [routerLink]="['/catalog', p.id]">{{ p.name }}</a>
              </h2>
              <p class="muted">{{ p.category }}</p>
              <p class="price">{{ p.price | number : '1.2-2' }}</p>
              <p class="desc">{{ p.description }}</p>
              @if (auth.isCustomer()) {
                <button mat-stroked-button type="button" (click)="add(p)" [disabled]="!p.isActive || p.stockQuantity <= 0">
                  Adicionar ao carrinho
                </button>
              } @else if (!auth.isAuthenticated()) {
                <a mat-button routerLink="/login">Entre para comprar</a>
              } @else {
                <p class="hint">Conta administrativa não utiliza carrinho.</p>
              }
            </article>
          }
        </div>
        <div class="pager">
          <button mat-stroked-button type="button" [disabled]="page <= 1" (click)="prev()">Anterior</button>
          <span>Página {{ page }} de {{ totalPages }}</span>
          <button mat-stroked-button type="button" [disabled]="page >= totalPages" (click)="next()">Próxima</button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .wrap {
        padding: 8px 0;
      }
      .filters {
        display: flex;
        flex-wrap: wrap;
        gap: 12px;
        align-items: center;
        margin-bottom: 16px;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 16px;
      }
      .card {
        border: 1px solid #e0e0e0;
        border-radius: 8px;
        padding: 12px;
      }
      .card h2 {
        margin: 0 0 8px;
        font-size: 1.1rem;
      }
      .card h2 a {
        color: inherit;
        text-decoration: none;
      }
      .muted {
        color: #666;
        font-size: 0.85rem;
        margin: 0;
      }
      .price {
        font-weight: 600;
        margin: 8px 0;
      }
      .desc {
        font-size: 0.9rem;
        color: #444;
        min-height: 3em;
      }
      .pager {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-top: 20px;
      }
      .err {
        color: #b00020;
      }
      .hint {
        font-size: 0.85rem;
        color: #666;
      }
    `
  ]
})
export class ProductListComponent implements OnInit {
  private readonly products = inject(ProductService);
  private readonly cart = inject(CartService);
  readonly auth = inject(AuthService);
  private readonly snack = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  items: ProductDto[] = [];
  page = 1;
  readonly pageSize = 8;
  totalPages = 1;
  loading = false;
  error: string | null = null;

  search = '';
  category: string | null = null;

  readonly filterForm = this.fb.nonNullable.group({
    search: [''],
    category: ['']
  });

  ngOnInit(): void {
    this.load();
  }

  applyFilters(): void {
    const v = this.filterForm.getRawValue();
    this.search = v.search.trim();
    const c = v.category.trim();
    this.category = c.length > 0 ? c : null;
    this.page = 1;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.products
      .getPaged({
        page: this.page,
        pageSize: this.pageSize,
        search: this.search || null,
        category: this.category
      })
      .subscribe({
        next: (res: PagedResult<ProductDto>) => {
          this.items = [...res.items];
          this.totalPages = res.totalPages || 1;
          this.loading = false;
        },
        error: (err: unknown) => {
          this.loading = false;
          this.error = getApiErrorMessage(err);
        }
      });
  }

  prev(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.load();
  }

  next(): void {
    if (this.page >= this.totalPages) return;
    this.page += 1;
    this.load();
  }

  add(p: ProductDto): void {
    this.cart.addItem({ productId: p.id, quantity: 1 }).subscribe({
      next: () => this.snack.open('Adicionado ao carrinho', 'Fechar', { duration: 2500 }),
      error: (err: unknown) => this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 })
    });
  }
}
