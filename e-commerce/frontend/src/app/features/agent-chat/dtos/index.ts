/**
 * Barrel dos DTOs do agent-chat. Cada `ChatDataType` do `agent-chat.models.ts`
 * mapeia para um tipo deste conjunto e será consumido pelos cards em `../cards/`.
 */
export type { PagedProductsDto, ProductDto } from './product.dto';
export type { CartDto, CartItemDto } from './cart.dto';
export type { OrderDto, OrderItemDto, OrderSummaryDto, PagedOrdersDto } from './order.dto';
