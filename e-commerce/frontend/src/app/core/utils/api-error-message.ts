import { HttpErrorResponse } from '@angular/common/http';

type ApiErrorBody = {
  message?: string;
  Message?: string;
  /** Resposta do Agent quando o POST falha com corpo `ChatResponse`. */
  reply?: string;
  errors?: string[] | Record<string, string[]>;
};

function joinErrors(errors: ApiErrorBody['errors']): string | null {
  if (!errors) return null;
  if (Array.isArray(errors)) {
    const parts = errors.map((e) => String(e).trim()).filter(Boolean);
    return parts.length ? parts.join(' ') : null;
  }
  const parts = Object.values(errors)
    .flat()
    .map((e) => String(e).trim())
    .filter(Boolean);
  return parts.length ? parts.join(' ') : null;
}

/** Texto que o Angular coloca em `HttpErrorResponse.message` (inclui URL da requisição). */
function isAngularHttpFailureText(msg: string | undefined): boolean {
  if (!msg) return false;
  return msg.startsWith('Http failure response for http');
}

/**
 * Mensagem amigável para o usuário a partir de falhas HTTP ou `Error` genérico.
 * Prioriza `message` / `errors` do JSON da API (`ApiResponse`) e não exibe a URL da rota.
 */
export function getApiErrorMessage(err: unknown, fallback = 'Não foi possível concluir a operação.'): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error;

    if (body && typeof body === 'object' && !Array.isArray(body)) {
      const b = body as ApiErrorBody;
      const primary = (b.message ?? b.Message ?? b.reply)?.trim();
      const extra = joinErrors(b.errors);
      if (primary && extra) return `${primary} ${extra}`.trim();
      if (primary) return primary;
      if (extra) return extra;
    }

    if (typeof body === 'string') {
      const t = body.trim();
      if (t.startsWith('{')) {
        try {
          const p = JSON.parse(t) as ApiErrorBody;
          const primary = (p.message ?? p.Message ?? p.reply)?.trim();
          const extra = joinErrors(p.errors);
          if (primary && extra) return `${primary} ${extra}`.trim();
          if (primary) return primary;
          if (extra) return extra;
        } catch {
          /* corpo não JSON */
        }
      }
      if (t && !t.includes('<!DOCTYPE') && !/<html[\s>]/i.test(t) && t.length < 600) {
        return t;
      }
    }

    if (err.status === 0) return 'Não foi possível conectar ao servidor.';
    if (err.status === 404) return 'Recurso não encontrado.';
    if (err.status === 401) return 'Não autorizado. Verifique suas credenciais.';
    if (err.status === 403) return 'Você não tem permissão para esta ação.';
    if (err.status === 429) {
      return 'Muitas mensagens no último minuto; aguarde e tente de novo.';
    }
    if (err.status >= 500) return 'Ocorreu um erro no servidor. Tente novamente.';
    return fallback;
  }

  if (err instanceof Error) {
    if (isAngularHttpFailureText(err.message)) {
      return fallback;
    }
    return err.message?.trim() || fallback;
  }

  return fallback;
}
