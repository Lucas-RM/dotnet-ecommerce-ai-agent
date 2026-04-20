import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

const passwordsMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const p = group.get('password')?.value as string | undefined;
  const c = group.get('confirmPassword')?.value as string | undefined;
  if (!p || !c) return null;
  return p === c ? null : { mismatch: true };
};

@Component({
  standalone: true,
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSnackBarModule],
  template: `
    <mat-card class="card">
      <mat-card-title>Criar conta</mat-card-title>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline" class="full">
            <mat-label>Nome</mat-label>
            <input matInput formControlName="name" />
            @if (form.controls.name.invalid && form.controls.name.touched) {
              <mat-error>Obrigatório</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            @if (form.controls.email.invalid && form.controls.email.touched) {
              <mat-error>Informe um email válido</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Senha</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="new-password" />
            @if (form.controls.password.invalid && form.controls.password.touched) {
              <mat-error>Mínimo 6 caracteres</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Confirmar senha</mat-label>
            <input matInput type="password" formControlName="confirmPassword" autocomplete="new-password" />
            @if (form.hasError('mismatch') && form.controls.confirmPassword.touched) {
              <mat-error>As senhas não conferem</mat-error>
            }
          </mat-form-field>
          <div class="actions">
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || loading">
              {{ loading ? 'Enviando…' : 'Cadastrar' }}
            </button>
            <a mat-button routerLink="/login">Já tenho conta</a>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      .card {
        max-width: 420px;
        margin: 24px auto;
        padding: 8px;
      }
      .full {
        width: 100%;
        display: block;
      }
      .actions {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
        align-items: center;
      }
    `
  ]
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  loading = false;

  readonly form = this.fb.nonNullable.group(
    {
      name: ['', [Validators.required, Validators.maxLength(120)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: passwordsMatchValidator }
  );

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const v = this.form.getRawValue();
    this.auth
      .register({
        name: v.name.trim(),
        email: v.email.trim(),
        password: v.password,
        confirmPassword: v.confirmPassword
      })
      .subscribe({
        next: () => {
          this.loading = false;
          this.snack.open('Conta criada. Faça login.', 'Fechar', { duration: 4000 });
          void this.router.navigate(['/login']);
        },
        error: (err: unknown) => {
          this.loading = false;
          this.snack.open(getApiErrorMessage(err, 'Não foi possível cadastrar.'), 'Fechar', { duration: 5000 });
        }
      });
  }
}
