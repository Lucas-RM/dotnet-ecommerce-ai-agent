/**
 * Contrato de tipos lógicos de payload aceitos no frontend.
 * Mantém uma fonte única para mapeamento de cards e validação em runtime.
 */
export const KNOWN_CHAT_DATA_TYPES = [
  'PagedProducts',
  'Product',
  'Cart',
  'CartItem',
  'PagedOrders',
  'Order'
] as const;

export type KnownChatDataType = (typeof KNOWN_CHAT_DATA_TYPES)[number];

/**
 * Tipo aberto para permitir evolução de backend sem quebrar parsing em runtime.
 * Valores não conhecidos devem cair no fallback visual.
 */
export type ChatDataType = KnownChatDataType | (string & {});

const KNOWN_TYPES_LOOKUP = new Set<string>(KNOWN_CHAT_DATA_TYPES);

export function isKnownChatDataType(value: unknown): value is KnownChatDataType {
  return typeof value === 'string' && KNOWN_TYPES_LOOKUP.has(value);
}
