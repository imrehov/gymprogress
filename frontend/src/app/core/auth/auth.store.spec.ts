import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';

describe('AuthStore', () => {
  let store: AuthStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    store = TestBed.inject(AuthStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('successful login updates user, token, authentication, and loading state', async () => {
    const loginPromise = store.login({
      email: 'user@example.com',
      password: 'ValidPass1!',
    });

    expect(store.isLoading()).toBe(true);

    const request = httpTesting.expectOne('/api/v1/auth/login');

    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);

    request.flush({
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2026-10-26T12:15:00Z',
      user: {
        id: '98c1a48b-f476-49de-9fb3-4cb072ec9a39',
        email: 'user@example.com',
        displayName: 'Test User',
      },
    });

    await loginPromise;

    expect(store.accessToken()).toBe('access-token');
    expect(store.currentUser()?.email).toBe('user@example.com');
    expect(store.isAuthenticated()).toBe(true);
    expect(store.isLoading()).toBe(false);
  });

  it('failed login rejects promise, keeps unauthenticated state, and resets loading state', async () => {
    const loginPromise = store.login({
      email: 'user@example.com',
      password: 'WrongPassword!',
    });

    expect(store.isLoading()).toBe(true);

    const request = httpTesting.expectOne('/api/v1/auth/login');

    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);

    request.flush(null, {
      status: 401,
      statusText: 'Unauthorized',
    });

    await expect(loginPromise).rejects.toBeTruthy();

    expect(store.currentUser()).toBeNull();
    expect(store.accessToken()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
    expect(store.isLoading()).toBe(false);
  });
});
