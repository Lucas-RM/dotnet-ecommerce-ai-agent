import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * Converte texto do assistente (**, `code`, quebras de linha) em HTML seguro para exibição.
 */
@Pipe({
  name: 'chatReplyHtml',
  standalone: true
})
export class ChatReplyHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(text: string | null | undefined): SafeHtml {
    if (text == null || text === '') {
      return this.sanitizer.bypassSecurityTrustHtml('');
    }

    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');

    let html = escaped.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
    html = html.replace(/\n/g, '<br />');

    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
