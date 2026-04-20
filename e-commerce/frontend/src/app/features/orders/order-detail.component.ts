import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { OrderService } from '../../core/services/order.service';
import { OrderDto } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-order-detail',
  imports: [CommonModule, RouterLink, MatButtonModule, MatSnackBarModule],
  template: `
    <div class="wrap">
      <a mat-button routerLink="/orders">← Voltar</a>
      @if (order) {
        <h1>Pedido</h1>
        <p>Data: {{ order.placedAt | date : 'short' }} — Status: {{ order.status }}</p>
        <p><strong>Total: {{ order.totalAmount | number : '1.2-2' }}</strong></p>
        <table class="table">
          <thead>
            <tr>
              <th>Produto</th>
              <th>Qtd</th>
              <th>Preço</th>
              <th>Subtotal</th>
            </tr>
          </thead>
          <tbody>
            @for (item of order.items; track item.productId) {
              <tr>
                <td>{{ item.productName }}</td>
                <td>{{ item.quantity }}</td>
                <td>{{ item.unitPrice | number : '1.2-2' }}</td>
                <td>{{ item.subtotal | number : '1.2-2' }}</td>
              </tr>
            }
          </tbody>
        </table>
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
        margin-top: 16px;
      }
      .table th,
      .table td {
        border-bottom: 1px solid #eee;
        padding: 8px;
        text-align: left;
      }
    `
  ]
})
export class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly orderService = inject(OrderService);
  private readonly snack = inject(MatSnackBar);

  order: OrderDto | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.orderService.getOrderById(id).subscribe({
      next: (o) => (this.order = o),
      error: (err: unknown) => this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 })
    });
  }
}
