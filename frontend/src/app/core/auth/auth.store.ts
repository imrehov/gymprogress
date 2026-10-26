import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AuthUser,
  LoginCredentials,
  LoginResponse,
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly http = inject(HttpClient);

  readonly currentUser = signal<AuthUser | null>(null);
  readonly accessToken = signal<string | null>(null);
  readonly isLoading = signal(false);

  readonly isAuthenticated = computed(
    () => this.currentUser() !== null,
  );

  async login(credentials: LoginCredentials): Promise<void> {
    this.isLoading.set(true);

    try {
      const response = await firstValueFrom(
        this.http.post<LoginResponse>(
          '/api/v1/auth/login',
          credentials,
          { withCredentials: true },
        ),
      );

      this.accessToken.set(response.accessToken);
      this.currentUser.set(response.user);
    } finally {
      this.isLoading.set(false);
    }
  }
}