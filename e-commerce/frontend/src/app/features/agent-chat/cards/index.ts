/**
 * Barrel dos cards do agent-chat.
 *
 * O consumidor típico (ex.: `agent-chat.component.ts` no item 17) só importa
 * `ChatDataCardComponent` — os cards específicos são descobertos em runtime
 * via `CHAT_CARD_REGISTRY`. Os demais exports estão aqui para testes e para
 * cenários avançados que queiram renderizar um card diretamente.
 */
export { ChatDataCardComponent } from './chat-data-card.component';
export { CHAT_CARD_REGISTRY } from './card-registry';
export { ProductsCardComponent } from './products-card.component';
export { ProductCardComponent } from './product-card.component';
export { CartCardComponent } from './cart-card.component';
export { CartItemCardComponent } from './cart-item-card.component';
export { OrdersCardComponent } from './orders-card.component';
export { OrderCardComponent } from './order-card.component';
export { UnknownDataCardComponent } from './unknown-data-card.component';
