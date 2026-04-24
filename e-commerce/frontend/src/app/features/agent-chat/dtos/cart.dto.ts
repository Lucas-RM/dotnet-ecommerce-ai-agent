import { CartDto, CartItemDto } from '../../../core/models/api.models';

/**
 * DTOs usados pelos cards do agent-chat cujo `dataType` deriva do carrinho.
 *
 * Espelham `CartDto` / `CartItemDto` do backend de e-commerce
 * (`core/models/api.models.ts`) e são reexportados aqui para isolar o módulo.
 *
 * Mapas `dataType` → DTO:
 * - `"Cart"`     → {@link CartDto}
 * - `"CartItem"` → {@link CartItemDto}
 */
export type { CartDto, CartItemDto };
