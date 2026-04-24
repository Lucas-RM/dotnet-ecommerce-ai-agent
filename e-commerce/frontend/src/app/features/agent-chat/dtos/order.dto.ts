import {
  OrderDto,
  OrderItemDto,
  OrderSummaryDto,
  PagedResult
} from '../../../core/models/api.models';

/**
 * DTOs usados pelos cards do agent-chat cujo `dataType` deriva de pedido.
 *
 * Espelham `OrderDto`, `OrderItemDto`, `OrderSummaryDto` e `PagedResult<OrderSummaryDto>`
 * do backend de e-commerce e são reexportados aqui para isolar o módulo.
 *
 * Mapas `dataType` → DTO:
 * - `"Order"`       → {@link OrderDto}
 * - `"PagedOrders"` → {@link PagedOrdersDto}
 */
export type { OrderDto, OrderItemDto, OrderSummaryDto };

/** Página de pedidos do usuário (resultado de `list_orders`). `dataType = "PagedOrders"`. */
export type PagedOrdersDto = PagedResult<OrderSummaryDto>;
