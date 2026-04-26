import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ChatReplyHtmlPipe } from './chat-reply-html.pipe';

/** Dados passados ao <code>MatDialog</code> para aprovação de tool. */
export interface AgentApprovalDialogData {
  approvalMessage: string;
}

/**
 * Modal de confirmação quando <code>ChatResponse.requiresApproval</code> é <code>true</code>.
 * Confirmação envia a próxima rodada com "sim" no fluxo do chat; cancelar envia "não".
 */
@Component({
  standalone: true,
  selector: 'app-approval-dialog',
  imports: [CommonModule, MatButtonModule, MatDialogModule, ChatReplyHtmlPipe],
  templateUrl: './approval-dialog.component.html',
  styleUrls: ['./approval-dialog.component.scss']
})
export class ApprovalDialogComponent {
  private readonly ref = inject(MatDialogRef<ApprovalDialogComponent, boolean>);
  readonly data = inject<AgentApprovalDialogData>(MAT_DIALOG_DATA);

  confirm(): void {
    this.ref.close(true);
  }

  cancel(): void {
    this.ref.close(false);
  }
}
