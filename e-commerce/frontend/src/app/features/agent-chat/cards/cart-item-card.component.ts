import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartItemDto } from '../dtos';

/**
 * Card renderizado para `dataType = "CartItem"` (resultado de `add_cart_item`).
 *
 * Confirma de forma compacta o item recém-adicionado ao carrinho (nome,
 * quantidade, preço unitário e subtotal). Não exibe o carrinho inteiro —
 * para isso, o usuário pede explicitamente `"meu carrinho"` e o card de
 * `"Cart"` é usado.
 */
@Component({
  standalone: true,
  selector: 'app-cart-item-card',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data) {
      <div class="card">
        <div class="icon" aria-hidden="true">🛒</div>
        <div class="body">
          <div class="row row--main">
            <span class="name">{{ data.productName }}</span>
            <span class="qty-chip">× {{ data.quantity }}</span>
          </div>
          <div class="row row--meta">
            <span class="unit">{{ data.unitPrice | number : '1.2-2' }} / un</span>
            <span class="subtotal">
              Subtotal: <strong>{{ data.subtotal | number : '1.2-2' }}</strong>
            </span>
          </div>
        </div>
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .card {
        display: flex;
        align-items: center;
        gap: 12px;
        border: 1px solid rgba(46, 125, 50, 0.22);
        border-left: 3px solid #2e7d32;
        border-radius: 10px;
        background: linear-gradient(90deg, rgba(46, 125, 50, 0.06) 0%, #fafcff 60%);
        padding: 10px 14px;
      }
      .icon {
        font-size: 1.4rem;
        line-height: 1;
      }
      .body {
        flex: 1;
        min-width: 0;
      }
      .row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
      }
      .row--main {
        margin-bottom: 2px;
      }
      .name {
        font-weight: 600;
        color: #0d47a1;
        font-size: 0.95rem;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .qty-chip {
        flex-shrink: 0;
        font-size: 0.78rem;
        padding: 2px 8px;
        border-radius: 999px;
        background: rgba(25, 118, 210, 0.1);
        color: #1565c0;
        font-weight: 600;
      }
      .row--meta {
        font-size: 0.82rem;
        color: rgba(0, 0, 0, 0.6);
        font-variant-numeric: tabular-nums;
      }
      .subtotal strong {
        color: #1b5e20;
      }
    `
  ]
})
export class CartItemCardComponent {
  @Input() data: CartItemDto | null = null;
}
