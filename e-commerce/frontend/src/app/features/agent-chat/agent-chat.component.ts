import { AfterViewInit, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AgentChatService } from './agent-chat.service';
import { AgentChatMessage, AgentDescriptor, ChatResponse } from './agent-chat.models';
import { ChatReplyHtmlPipe } from './chat-reply-html.pipe';
import { ApprovalDialogComponent } from './approval-dialog.component';
import { ChatDataCardComponent } from './cards';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

type LlmProviderLabel = 'openai' | 'google';

@Component({
  standalone: true,
  selector: 'app-agent-chat',
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    ChatReplyHtmlPipe,
    ChatDataCardComponent
  ],
  templateUrl: './agent-chat.component.html',
  styleUrls: ['./agent-chat.component.scss']
})
export class AgentChatComponent implements OnInit, AfterViewInit {
  private readonly agentChat = inject(AgentChatService);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  @ViewChild('scrollArea') private readonly scrollArea?: ElementRef<HTMLElement>;

  messages: AgentChatMessage[] = [];
  draft = '';
  sending = false;
  /** Bloqueia o compositor enquanto o modal de aprovação estiver aberto. */
  approvalOpen = false;
  /** Provedor LLM da última resposta do Agent (cabeçalho do widget). */
  activeLlmProvider: LlmProviderLabel | null = null;
  agents: AgentDescriptor[] = [];
  selectedAgentId = '';

  ngOnInit(): void {
    this.messages = this.agentChat.loadPersistedMessages();
    this.selectedAgentId = this.agentChat.getSelectedAgentId();
    this.agentChat.listAgents().subscribe((items) => {
      this.agents = items;
      if (!this.agents.some((x) => x.id === this.selectedAgentId) && this.agents.length > 0) {
        this.selectedAgentId = this.agents[0].id;
        this.agentChat.setSelectedAgentId(this.selectedAgentId);
      }
    });
  }

  ngAfterViewInit(): void {
    this.queueScrollBottom();
  }

  send(): void {
    const text = this.draft.trim();
    if (!text || this.sending || this.approvalOpen) {
      return;
    }
    this.draft = '';
    this.appendUserAndPostMessage(text);
  }

  onKeydown(ev: KeyboardEvent): void {
    if (ev.key === 'Enter' && !ev.shiftKey) {
      ev.preventDefault();
      this.send();
    }
  }

  startNewConversation(): void {
    if (this.sending) {
      return;
    }
    this.agentChat.newConversation().subscribe({
      next: () => {
        this.messages = [];
        this.activeLlmProvider = null;
        this.queueScrollBottom();
      },
      error: (err: unknown) => {
        this.messages = [];
        this.activeLlmProvider = null;
        this.queueScrollBottom();
        this.snack.open(
          getApiErrorMessage(err, 'A conversa foi reiniciada localmente; o servidor pode ainda reter a sessão anterior.'),
          'Fechar',
          { duration: 6000 }
        );
      }
    });
  }

  onAgentChanged(agentId: string): void {
    if (this.sending || this.approvalOpen) {
      return;
    }

    this.selectedAgentId = (agentId ?? '').trim();
    this.agentChat.setSelectedAgentId(this.selectedAgentId);
    this.startNewConversation();
  }

  private appendUserAndPostMessage(text: string): void {
    this.messages = [...this.messages, { role: 'user', text }];
    this.persistMessages();
    this.sending = true;
    this.queueScrollBottom();
    this.agentChat.sendMessage(text).subscribe({
      next: (res) => this.onResponse(res),
      error: (err: unknown) => {
        this.sending = false;
        this.approvalOpen = false;
        this.snack.open(getApiErrorMessage(err, 'Não foi possível enviar a mensagem.'), 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  private onResponse(res: ChatResponse): void {
    this.updateLlmProviderFrom(res);
    this.messages = [
      ...this.messages,
      {
        role: 'assistant',
        introMessage: res.introMessage,
        outroMessage: res.outroMessage,
        tool: res.tool,
        data: res.data,
        details: res.details ?? null,
        metadata: res.metadata ?? null,
        requiresApproval: res.requiresApproval
      }
    ];
    this.persistMessages();
    this.sending = false;
    if (!res.requiresApproval) {
      this.approvalOpen = false;
    }
    this.queueScrollBottom();

    if (res.requiresApproval) {
      this.openApprovalAndFollowUp(res);
    }
  }

  private updateLlmProviderFrom(res: ChatResponse): void {
    const p = (res.llmProvider ?? '').toLowerCase();
    if (p === 'google' || p === 'gemini') {
      this.activeLlmProvider = 'google';
    } else if (p === 'openai' || p === 'azure' || p === 'azureopenai') {
      this.activeLlmProvider = 'openai';
    }

    const responseAgentId = (res.agentId ?? '').trim();
    if (responseAgentId.length > 0) {
      this.selectedAgentId = responseAgentId;
      this.agentChat.setSelectedAgentId(responseAgentId);
    }
  }

  private openApprovalAndFollowUp(res: ChatResponse): void {
    this.approvalOpen = true;
    const dref = this.dialog.open(ApprovalDialogComponent, {
      data: {
        approvalMessage: res.introMessage ?? ''
      },
      disableClose: false
    });
    dref.afterClosed().subscribe((confirmed) => {
      this.approvalOpen = false;
      const decision = confirmed === true ? 'sim' : 'não';
      this.appendUserAndPostApprovalDecision(decision, this.tryResolveApprovalId(res));
    });
  }

  private appendUserAndPostApprovalDecision(decision: string, approvalId?: string): void {
    this.messages = [...this.messages, { role: 'user', text: decision }];
    this.persistMessages();
    this.sending = true;
    this.queueScrollBottom();

    this.agentChat.submitApprovalDecision(decision, approvalId).subscribe({
      next: (response) => this.onResponse(response),
      error: (err: unknown) => {
        this.sending = false;
        this.approvalOpen = false;
        this.snack.open(getApiErrorMessage(err, 'Não foi possível enviar a decisão de aprovação.'), 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  private tryResolveApprovalId(res: ChatResponse): string | undefined {
    const direct = this.extractString(res.approvalId);
    if (direct) {
      return direct;
    }

    const metadata = res.metadata ?? null;
    if (metadata && typeof metadata === 'object') {
      const value = this.extractString((metadata as Record<string, unknown>)['approvalId']);
      if (value) {
        return value;
      }
    }

    const details = res.details ?? null;
    if (details && typeof details === 'object') {
      return this.extractString((details as Record<string, unknown>)['approvalId']);
    }

    return undefined;
  }

  private extractString(value: unknown): string | undefined {
    if (typeof value !== 'string') {
      return undefined;
    }
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  private persistMessages(): void {
    this.agentChat.saveMessages(this.messages);
  }

  private queueScrollBottom(): void {
    queueMicrotask(() => {
      const el = this.scrollArea?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }
}
