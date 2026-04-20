export interface LoginDto {
  email: string;
  password: string;
}
export interface RegisterDto {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
}
export interface AuthResponseDto {
  accessToken: string;
  expiresIn: number;
  role: string;
}
/** Perfil retornado por `GET /users/me`. */
export interface UserDto {
  id: string;
  name: string;
  email: string;
  role: string;
  createdAt: string;
  isActive: boolean;
}
export interface User {
  id: string;
  name: string;
  email: string;
  role: 'Customer' | 'Admin';
}
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: string[] | null;
}
