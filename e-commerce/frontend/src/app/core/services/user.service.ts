import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, UserDto } from '../models/auth.models';

/**
 * Perfil do usuário autenticado (`GET /users/me`).
 * Listagens administrativas ficam em `AdminService`.
 */
@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getMe(): Observable<UserDto> {
    return this.http.get<ApiResponse<UserDto>>(`${environment.apiUrl}/users/me`).pipe(
      map((r) => {
        if (!r.data) {
          throw new Error(r.message ?? 'Usuário não encontrado');
        }
        return r.data;
      })
    );
  }
}
