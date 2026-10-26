export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  user: AuthUser;
}

export interface RefreshResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
}
