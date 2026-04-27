import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrderDto } from '../dtos';

/**
 * Card renderizado para `dataType = "Order"` (resultado de `get_order` e `checkout`).
 *
 * Mostra o cabeçalho do pedido (id curto, data, status, total) e a tabela de itens
 * com produto, quantidade, preço unitário e subtotal.
 */
@Component({
  standalone: true,
  selector: 'app-order-card',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data) {
      <div class="card card--wide">
        <header class="card-head">
          <div class="head-left">
            <span class="order-id" [attr.title]="data.id">#{{ data.id }}</span>
            <span class="order-date">{{ data.placedAt | date : 'short' }}</span>
          </div>
          <div class="head-right">
            <span class="status-pill">{{ data.status }}</span>
            <span class="order-total">{{ data.totalAmount | number : '1.2-2' }}</span>
          </div>
        </header>

        @if (data.items.length > 0) {
          <table class="card-table">
            <thead>
              <tr>
                <th>Produto</th>
                <th class="num">Qtd</th>
                <th class="num">Unitário</th>
                <th class="num">Subtotal</th>
              </tr>
            </thead>
            <tbody>
              @for (it of data.items; track it.productId) {
                <tr>
                  <td class="prod-name">{{ it.productName }}</td>
                  <td class="num">{{ it.quantity }}</td>
                  <td class="num">{{ it.unitPrice | number : '1.2-2' }}</td>
                  <td class="num">{{ it.subtotal | number : '1.2-2' }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .card {
        border: 1px solid rgba(25, 118, 210, 0.18);
        border-radius: 10px;
        background: #fafcff;
        overflow: hidden;
      }
      .card-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
        padding: 10px 14px;
        background: linear-gradient(90deg, rgba(25, 118, 210, 0.06) 0%, #fff 100%);
        border-bottom: 1px solid rgba(0, 0, 0, 0.05);
        flex-wrap: wrap;
      }
      .head-left,
      .head-right {
        display: flex;
        align-items: center;
        gap: 10px;
      }
      .order-id {
        font-family: 'Consolas', 'Segoe UI Mono', ui-monospace, monospace;
        font-weight: 700;
        font-size: 0.82rem;
        color: #0d47a1;
        word-break: break-all;
      }
      .order-date {
        font-size: 0.82rem;
        color: rgba(0, 0, 0, 0.55);
      }
      .status-pill {
        font-size: 0.72rem;
        padding: 2px 8px;
        border-radius: 999px;
        background: rgba(25, 118, 210, 0.1);
        color: #1565c0;
        font-weight: 600;
      }
      .order-total {
        font-weight: 700;
        color: #0d47a1;
        font-variant-numeric: tabular-nums;
      }
      .card-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.88rem;
      }
      .card-table th,
      .card-table td {
        padding: 8px 10px;
        text-align: left;
        border-bottom: 1px solid rgba(0, 0, 0, 0.06);
      }
      .card-table th {
        font-weight: 600;
        color: #1565c0;
        background: rgba(25, 118, 210, 0.05);
      }
      .card-table tbody tr:last-child td {
        border-bottom: none;
      }
      .num {
        text-align: right;
        font-variant-numeric: tabular-nums;
      }
      .prod-name {
        font-weight: 500;
        color: #0d47a1;
      }
    `
  ]
})
export class OrderCardComponent {
  @Input() data: OrderDto | null = null;
}
