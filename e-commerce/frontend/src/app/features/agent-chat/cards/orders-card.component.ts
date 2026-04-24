import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PagedOrdersDto } from '../dtos';

/**
 * Card renderizado para `dataType = "PagedOrders"` (resultado de `list_orders`).
 *
 * Lista os pedidos do usuário com data, status e total, e mostra informações
 * textuais de paginação. Sem navegação — o chat é o driver de interação.
 */
@Component({
  standalone: true,
  selector: 'app-orders-card',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data && data.items.length > 0) {
      <div class="card card--wide">
        <table class="card-table">
          <thead>
            <tr>
              <th>Pedido</th>
              <th>Data</th>
              <th>Status</th>
              <th class="num">Total</th>
            </tr>
          </thead>
          <tbody>
            @for (o of data.items; track o.id) {
              <tr>
                <td class="order-id" [attr.title]="o.id">{{ shortId(o.id) }}</td>
                <td>{{ o.placedAt | date : 'short' }}</td>
                <td>
                  <span class="status-pill">{{ o.status }}</span>
                </td>
                <td class="num">{{ o.totalAmount | number : '1.2-2' }}</td>
              </tr>
            }
          </tbody>
        </table>
        <footer class="card-footer">
          Página {{ data.page }} de {{ data.totalPages }}
          · {{ data.items.length }} de {{ data.totalCount }} pedido(s)
        </footer>
      </div>
    } @else if (data) {
      <div class="card card--empty">
        <p>Nenhum pedido encontrado.</p>
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
      .card--empty p {
        margin: 0;
        padding: 14px;
        text-align: center;
        color: rgba(0, 0, 0, 0.55);
        font-size: 0.9rem;
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
      .order-id {
        font-family: 'Consolas', 'Segoe UI Mono', ui-monospace, monospace;
        font-size: 0.8rem;
        color: #1565c0;
      }
      .status-pill {
        font-size: 0.72rem;
        padding: 2px 8px;
        border-radius: 999px;
        background: rgba(25, 118, 210, 0.1);
        color: #1565c0;
        font-weight: 600;
      }
      .card-footer {
        padding: 8px 12px;
        font-size: 0.78rem;
        color: rgba(0, 0, 0, 0.55);
        background: #fff;
        border-top: 1px solid rgba(0, 0, 0, 0.05);
      }
    `
  ]
})
export class OrdersCardComponent {
  @Input() data: PagedOrdersDto | null = null;

  /** Exibe apenas o prefixo curto do GUID para caber na coluna sem poluir a tabela. */
  shortId(id: string): string {
    return id?.length > 8 ? `${id.slice(0, 8)}…` : id;
  }
}
