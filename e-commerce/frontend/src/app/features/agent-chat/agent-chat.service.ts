import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatRequest, ChatResponse } from './agent-chat.models';

@Injectable({ providedIn: 'root' })
export class AgentChatService {
  private readonly http = inject(HttpClient);
  private readonly chatUrl = `${environment.agentApiUrl}/api/agent/chat`;

  /** Um GUID por instância do serviço (sessão do browser); use `resetSession` no logout. */
  private sessionId = crypto.randomUUID();

  sendMessage(message: string): Observable<ChatResponse> {
    const body: ChatRequest = {
      sessionId: this.sessionId,
      message
    };
    return this.http.post<ChatResponse>(this.chatUrl, body);
  }

  /** Nova sessão de conversa no Agent (memória volátil no servidor). */
  resetSession(): void {
    this.sessionId = crypto.randomUUID();
  }
}
