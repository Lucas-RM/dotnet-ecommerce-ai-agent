import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AgentChatMessage, ChatRequest, ChatResponse } from './agent-chat.models';

const SESSION_ID_STORAGE_KEY = 'ecommerce.agentChat.sessionId';
const MESSAGES_STORAGE_KEY = 'ecommerce.agentChat.messages';

@Injectable({ providedIn: 'root' })
export class AgentChatService {
  private readonly http = inject(HttpClient);
  private readonly agentUrl = `${environment.agentApiUrl}/api/agent/chat`;

  /** Mesmo GUID após F5 (sessionStorage); `resetSession` no logout ou nova conversa. */
  private sessionId = this.loadOrCreateSessionId();

  sendMessage(message: string): Observable<ChatResponse> {
    const body: ChatRequest = {
      sessionId: this.sessionId,
      message
    };
    return this.http.post<ChatResponse>(this.agentUrl, body);
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
      return parsed.filter(isPersistedAgentChatMessage);
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
}

function isPersistedAgentChatMessage(x: unknown): x is AgentChatMessage {
  if (x === null || typeof x !== 'object') {
    return false;
  }
  const o = x as Record<string, unknown>;
  const role = o['role'];
  const text = o['text'];
  if (role !== 'user' && role !== 'assistant') {
    return false;
  }
  return typeof text === 'string';
}
