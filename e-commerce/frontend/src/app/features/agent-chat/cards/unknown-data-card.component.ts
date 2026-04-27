import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-unknown-data-card',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card">
      <div class="title">Novo tipo de resposta recebido</div>
      <p class="description">
        Ainda não há card específico para <strong>{{ dataType || 'desconhecido' }}</strong>.
        Mostrando payload bruto em modo seguro.
      </p>
      @if (hasPayload) {
        <pre class="payload">{{ payload | json }}</pre>
      }
      @if (hasDetails) {
        <div class="meta-title">details</div>
        <pre class="payload">{{ details | json }}</pre>
      }
      @if (hasMetadata) {
        <div class="meta-title">metadata</div>
        <pre class="payload">{{ metadata | json }}</pre>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .card {
        border: 1px solid rgba(255, 152, 0, 0.35);
        border-radius: 10px;
        background: #fffaf3;
        padding: 10px 12px;
      }
      .title {
        font-size: 0.84rem;
        font-weight: 700;
        color: #b26a00;
      }
      .description {
        margin: 6px 0 10px;
        font-size: 0.8rem;
        color: rgba(0, 0, 0, 0.68);
      }
      .meta-title {
        margin-top: 8px;
        font-size: 0.75rem;
        font-weight: 600;
        color: #8a5300;
      }
      .payload {
        margin: 0;
        margin-top: 6px;
        padding: 8px;
        max-height: 220px;
        overflow: auto;
        border-radius: 8px;
        background: rgba(0, 0, 0, 0.04);
        font-size: 0.75rem;
        line-height: 1.25;
        white-space: pre-wrap;
        word-break: break-word;
      }
    `
  ]
})
export class UnknownDataCardComponent {
  @Input() dataType: string | null = null;
  @Input() data: unknown = null;
  @Input() details: Record<string, unknown> | null = null;
  @Input() metadata: Record<string, unknown> | null = null;

  get payload(): unknown {
    return this.data;
  }

  get hasPayload(): boolean {
    return this.data !== null && this.data !== undefined;
  }

  get hasDetails(): boolean {
    return !!this.details && Object.keys(this.details).length > 0;
  }

  get hasMetadata(): boolean {
    return !!this.metadata && Object.keys(this.metadata).length > 0;
  }
}
