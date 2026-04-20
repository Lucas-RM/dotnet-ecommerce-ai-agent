import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { OrderService } from '../../core/services/order.service';
import { OrderSummaryDto } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-order-list',
  imports: [CommonModule, RouterLink, MatButtonModule, MatSnackBarModule],
  template: `
    <div class="wrap">
      <h1>Meus pedidos</h1>
      @if (orders.length === 0) {
        <p>Você ainda não tem pedidos.</p>
        <a mat-button color="primary" routerLink="/catalog">Ver produtos</a>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Status</th>
              <th>Total</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (o of orders; track o.id) {
              <tr>
                <td>{{ o.placedAt | date : 'short' }}</td>
                <td>{{ o.status }}</td>
                <td>{{ o.totalAmount | number : '1.2-2' }}</td>
                <td><a mat-button [routerLink]="['/orders', o.id]">Detalhes</a></td>
              </tr>
            }
          </tbody>
        </table>
        <div class="pager">
          <button mat-stroked-button type="button" [disabled]="page <= 1" (click)="prev()">Anterior</button>
          <span>Página {{ page }}</span>
          <button mat-stroked-button type="button" [disabled]="!hasMore" (click)="next()">Próxima</button>
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
      .pager {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-top: 16px;
      }
    `
  ]
})
export class OrderListComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly snack = inject(MatSnackBar);

  orders: OrderSummaryDto[] = [];
  page = 1;
  readonly pageSize = 10;
  hasMore = false;
  totalPages = 1;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.orderService.getOrders({ page: this.page, pageSize: this.pageSize }).subscribe({
      next: (res) => {
        this.orders = [...res.items];
        this.totalPages = res.totalPages;
        this.hasMore = this.page < this.totalPages;
      },
      error: (err: unknown) => this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 })
    });
  }

  prev(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.load();
  }

  next(): void {
    if (!this.hasMore) return;
    this.page += 1;
    this.load();
  }
}
