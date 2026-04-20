import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, finalize, map, of, shareReplay, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, AuthResponseDto, LoginDto, RegisterDto, User, UserDto } from '../models/auth.models';

const ACCESS_TOKEN_STORAGE_KEY = 'ecommerce.accessToken';

/**
 * Autenticação: access token em memória, espelhado em `sessionStorage` para sobreviver a F5.
 * Renovação preferencial via cookie HttpOnly (`/auth/refresh`); se falhar, restaura o token salvo.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUserSubject = new BehaviorSubject<User | null>(null);
  private accessToken: string | null = null;
  private refreshInFlight$: Observable<string> | null = null;

  readonly currentUser$ = this.currentUserSubject.asObservable();

  /** Tenta renovar o access token com o cookie de refresh e carregar o perfil (`/users/me`). */
  bootstrapSession(): Observable<boolean> {
    return this.refreshToken().pipe(
      switchMap(() => this.loadCurrentUser()),
      map(() => true),
      catchError(() => this.tryRestoreSessionFromStorage())
    );
  }

  login(dto: LoginDto): Observable<void> {
    return this.http.post<ApiResponse<AuthResponseDto>>(`${environment.apiUrl}/auth/login`, dto).pipe(
      map((r) => {
        if (!r.data?.accessToken) {
          throw new Error(r.message ?? 'Login inválido');
        }
        return r.data;
      }),
      tap((auth) => {
        this.setAccessToken(auth.accessToken);
      }),
      switchMap(() => this.loadCurrentUser()),
      map(() => undefined)
    );
  }

  register(dto: RegisterDto): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${environment.apiUrl}/auth/register`, dto).pipe(map(() => undefined));
  }

  /**
   * Renova o access token; requisições concorrentes compartilham a mesma chamada HTTP.
   */
  refreshToken(): Observable<string> {
    if (!this.refreshInFlight$) {
      this.refreshInFlight$ = this.http.post<ApiResponse<AuthResponseDto>>(`${environment.apiUrl}/auth/refresh`, {}).pipe(
        map((r) => {
          if (!r.data?.accessToken) {
            throw new Error(r.message ?? 'Sessão expirada');
          }
          return r.data.accessToken;
        }),
        tap((token) => {
          this.setAccessToken(token);
        }),
        finalize(() => {
          this.refreshInFlight$ = null;
        }),
        shareReplay({ bufferSize: 1, refCount: true })
      );
    }
    return this.refreshInFlight$!;
  }

  /** Carrega usuário atual após já existir access token. */
  loadCurrentUser(): Observable<User> {
    return this.http.get<ApiResponse<UserDto>>(`${environment.apiUrl}/users/me`).pipe(
      map((r) => {
        if (!r.data) {
          throw new Error(r.message ?? 'Usuário não encontrado');
        }
        return this.mapUserDto(r.data);
      }),
      tap((u) => this.currentUserSubject.next(u))
    );
  }

  revoke(): Observable<void> {
    return this.http.post(`${environment.apiUrl}/auth/revoke`, {}, { observe: 'response' }).pipe(map(() => undefined));
  }

  logout(): void {
    if (this.accessToken) {
      this.revoke().subscribe({ error: () => undefined });
    }
    this.clearLocalSession();
  }

  /** Limpa memória (e estado de usuário) sem chamar API; usado após falha de refresh. */
  clearLocalSession(): void {
    this.setAccessToken(null);
    this.currentUserSubject.next(null);
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  isAuthenticated(): boolean {
    return this.accessToken !== null;
  }

  hasRole(role: string): boolean {
    return this.currentUserSubject.value?.role === role;
  }

  isCustomer(): boolean {
    return this.hasRole('Customer');
  }

  /** Para guards: usuário autenticado após tentativa de bootstrap implícita no carregamento da app. */
  ensureAuthenticated(): boolean {
    return this.isAuthenticated();
  }

  private mapUserDto(d: UserDto): User {
    const role: User['role'] = d.role === 'Admin' ? 'Admin' : 'Customer';
    return { id: d.id, name: d.name, email: d.email, role };
  }

  private setAccessToken(token: string | null): void {
    this.accessToken = token;
    try {
      if (token) {
        sessionStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token);
      } else {
        sessionStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY);
      }
    } catch {
      /* ignore quota / private mode */
    }
  }

  private readStoredAccessToken(): string | null {
    try {
      return sessionStorage.getItem(ACCESS_TOKEN_STORAGE_KEY);
    } catch {
      return null;
    }
  }

  /** Quando o refresh por cookie falha (ex.: dev entre origens), reutiliza o token da aba. */
  private tryRestoreSessionFromStorage(): Observable<boolean> {
    const stored = this.readStoredAccessToken();
    if (!stored) {
      this.clearLocalSession();
      return of(false);
    }
    this.setAccessToken(stored);
    return this.loadCurrentUser().pipe(
      map(() => true),
      catchError(() => {
        this.clearLocalSession();
        return of(false);
      })
    );
  }
}
