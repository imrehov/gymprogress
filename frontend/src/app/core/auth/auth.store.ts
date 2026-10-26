import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthUser, LoginCredentials, LoginResponse, RefreshResponse } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly http = inject(HttpClient);

  readonly currentUser = signal<AuthUser | null>(null);
  readonly accessToken = signal<string | null>(null);
  readonly isLoading = signal(false);

  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  private clearSession(): void {
    this.accessToken.set(null);
    this.currentUser.set(null);
  }

  async login(credentials: LoginCredentials): Promise<void> {
    this.isLoading.set(true);

    try {
      const response = await firstValueFrom(
        this.http.post<LoginResponse>('/api/v1/auth/login', credentials, { withCredentials: true }),
      );

      this.accessToken.set(response.accessToken);
      this.currentUser.set(response.user);
    } finally {
      this.isLoading.set(false);
    }
  }

  async refreshToken(): Promise<void> {
    this.isLoading.set(true);

      try {
        const refresh = await firstValueFrom(
          this.http.post<RefreshResponse>(
            '/api/v1/auth/refresh',
            null,
            //tells browser to send the refresh cookie, angular cannot read it
            { withCredentials: true },
          ),
        );

        const user = await firstValueFrom(
          this.http.get<AuthUser>(
            '/api/v1/auth/me',
            {
              headers: {
                Authorization: `Bearer ${refresh.accessToken}`,
              },
            },
          ),
        );

        //token is only put into the store after /me succeeds
        this.accessToken.set(refresh.accessToken);
        this.currentUser.set(user);
      } catch (error: unknown) {
        this.clearSession();
        throw error;
      } finally {
        this.isLoading.set(false);
      }
  }

  async logout(): Promise<void> {
      this.isLoading.set(true);

      try {
        await firstValueFrom(
          this.http.post<void>(
            '/api/v1/auth/logout',
            null,
            { withCredentials: true },
          ),
        );
        //even if server is unavailable, frontend must forget in-memory access token
      } finally {
        this.clearSession();
        this.isLoading.set(false);
      }
    }
}
