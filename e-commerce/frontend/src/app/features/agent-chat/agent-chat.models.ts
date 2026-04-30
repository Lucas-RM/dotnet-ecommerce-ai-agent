import { ChatDataType } from './chat-contract';

/** Corpo de POST /api/agent/chat (alinhado ao backend). */
export interface ChatRequest {
  agentId?: string;
  sessionId: string;
  message: string;
  clientVersion?: string;
  locale?: string;
  channel?: string;
  metadata?: Record<string, unknown>;
  correlationId?: string;
}

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
  details?: Record<string, unknown> | null;
  metadata?: Record<string, unknown> | null;
  requiresApproval: boolean;
  approvalId?: string | null;
  /** Provedor ativo no Agent (`openai` | `google`), quando o backend expõe. */
  llmProvider?: string | null;
  agentId?: string | null;
  correlationId?: string | null;
  contractVersion?: string | null;
}

export interface ApprovalDecisionRequest {
  sessionId: string;
  decision: string;
  correlationId?: string;
}

export interface AgentDescriptor {
  id: string;
  displayName: string;
  description?: string | null;
  provider?: string | null;
  model?: string | null;
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
  /** Payload opcional para extensões futuras sem quebrar histórico. */
  details?: Record<string, unknown> | null;
  /** Metadados opcionais de observabilidade/contrato. */
  metadata?: Record<string, unknown> | null;
  requiresApproval?: boolean;
}
