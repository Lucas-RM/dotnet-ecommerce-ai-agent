import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartDto } from '../dtos';

/**
 * Card renderizado para `dataType = "Cart"` (resultado de `get_cart`,
 * `update_cart_item` e `remove_cart_item`).
 *
 * Lista os itens do carrinho (produto, quantidade, preço unitário e subtotal)
 * e o total do carrinho. Visual apenas — ações (checkout, remoção) ficam a
 * cargo do fluxo conversacional.
 */
@Component({
  standalone: true,
  selector: 'app-cart-card',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data) {
      <div class="card card--wide">
        @if (data.items.length === 0) {
          <p class="empty">Seu carrinho está vazio.</p>
        } @else {
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
            <tfoot>
              <tr>
                <td colspan="3" class="total-label">Total</td>
                <td class="num total-val">{{ data.totalPrice | number : '1.2-2' }}</td>
              </tr>
            </tfoot>
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
      .empty {
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
      .num {
        text-align: right;
        font-variant-numeric: tabular-nums;
      }
      .prod-name {
        font-weight: 500;
        color: #0d47a1;
      }
      tfoot td {
        background: #fff;
        border-top: 1px solid rgba(0, 0, 0, 0.08);
        border-bottom: none;
      }
      .total-label {
        text-align: right;
        font-weight: 600;
        color: #1565c0;
        text-transform: uppercase;
        font-size: 0.8rem;
        letter-spacing: 0.04em;
      }
      .total-val {
        font-weight: 700;
        color: #0d47a1;
      }
    `
  ]
})
export class CartCardComponent {
  @Input() data: CartDto | null = null;
}
