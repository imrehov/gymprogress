import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';

describe('AuthStore', () => {
  let store: AuthStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthStore, provideHttpClient(), provideHttpClientTesting()],
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

  it('refreshes the access token and restores the current user', async () => {
    const refreshPromise = store.refreshToken();

    expect(store.isLoading()).toBe(true);

    const refreshRequest = httpTesting.expectOne(
      '/api/v1/auth/refresh',
    );

    expect(refreshRequest.request.method).toBe('POST');
    expect(refreshRequest.request.withCredentials).toBe(true);
    expect(refreshRequest.request.body).toBeNull();

    refreshRequest.flush({
      accessToken: 'refreshed-access-token',
      accessTokenExpiresAtUtc: '2026-10-26T12:15:00Z',
    });

    await Promise.resolve();

    const meRequest = httpTesting.expectOne('/api/v1/auth/me');

    expect(meRequest.request.method).toBe('GET');
    expect(
      meRequest.request.headers.get('Authorization'),
    ).toBe('Bearer refreshed-access-token');

    meRequest.flush({
      id: '98c1a48b-f476-49de-9fb3-4cb072ec9a39',
      email: 'user@example.com',
      displayName: 'Test User',
    });

    await refreshPromise;

    expect(store.accessToken()).toBe(
      'refreshed-access-token',
    );
    expect(store.currentUser()?.email).toBe(
      'user@example.com',
    );
    expect(store.isAuthenticated()).toBe(true);
    expect(store.isLoading()).toBe(false);
  });

  it('clears authentication state when refresh fails', async () => {
    store.accessToken.set('old-access-token');
    store.currentUser.set({
      id: '98c1a48b-f476-49de-9fb3-4cb072ec9a39',
      email: 'user@example.com',
      displayName: 'Test User',
    });

    const refreshPromise = store.refreshToken();

    const request = httpTesting.expectOne('/api/v1/auth/refresh');
    request.flush(null, {
      status: 401,
      statusText: 'Unauthorized',
    });

    await expect(refreshPromise).rejects.toBeTruthy();

    expect(store.accessToken()).toBeNull();
    expect(store.currentUser()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
    expect(store.isLoading()).toBe(false);
    httpTesting.expectNone('/api/v1/auth/me');
  });

  it('logs out through the API and clears authentication state', async () => {
    store.accessToken.set('access-token');
    store.currentUser.set({
      id: '98c1a48b-f476-49de-9fb3-4cb072ec9a39',
      email: 'user@example.com',
      displayName: 'Test User',
    });

    const logoutPromise = store.logout();

    expect(store.isLoading()).toBe(true);

    const request = httpTesting.expectOne('/api/v1/auth/logout');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toBeNull();

    request.flush(null);
    await logoutPromise;

    expect(store.accessToken()).toBeNull();
    expect(store.currentUser()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
    expect(store.isLoading()).toBe(false);
  });

  it('clears local authentication state when logout request fails', async () => {
    store.accessToken.set('access-token');
    store.currentUser.set({
      id: '98c1a48b-f476-49de-9fb3-4cb072ec9a39',
      email: 'user@example.com',
      displayName: 'Test User',
    });

    const logoutPromise = store.logout();

    const request = httpTesting.expectOne('/api/v1/auth/logout');
    request.flush(null, {
      status: 500,
      statusText: 'Server Error',
    });

    await expect(logoutPromise).rejects.toBeTruthy();

    expect(store.accessToken()).toBeNull();
    expect(store.currentUser()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
    expect(store.isLoading()).toBe(false);
  });
});
