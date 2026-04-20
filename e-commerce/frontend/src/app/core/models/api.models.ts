export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly totalPages: number;
}

export interface CartItemDto {
  readonly productId: string;
  readonly productName: string;
  readonly unitPrice: number;
  readonly quantity: number;
  readonly subtotal: number;
}

export interface CartDto {
  readonly items: readonly CartItemDto[];
  readonly totalPrice: number;
}

export interface AddCartItemDto {
  readonly productId: string;
  readonly quantity: number;
}

export interface UpdateCartItemDto {
  readonly quantity: number;
}

export interface OrderItemDto {
  readonly productId: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly subtotal: number;
}

export interface OrderDto {
  readonly id: string;
  readonly placedAt: string;
  readonly status: string;
  readonly totalAmount: number;
  readonly items: readonly OrderItemDto[];
}

export interface OrderSummaryDto {
  readonly id: string;
  readonly placedAt: string;
  readonly status: string;
  readonly totalAmount: number;
}

export interface OrderQueryParams {
  readonly page?: number;
  readonly pageSize?: number;
}

export interface ProductDto {
  readonly id: string;
  readonly name: string;
  readonly description: string;
  readonly price: number;
  readonly stockQuantity: number;
  readonly category: string;
  readonly isActive: boolean;
}

export interface CreateProductDto {
  readonly name: string;
  readonly description: string;
  readonly price: number;
  readonly stockQuantity: number;
  readonly category: string;
}

export interface ProductQueryParams {
  readonly page?: number;
  readonly pageSize?: number;
  readonly category?: string | null;
  readonly search?: string | null;
}

/** Alinhado a `OrderStatus` no backend (ex.: 0 = Placed). */
export interface AdminOrderQueryParams {
  readonly page?: number;
  readonly pageSize?: number;
  readonly userId?: string | null;
  readonly status?: number | null;
}

export interface AdminUserQueryParams {
  readonly page?: number;
  readonly pageSize?: number;
}
