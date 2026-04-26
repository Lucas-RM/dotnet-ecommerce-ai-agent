/** Corpo de POST /api/agent/chat (alinhado ao backend). */
export interface ChatRequest {
  sessionId: string;
  message: string;
}

/**
 * Tipos lógicos de dado reconhecidos pelo registry de cards do chat.
 * Adicionar tool nova = adicionar literal aqui + entrada em `CHAT_CARD_REGISTRY`.
 */
export type ChatDataType =
  | 'PagedProducts'
  | 'Product'
  | 'Cart'
  | 'CartItem'
  | 'PagedOrders'
  | 'Order';

/** Identifica a tool executada na resposta e o `dataType` lógico para escolher o card visual. */
export interface ChatToolInfo {
  readonly name: string;
  readonly dataType: ChatDataType | null;
}

/**
 * Resposta do Agent ao widget de chat — contrato padronizado em três blocos:
 * `introMessage` (texto) → `data` estruturado (renderizado por card) → `outroMessage` (texto).
 *
 * Respostas puramente conversacionais (sem tool) trazem apenas `introMessage`.
 */
export interface ChatResponse {
  introMessage: string | null;
  outroMessage: string | null;
  tool: ChatToolInfo | null;
  data: unknown | null;
  requiresApproval: boolean;
  /** Provedor ativo no Agent (`openai` | `google`), quando o backend expõe. */
  llmProvider?: string | null;
}

/**
 * Mensagem exibida no widget e persistida em sessionStorage.
 *
 * - Usuário: usa `text` (entrada crua do compositor).
 * - Assistente: usa `introMessage` / `outroMessage` / `tool` / `data` conforme o contrato acima.
 */
export interface AgentChatMessage {
  role: 'user' | 'assistant';
  /** Somente para mensagens do usuário. */
  text?: string;
  /** Mensagem introdutória do assistente (antes do card). */
  introMessage?: string | null;
  /** Follow-up do assistente (depois do card). */
  outroMessage?: string | null;
  /** Tool executada nesta resposta e `dataType` do card. */
  tool?: ChatToolInfo | null;
  /** Payload estruturado da tool; cada `dataType` mapeia para um DTO em `./dtos`. */
  data?: unknown | null;
  requiresApproval?: boolean;
}
