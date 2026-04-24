import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PagedProductsDto } from '../dtos';

/**
 * Card renderizado para `dataType = "PagedProducts"` (resultado de `search_products`).
 *
 * Exibe uma tabela com nome, categoria, preço e estoque dos produtos encontrados,
 * além de um rodapé informativo com dados de paginação (puramente textual —
 * por decisão do projeto, os cards do chat não oferecem ações interativas).
 */
@Component({
  standalone: true,
  selector: 'app-products-card',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data && data.items.length > 0) {
      <div class="card card--wide">
        <table class="card-table">
          <thead>
            <tr>
              <th>Produto</th>
              <th>Categoria</th>
              <th class="num">Preço</th>
              <th class="num">Estoque</th>
            </tr>
          </thead>
          <tbody>
            @for (p of data.items; track p.id) {
              <tr>
                <td class="prod-name" [attr.title]="p.description">{{ p.name }}</td>
                <td class="prod-cat">{{ p.category }}</td>
                <td class="num">{{ p.price | number : '1.2-2' }}</td>
                <td
                  class="num"
                  [class.low-stock]="p.stockQuantity <= 0"
                >
                  {{ p.stockQuantity }}
                </td>
              </tr>
            }
          </tbody>
        </table>
        <footer class="card-footer">
          Página {{ data.page }} de {{ data.totalPages }}
          · {{ data.items.length }} de {{ data.totalCount }} resultado(s)
        </footer>
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
      .prod-cat {
        color: rgba(0, 0, 0, 0.6);
      }
      .low-stock {
        color: #b71c1c;
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
export class ProductsCardComponent {
  @Input() data: PagedProductsDto | null = null;
}
