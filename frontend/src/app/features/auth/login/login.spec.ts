import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { Login } from './login';

describe('Login Component', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let httpTesting: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    httpTesting = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => httpTesting.verify());

  it('submitting an invalid form marks its controls as touched and sends no request', async () => {
    component.form.setValue({
      email: '',
      password: '',
    });

    await component.submit();

    expect(component.form.controls.email.touched).toBe(true);
    expect(component.form.controls.password.touched).toBe(true);
    httpTesting.expectNone('/api/v1/auth/login');
  });

  it('successful submission sends credentials and navigates to /', async () => {
    component.form.setValue({
      email: 'user@example.com',
      password: 'ValidPass1!',
    });

    const navigation = vi
      .spyOn(router, 'navigateByUrl')
      .mockResolvedValue(true);

    const submission = component.submit();

    const request = httpTesting.expectOne('/api/v1/auth/login');

    expect(request.request.body).toEqual({
      email: 'user@example.com',
      password: 'ValidPass1!',
    });

    request.flush({
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2026-10-26T12:15:00Z',
      user: {
        id: '98c1a48b-f476-49de-9fb3-4cb072ec9a39',
        email: 'user@example.com',
        displayName: 'Test User',
      },
    });

    await submission;

    expect(navigation).toHaveBeenCalledWith('/');
  });

  it('backend 401 displays Invalid email or password.', async () => {
    component.form.setValue({
      email: 'user@example.com',
      password: 'WrongPassword!',
    });

    const submission = component.submit();

    const request = httpTesting.expectOne('/api/v1/auth/login');
    request.flush(null, {
      status: 401,
      statusText: 'Unauthorized',
    });

    await submission;

    expect(component.submitError()).toBe('Invalid email or password.');
  });
});