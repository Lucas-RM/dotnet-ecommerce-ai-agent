import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { UserDto } from '../../core/models/auth.models';
import { UserService } from '../../core/services/user.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  standalone: true,
  selector: 'app-profile',
  imports: [CommonModule, MatButtonModule, MatSnackBarModule],
  template: `
    <div class="wrap">
      <h1>Meu perfil</h1>
      @if (loading) {
        <p>Carregando…</p>
      } @else if (error) {
        <p class="err">{{ error }}</p>
      } @else if (user) {
        <dl class="grid">
          <dt>Nome</dt>
          <dd>{{ user.name }}</dd>
          <dt>Email</dt>
          <dd>{{ user.email }}</dd>
          <dt>Papel</dt>
          <dd>{{ user.role }}</dd>
          <dt>Cadastro</dt>
          <dd>{{ user.createdAt | date : 'medium' }}</dd>
          <dt>Ativo</dt>
          <dd>{{ user.isActive ? 'Sim' : 'Não' }}</dd>
        </dl>
        <button mat-stroked-button type="button" (click)="refresh()">Atualizar dados</button>
      }
    </div>
  `,
  styles: [
    `
      .wrap {
        max-width: 520px;
        margin: 0 auto;
      }
      .grid {
        display: grid;
        grid-template-columns: 140px 1fr;
        gap: 8px 16px;
        margin: 16px 0;
      }
      dt {
        font-weight: 600;
        color: #555;
      }
      dd {
        margin: 0;
      }
      .err {
        color: #b00020;
      }
    `
  ]
})
export class ProfileComponent implements OnInit {
  private readonly users = inject(UserService);
  private readonly snack = inject(MatSnackBar);

  user: UserDto | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.users.getMe().subscribe({
      next: (u) => {
        this.user = u;
        this.loading = false;
      },
      error: (err: unknown) => {
        this.loading = false;
        this.error = getApiErrorMessage(err);
      }
    });
  }

  refresh(): void {
    this.loading = true;
    this.error = null;
    this.users.getMe().subscribe({
      next: (u) => {
        this.user = u;
        this.loading = false;
        this.snack.open('Atualizado', 'Fechar', { duration: 2000 });
      },
      error: (err: unknown) => {
        this.loading = false;
        this.snack.open(getApiErrorMessage(err), 'Fechar', { duration: 4000 });
      }
    });
  }
}
