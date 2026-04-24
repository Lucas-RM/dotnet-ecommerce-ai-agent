import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnChanges,
  SimpleChanges,
  Type
} from '@angular/core';
import { CommonModule, NgComponentOutlet } from '@angular/common';
import { ChatDataType } from '../agent-chat.models';
import { CHAT_CARD_REGISTRY } from './card-registry';

/**
 * Dispatcher genérico dos cards do agent-chat.
 *
 * Recebe `dataType` (literal string) e `data` (payload já tipado pelo backend)
 * e delega a renderização ao componente correto via `CHAT_CARD_REGISTRY`.
 * Como todos os cards do registry expõem um `@Input() data`, passamos o mesmo
 * nome via `ngComponentOutletInputs` e cada card faz seu casting interno.
 *
 * Se o `dataType` for desconhecido (ex.: payload antigo persistido em
 * sessionStorage após atualização de backend), o componente não renderiza
 * nada — a UI continua legível via intro/outro.
 */
@Component({
  standalone: true,
  selector: 'app-chat-data-card',
  imports: [CommonModule, NgComponentOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (component) {
      <ng-container
        [ngComponentOutlet]="component"
        [ngComponentOutletInputs]="inputs"
      />
    }
  `
})
export class ChatDataCardComponent implements OnChanges {
  @Input() dataType: ChatDataType | null = null;
  @Input() data: unknown = null;

  component: Type<unknown> | null = null;
  inputs: Record<string, unknown> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if ('dataType' in changes) {
      this.component = this.resolveComponent(this.dataType);
    }
    if ('data' in changes || 'dataType' in changes) {
      this.inputs = { data: this.data };
    }
  }

  private resolveComponent(dataType: ChatDataType | null): Type<unknown> | null {
    if (!dataType) {
      return null;
    }
    return CHAT_CARD_REGISTRY[dataType] ?? null;
  }
}
