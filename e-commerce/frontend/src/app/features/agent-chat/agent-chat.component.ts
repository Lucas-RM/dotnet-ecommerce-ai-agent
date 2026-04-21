import { Component, ElementRef, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AgentChatService } from './agent-chat.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

export interface AgentChatMessage {
  role: 'user' | 'assistant';
  text: string;
  requiresApproval?: boolean;
  pendingToolName?: string | null;
}

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
    MatProgressSpinnerModule
  ],
  templateUrl: './agent-chat.component.html',
  styleUrls: ['./agent-chat.component.scss']
})
export class AgentChatComponent {
  private readonly agentChat = inject(AgentChatService);
  private readonly snack = inject(MatSnackBar);

  @ViewChild('scrollArea') private readonly scrollArea?: ElementRef<HTMLElement>;

  messages: AgentChatMessage[] = [];
  draft = '';
  sending = false;

  send(): void {
    const text = this.draft.trim();
    if (!text || this.sending) {
      return;
    }

    this.messages = [...this.messages, { role: 'user', text }];
    this.draft = '';
    this.sending = true;
    this.queueScrollBottom();

    this.agentChat.sendMessage(text).subscribe({
      next: (res) => {
        this.messages = [
          ...this.messages,
          {
            role: 'assistant',
            text: res.reply,
            requiresApproval: res.requiresApproval,
            pendingToolName: res.pendingToolName
          }
        ];
        this.sending = false;
        this.queueScrollBottom();
      },
      error: (err: unknown) => {
        this.sending = false;
        this.snack.open(getApiErrorMessage(err, 'Não foi possível enviar a mensagem.'), 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  onKeydown(ev: KeyboardEvent): void {
    if (ev.key === 'Enter' && !ev.shiftKey) {
      ev.preventDefault();
      this.send();
    }
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
