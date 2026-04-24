import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductDto } from '../dtos';

/**
 * Card renderizado para `dataType = "Product"` (resultado de `get_product`).
 *
 * Mostra detalhes de um único produto: nome, categoria, descrição, preço
 * e estoque. Sem botões de ação — visual apenas, conforme a convenção dos
 * cards do agent-chat.
 */
@Component({
  standalone: true,
  selector: 'app-product-card',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data) {
      <div class="card">
        <header class="card-head">
          <h3 class="card-title">{{ data.name }}</h3>
          <span class="card-chip">{{ data.category }}</span>
        </header>
        @if (data.description) {
          <p class="card-desc">{{ data.description }}</p>
        }
        <dl class="card-kv">
          <div>
            <dt>Preço</dt>
            <dd class="num">{{ data.price | number : '1.2-2' }}</dd>
          </div>
          <div>
            <dt>Estoque</dt>
            <dd class="num" [class.low-stock]="data.stockQuantity <= 0">
              {{ data.stockQuantity }}
            </dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>
              <span
                class="status-pill"
                [class.status-pill--on]="data.isActive"
                [class.status-pill--off]="!data.isActive"
              >
                {{ data.isActive ? 'Ativo' : 'Inativo' }}
              </span>
            </dd>
          </div>
        </dl>
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
        padding: 12px 14px;
      }
      .card-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
        margin-bottom: 8px;
      }
      .card-title {
        margin: 0;
        font-size: 1rem;
        font-weight: 600;
        color: #0d47a1;
      }
      .card-chip {
        font-size: 0.72rem;
        padding: 2px 8px;
        border-radius: 999px;
        background: rgba(25, 118, 210, 0.1);
        color: #1565c0;
        font-weight: 600;
      }
      .card-desc {
        margin: 0 0 10px;
        font-size: 0.88rem;
        color: rgba(0, 0, 0, 0.7);
        line-height: 1.45;
      }
      .card-kv {
        margin: 0;
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 8px 14px;
      }
      .card-kv div {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }
      .card-kv dt {
        font-size: 0.72rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: rgba(0, 0, 0, 0.5);
      }
      .card-kv dd {
        margin: 0;
        font-size: 0.92rem;
        color: #1a237e;
        font-weight: 500;
      }
      .num {
        font-variant-numeric: tabular-nums;
      }
      .low-stock {
        color: #b71c1c;
      }
      .status-pill {
        font-size: 0.72rem;
        padding: 2px 8px;
        border-radius: 999px;
        font-weight: 600;
      }
      .status-pill--on {
        background: rgba(46, 125, 50, 0.1);
        color: #1b5e20;
      }
      .status-pill--off {
        background: rgba(183, 28, 28, 0.1);
        color: #b71c1c;
      }
    `
  ]
})
export class ProductCardComponent {
  @Input() data: ProductDto | null = null;
}
