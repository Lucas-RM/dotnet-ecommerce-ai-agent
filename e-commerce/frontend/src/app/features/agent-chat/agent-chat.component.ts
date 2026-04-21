import { AfterViewInit, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AgentChatService } from './agent-chat.service';
import { AgentChatMessage } from './agent-chat.models';
import { ChatReplyHtmlPipe } from './chat-reply-html.pipe';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

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
    ChatReplyHtmlPipe
  ],
  templateUrl: './agent-chat.component.html',
  styleUrls: ['./agent-chat.component.scss']
})
export class AgentChatComponent implements OnInit, AfterViewInit {
  private readonly agentChat = inject(AgentChatService);
  private readonly snack = inject(MatSnackBar);

  @ViewChild('scrollArea') private readonly scrollArea?: ElementRef<HTMLElement>;

  messages: AgentChatMessage[] = [];
  draft = '';
  sending = false;

  ngOnInit(): void {
    this.messages = this.agentChat.loadPersistedMessages();
  }

  ngAfterViewInit(): void {
    this.queueScrollBottom();
  }

  send(): void {
    const text = this.draft.trim();
    if (!text || this.sending) {
      return;
    }

    this.messages = [...this.messages, { role: 'user', text }];
    this.persistMessages();
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
        this.persistMessages();
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
