import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { AdminService } from '../../core/services/admin.service';
import { OrderSummaryDto } from '../../core/models/api.models';
import { UserDto } from '../../core/models/auth.models';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-admin',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTabsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule
  ],
  template: `
    <div class="wrap">
      <h1>Administração</h1>
      <mat-tab-group>
        <mat-tab label="Pedidos">
          <div class="panel">
            @if (ordersLoading) {
              <p>Carregando…</p>
            } @else if (orders.length === 0) {
              <p>Nenhum pedido.</p>
            } @else {
              <table class="table">
                <thead>
                  <tr>
                    <th>Data</th>
                    <th>Status</th>
                    <th>Total</th>
                  </tr>
                </thead>
                <tbody>
                  @for (o of orders; track o.id) {
                    <tr>
                      <td>{{ o.placedAt | date : 'short' }}</td>
                      <td>{{ o.status }}</td>
                      <td>{{ o.totalAmount | number : '1.2-2' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
              <div class="pager">
                <button mat-stroked-button type="button" [disabled]="ordersPage <= 1" (click)="ordersPrev()">Anterior</button>
                <span>Página {{ ordersPage }}</span>
                <button mat-stroked-button type="button" [disabled]="!ordersHasMore" (click)="ordersNext()">Próxima</button>
              </div>
            }
          </div>
        </mat-tab>
        <mat-tab label="Usuários">
          <div class="panel">
            @if (usersLoading) {
              <p>Carregando…</p>
            } @else if (users.length === 0) {
              <p>Nenhum usuário.</p>
            } @else {
              <table class="table">
                <thead>
                  <tr>
                    <th>Nome</th>
                    <th>Email</th>
                    <th>Papel</th>
                    <th>Ativo</th>
                  </tr>
                </thead>
                <tbody>
                  @for (u of users; track u.id) {
                    <tr>
                      <td>{{ u.name }}</td>
                      <td>{{ u.email }}</td>
                      <td>{{ u.role }}</td>
                      <td>{{ u.isActive ? 'Sim' : 'Não' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
              <div class="pager">
                <button mat-stroked-button type="button" [disabled]="usersPage <= 1" (click)="usersPrev()">Anterior</button>
                <span>Página {{ usersPage }}</span>
                <button mat-stroked-button type="button" [disabled]="!usersHasMore" (click)="usersNext()">Próxima</button>
              </div>
            }
          </div>
        </mat-tab>
        <mat-tab label="Novo produto">
          <div class="panel">
            <form [formGroup]="productForm" (ngSubmit)="createProduct()">
              <mat-form-field appearance="outline" class="full">
                <mat-label>Nome</mat-label>
                <input matInput formControlName="name" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full">
                <mat-label>Descrição</mat-label>
                <textarea matInput rows="3" formControlName="description"></textarea>
              </mat-form-field>
              <div class="row">
                <mat-form-field appearance="outline">
                  <mat-label>Preço</mat-label>
                  <input matInput type="number" step="0.01" formControlName="price" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Estoque</mat-label>
                  <input matInput type="number" formControlName="stockQuantity" />
                </mat-form-field>
                <mat-form-field appearance="outline" class="grow">
                  <mat-label>Categoria</mat-label>
                  <input matInput formControlName="category" />
                </mat-form-field>
              </div>
              <button mat-flat-button color="primary" type="submit" [disabled]="productForm.invalid || productSaving">
                {{ productSaving ? 'Salvando…' : 'Criar produto' }}
              </button>
            </form>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [
    `
      .wrap {
        padding: 8px 0;
      }
      .panel {
        padding: 16px 0;
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
      .pager {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-top: 12px;
      }
      .full {
        width: 100%;
        display: block;
      }
      .row {
        display: flex;
        flex-wrap: wrap;
        gap: 12px;
        align-items: flex-start;
      }
      .grow {
        flex: 1 1 200px;
      }
    `
  ]
})
export class AdminComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly snack = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  orders: OrderSummaryDto[] = [];
  ordersPage = 1;
  readonly ordersPageSize = 10;
  ordersHasMore = false;
  ordersLoading = false;

  users: UserDto[] = [];
  usersPage = 1;
  readonly usersPageSize = 10;
  usersHasMore = false;
  usersLoading = false;

  productSaving = false;

  readonly productForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    price: [0, [Validators.required, Validators.min(0.01)]],
    stockQuantity: [0, [Validators.required, Validators.min(0)]],
    category: ['', [Validators.required, Validators.maxLength(100)]]
  });

  ngOnInit(): void {
    this.loadOrders();
    this.loadUsers();
  }

  loadOrders(): void {
    this.ordersLoading = true;
    this.admin.getOrders({ page: this.ordersPage, pageSize: this.ordersPageSize }).subscribe({
      next: (page) => {
        this.orders = [...page.items];
        this.ordersHasMore = this.ordersPage < page.totalPages;
        this.ordersLoading = false;
      },
      error: (err: unknown) => {
        this.ordersLoading = false;
        this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 });
      }
    });
  }

  ordersPrev(): void {
    if (this.ordersPage <= 1) return;
    this.ordersPage -= 1;
    this.loadOrders();
  }

  ordersNext(): void {
    if (!this.ordersHasMore) return;
    this.ordersPage += 1;
    this.loadOrders();
  }

  loadUsers(): void {
    this.usersLoading = true;
    this.admin.getUsers({ page: this.usersPage, pageSize: this.usersPageSize }).subscribe({
      next: (page) => {
        this.users = [...page.items];
        this.usersHasMore = this.usersPage < page.totalPages;
        this.usersLoading = false;
      },
      error: (err: unknown) => {
        this.usersLoading = false;
        this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 });
      }
    });
  }

  usersPrev(): void {
    if (this.usersPage <= 1) return;
    this.usersPage -= 1;
    this.loadUsers();
  }

  usersNext(): void {
    if (!this.usersHasMore) return;
    this.usersPage += 1;
    this.loadUsers();
  }

  createProduct(): void {
    if (this.productForm.invalid) return;
    this.productSaving = true;
    const v = this.productForm.getRawValue();
    this.admin
      .createProduct({
        name: v.name.trim(),
        description: v.description.trim(),
        price: Number(v.price),
        stockQuantity: Math.floor(Number(v.stockQuantity)),
        category: v.category.trim()
      })
      .subscribe({
        next: () => {
          this.productSaving = false;
          this.productForm.reset({ name: '', description: '', price: 0, stockQuantity: 0, category: '' });
          this.snack.open('Produto criado', 'Fechar', { duration: 3000 });
        },
        error: (err: unknown) => {
          this.productSaving = false;
          this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 5000 });
        }
      });
  }
}
