import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AgentChatMessage,
  AgentDescriptor,
  ApprovalDecisionRequest,
  ChatRequest,
  ChatResponse,
  ChatToolInfo
} from './agent-chat.models';

const SESSION_ID_STORAGE_KEY = 'ecommerce.agentChat.sessionId';
const MESSAGES_STORAGE_KEY = 'ecommerce.agentChat.messages';
const SELECTED_AGENT_STORAGE_KEY = 'ecommerce.agentChat.selectedAgentId';
const AGENT_CHANNEL = 'web';
const AGENT_CLIENT_VERSION = 'frontend-angular';
const DEFAULT_AGENT_ID = 'support-general';

@Injectable({ providedIn: 'root' })
export class AgentChatService {
  private readonly http = inject(HttpClient);
  private readonly legacyAgentUrl = `${environment.agentApiUrl}/api/agent/chat`;
  private readonly agentsUrl = `${environment.agentApiUrl}/api/agents`;
  private readonly clearSessionUrl = `${environment.agentApiUrl}/api/agent/chat/session/clear`;

  /** Mesmo GUID após F5 (sessionStorage); `resetSession` no logout ou nova conversa. */
  private sessionId = this.loadOrCreateSessionId();
  private selectedAgentId = this.loadSelectedAgentId();

  /**
   * Liberta histórico e aprovação pendente no Agent para a sessão atual, depois gera um novo
   * `sessionId` local (equivalente a “nova conversa” no UI).
   */
  newConversation(): Observable<void> {
    const body = { sessionId: this.sessionId };
    return this.http.post<void>(this.clearSessionUrl, body).pipe(
      finalize(() => this.resetSession())
    );
  }

  getSelectedAgentId(): string {
    return this.selectedAgentId;
  }

  setSelectedAgentId(agentId: string): void {
    const next = (agentId ?? '').trim();
    this.selectedAgentId = next.length > 0 ? next : DEFAULT_AGENT_ID;
    try {
      sessionStorage.setItem(SELECTED_AGENT_STORAGE_KEY, this.selectedAgentId);
    } catch {
      /* ignore */
    }
  }

  listAgents(): Observable<AgentDescriptor[]> {
    return this.http.get<unknown[]>(this.agentsUrl).pipe(
      map((items) => normalizeAgents(items)),
      map((items) => items.length > 0 ? items : fallbackAgents()),
      catchError(() => of(fallbackAgents()))
    );
  }

  sendMessage(message: string): Observable<ChatResponse> {
    const correlationId = this.createCorrelationId();
    const body: ChatRequest = {
      agentId: this.selectedAgentId,
      sessionId: this.sessionId,
      message,
      clientVersion: AGENT_CLIENT_VERSION,
      locale: this.resolveLocale(),
      channel: AGENT_CHANNEL,
      metadata: {
        route: 'agent-chat-widget'
      },
      correlationId
    };
    const multiAgentUrl = `${this.agentsUrl}/${encodeURIComponent(this.selectedAgentId)}/chat`;
    return this.http.post<ChatResponse>(multiAgentUrl, body).pipe(
      catchError(() => this.http.post<ChatResponse>(this.legacyAgentUrl, body))
    );
  }

  submitApprovalDecision(decision: string, approvalId?: string): Observable<ChatResponse> {
    const body: ApprovalDecisionRequest = {
      sessionId: this.sessionId,
      decision,
      correlationId: this.createCorrelationId()
    };

    const approvalToken = (approvalId ?? '').trim() || 'pending';
    const explicitApprovalUrl =
      `${this.agentsUrl}/${encodeURIComponent(this.selectedAgentId)}/chat/approvals/${encodeURIComponent(approvalToken)}`;

    return this.http.post<ChatResponse>(explicitApprovalUrl, body);
  }

  /** Mensagens persistidas na aba atual (sessionStorage). */
  loadPersistedMessages(): AgentChatMessage[] {
    try {
      const raw = sessionStorage.getItem(MESSAGES_STORAGE_KEY);
      if (!raw) {
        return [];
      }
      const parsed: unknown = JSON.parse(raw);
      if (!Array.isArray(parsed)) {
        return [];
      }
      return parsed
        .map(normalizePersistedAgentChatMessage)
        .filter((message): message is AgentChatMessage => message !== null);
    } catch {
      return [];
    }
  }

  saveMessages(messages: AgentChatMessage[]): void {
    try {
      sessionStorage.setItem(MESSAGES_STORAGE_KEY, JSON.stringify(messages));
    } catch {
      /* ignore quota / private mode */
    }
  }

  /** Nova sessão no Agent (servidor) e limpa histórico local. */
  resetSession(): void {
    try {
      sessionStorage.removeItem(SESSION_ID_STORAGE_KEY);
      sessionStorage.removeItem(MESSAGES_STORAGE_KEY);
    } catch {
      /* ignore */
    }
    this.sessionId = crypto.randomUUID();
    this.persistSessionId();
  }

  private loadOrCreateSessionId(): string {
    try {
      const existing = sessionStorage.getItem(SESSION_ID_STORAGE_KEY);
      if (existing) {
        return existing;
      }
    } catch {
      /* ignore */
    }
    const id = crypto.randomUUID();
    try {
      sessionStorage.setItem(SESSION_ID_STORAGE_KEY, id);
    } catch {
      /* ignore */
    }
    return id;
  }

