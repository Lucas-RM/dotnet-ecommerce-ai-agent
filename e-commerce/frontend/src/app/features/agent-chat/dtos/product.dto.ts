import { PagedResult, ProductDto } from '../../../core/models/api.models';

/**
 * DTOs usados pelos cards do agent-chat cujo `dataType` deriva de produto.
 *
 * Espelham os tipos do backend de e-commerce (`ProductDto`, `PagedResult<ProductDto>`)
 * já definidos em `core/models/api.models.ts`; apenas reexportamos aqui para que cada
 * card da pasta `cards/` importe seu DTO sem sair do módulo `agent-chat`.
 */
export type { ProductDto };

/** Página de produtos (resultado de `search_products`). `dataType = "PagedProducts"`. */
export type PagedProductsDto = PagedResult<ProductDto>;
