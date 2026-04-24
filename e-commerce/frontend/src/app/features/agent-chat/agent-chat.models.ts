/** Corpo de POST /api/agent/chat (alinhado ao backend). */
export interface ChatRequest {
  sessionId: string;
  message: string;
}

/** Resposta do Agent ao widget de chat. */
export interface ChatResponse {
  reply: string;
  requiresApproval: boolean;
  pendingToolName: string | null;
  /** Provedor ativo no Agent (`openai` | `google`), quando o backend expõe. */
  llmProvider?: string | null;
}

/** Mensagem exibida no widget (espelhada em sessionStorage até fechar a aba ou logout). */
export interface AgentChatMessage {
  role: 'user' | 'assistant';
  text: string;
  requiresApproval?: boolean;
  pendingToolName?: string | null;
}