  private persistSessionId(): void {
    try {
      sessionStorage.setItem(SESSION_ID_STORAGE_KEY, this.sessionId);
    } catch {
      /* ignore */
    }
  }

  private loadSelectedAgentId(): string {
    try {
      const value = (sessionStorage.getItem(SELECTED_AGENT_STORAGE_KEY) ?? '').trim();
      if (value.length > 0) {
        return value;
      }
    } catch {
      /* ignore */
    }
    return DEFAULT_AGENT_ID;
  }

  private resolveLocale(): string | undefined {
    if (typeof navigator === 'undefined') {
      return undefined;
    }

    const locale = (navigator.language ?? '').trim();
    return locale.length > 0 ? locale : undefined;
  }

  private createCorrelationId(): string | undefined {
    if (typeof crypto === 'undefined' || typeof crypto.randomUUID !== 'function') {
      return undefined;
    }

    return crypto.randomUUID();
  }
}

function normalizePersistedAgentChatMessage(x: unknown): AgentChatMessage | null {
  if (x === null || typeof x !== 'object') {
    return null;
  }

  const o = x as Record<string, unknown>;
  const role = o['role'];
  if (role !== 'user' && role !== 'assistant') {
    return null;
  }

  // Usuário: sempre texto cru (entrada do compositor).
  // Assistente: novo contrato (intro/outro/card). `text` segue aceito apenas para
  // desserializar mensagens antigas persistidas antes da migração.
  if (role === 'user') {
    if (typeof o['text'] !== 'string') {
      return null;
    }

    return {
      role: 'user',
      text: o['text']
    };
  }

  const intro = asNullableString(o['introMessage']);
  const outro = asNullableString(o['outroMessage']);
  const legacyText = asNullableString(o['text']);
  const tool = normalizeToolInfo(o['tool']);
  const data = o['data'] ?? null;
  const details = asRecordOrNull(o['details']);
  const metadata = asRecordOrNull(o['metadata']);
  const requiresApproval = typeof o['requiresApproval'] === 'boolean'
    ? o['requiresApproval']
    : undefined;

  const hasAssistantContent = intro !== undefined
    || outro !== undefined
    || tool !== undefined
    || o['data'] !== undefined
    || legacyText !== undefined
    || details !== undefined
    || metadata !== undefined;

  if (!hasAssistantContent) {
    return null;
  }

  return {
    role: 'assistant',
    introMessage: intro ?? legacyText ?? null,
    outroMessage: outro ?? null,
    tool: tool ?? null,
    data,
    details: details ?? null,
    metadata: metadata ?? null,
    requiresApproval
  };
}

function normalizeToolInfo(value: unknown): ChatToolInfo | undefined {
  if (!value || typeof value !== 'object') {
    return undefined;
  }

  const obj = value as Record<string, unknown>;
  if (typeof obj['name'] !== 'string') {
    return undefined;
  }

  const rawType = obj['dataType'];
  if (rawType !== null && rawType !== undefined && typeof rawType !== 'string') {
    return undefined;
  }

  return {
    name: obj['name'],
    dataType: (rawType as string | null | undefined) ?? null
  };
}

function asNullableString(value: unknown): string | null | undefined {
  if (value === null) {
    return null;
  }
  if (value === undefined) {
    return undefined;
  }
  return typeof value === 'string' ? value : undefined;
}

function asRecordOrNull(value: unknown): Record<string, unknown> | null | undefined {
  if (value === undefined) {
    return undefined;
  }
  if (value === null) {
    return null;
  }
  if (typeof value !== 'object' || Array.isArray(value)) {
    return undefined;
  }

  return value as Record<string, unknown>;
}

function normalizeAgents(items: unknown[]): AgentDescriptor[] {
  return items
    .map((item) => normalizeAgent(item))
    .filter((item): item is AgentDescriptor => item !== null);
}

function normalizeAgent(value: unknown): AgentDescriptor | null {
  if (!value || typeof value !== 'object') {
    return null;
  }

  const item = value as Record<string, unknown>;
  const id = typeof item['id'] === 'string' ? item['id'].trim() : '';
  if (id.length === 0) {
    return null;
  }

  const displayName = typeof item['displayName'] === 'string' && item['displayName'].trim().length > 0
    ? item['displayName'].trim()
    : id;

  return {
    id,
    displayName,
    description: typeof item['description'] === 'string' ? item['description'] : null,
    provider: typeof item['provider'] === 'string' ? item['provider'] : null,
    model: typeof item['model'] === 'string' ? item['model'] : null
  };
}

function fallbackAgents(): AgentDescriptor[] {
  return [
    { id: 'support-general', displayName: 'Atendimento Geral', description: 'Agente padrão de suporte.' },
    { id: 'sales-assistant', displayName: 'Assistente de Vendas', description: 'Catálogo e carrinho.' },
    { id: 'order-tracker', displayName: 'Acompanhamento de Pedidos', description: 'Status e pedidos.' }
  ];
}
