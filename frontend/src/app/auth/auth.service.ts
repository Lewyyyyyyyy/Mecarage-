import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
  RefreshTokenResponse,
  RegisterRequest,
  RegisterResponse,
} from '../core/models/auth.models';
import { tap } from 'rxjs';

interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  garageId?: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly authUrl = `${environment.apiBaseUrl}/auth`;
  private currentUser = signal<User | null>(null);

  isAuthenticated = computed(() => !!this.currentUser());
  user = computed(() => this.currentUser());

  constructor(private readonly http: HttpClient) {
    this.loadInitialUser();
  }

  private loadInitialUser() {
    const token = this.getAccessToken();
    if (token) {
      this.updateUserFromToken(token);
    }
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.authUrl}/login`, payload).pipe(
      tap(response => {
        this.setSession(response.accessToken, response.refreshToken);
      })
    );
  }

  register(payload: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.authUrl}/register`, payload);
  }

  refreshToken(payload: RefreshTokenRequest): Observable<RefreshTokenResponse> {
    return this.http.post<RefreshTokenResponse>(`${this.authUrl}/refresh`, payload);
  }

  setSession(accessToken?: string, refreshToken?: string): void {
    if (accessToken) {
      sessionStorage.setItem('accessToken', accessToken);
      this.updateUserFromToken(accessToken);
    }

    if (refreshToken) {
      sessionStorage.setItem('refreshToken', refreshToken);
    }
  }

  clearSession(): void {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('refreshToken');
    this.currentUser.set(null);
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem('refreshToken');
  }

  changePassword(email: string, currentPassword: string, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.authUrl}/change-password`, {
      email,
      currentPassword,
      newPassword
    });
  }

  private updateUserFromToken(token: string) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.currentUser.set({
        id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload.sub || '',
        firstName: payload.firstName,
        lastName: payload.lastName,
        email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || payload.email,
        role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role,
        garageId: payload.garageId || undefined
      });
    } catch (e) {
      console.error('Failed to decode token', e);
      this.currentUser.set(null);
    }
  }
}
