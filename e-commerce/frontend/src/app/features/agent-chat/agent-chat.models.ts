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
}
