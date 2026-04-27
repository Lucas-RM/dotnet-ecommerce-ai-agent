import { Type } from '@angular/core';
import { KnownChatDataType } from '../chat-contract';
import { ProductsCardComponent } from './products-card.component';
import { ProductCardComponent } from './product-card.component';
import { CartCardComponent } from './cart-card.component';
import { CartItemCardComponent } from './cart-item-card.component';
import { OrdersCardComponent } from './orders-card.component';
import { OrderCardComponent } from './order-card.component';

/**
 * Mapa `ChatDataType` → componente standalone responsável por renderizar o payload.
 *
 * Ponto de extensão do frontend: adicionar uma tool nova no agente significa
 * apenas (1) criar um DTO em `../dtos/`, (2) criar um `XxxCardComponent` em
 * `./` e (3) registrar a entrada aqui. Nenhum outro arquivo do chat precisa
 * mudar — o `ChatDataCardComponent` fará o dispatch via `ngComponentOutlet`.
 *
 * O tipo `Record<KnownChatDataType, Type<unknown>>` garante em tempo de compilação
 * que todo `ChatDataType` suportado tenha um componente correspondente.
 */
export const CHAT_CARD_REGISTRY: Record<KnownChatDataType, Type<unknown>> = {
  PagedProducts: ProductsCardComponent,
  Product: ProductCardComponent,
  Cart: CartCardComponent,
  CartItem: CartItemCardComponent,
  PagedOrders: OrdersCardComponent,
  Order: OrderCardComponent
};
