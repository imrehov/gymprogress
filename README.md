# GymProgress

A self-hosted, mobile-first workout tracking and progressive overload analytics platform.

This project is built intentionally as a learning codebase for modern .NET, Angular, and full-stack software architecture. It intentionally applies robust enterprise patterns (Vertical Slice Architecture, CQRS with MediatR, secure cookie-based token rotation, and comprehensive integration testing) to explore how these decisions behave in a real-world, self-hosted deployment.

This project is a recreation of an old one, with more architectural knowledge:
Old version: https://github.com/imrehov/gymprogress-deploy & https://github.com/imrehov/gymprogress-api

---

## Key Goals & Design Principles

* **Domain-First Progressive Overload Tracking**: Workouts, ordered exercises, and sets with strict user data scoping, optional bodyweight tracking, and effective-load calculations for weighted and bodyweight movements.
* **Vertical Slice Architecture (VSA)**: Backend features live in cohesive slices (`Features/{Feature}/`) containing endpoints, requests, handlers, and validators, rather than split across arbitrary technical layers.
* **Security & Auth Boundaries**: Short-lived JWT access tokens stored exclusively in frontend memory paired with rotating, family-tracked refresh tokens stored as SHA-256 hashes and delivered via `HttpOnly`, `SameSite=Strict` cookies.
* **Modern Reactive Frontend**: Angular with standalone components, Signals, typed reactive forms, and functional interceptors and guards.
* **Self-Hostable Simplicity**: Lightweight single-host deployment using Docker Compose, SQLite with EF Core migrations, and Caddy v2 reverse proxy.

---

## Tech Stack

| Layer | Technologies |
|---|---|
| **Backend** | .NET 10 / C# 13, ASP.NET Core Minimal APIs, MediatR, FluentValidation, Entity Framework Core |
| **Database** | SQLite with EF Core migrations |
| **Authentication** | ASP.NET Identity Core, JWT Bearer tokens, rotating refresh tokens with lockout protection |
| **Frontend** | Angular 19+ (Standalone components, Signals, Typed Reactive Forms, Angular Material) |
| **Infrastructure** | Docker Compose, Caddy v2 (replaces Nginx, static file server & reverse proxy), Cloudflare Tunnels |
| **Testing** | NUnit, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), integration tests with isolated SQLite instances |

---

## Repository Structure

```text
gymprogress/
├── backend/
│   ├── api/                     # ASP.NET Core API
│   │   ├── domain/              # Entities: AppUser, RefreshToken, Workout, Set...
│   │   ├── features/            # Vertical slices (Auth, Workouts, Exercises, Analytics)
│   │   ├── shared/              # DbContext, pipeline behaviors, auth services
│   │   └── Program.cs
│   └── tests/                   # NUnit integration & unit tests
├── frontend/                    # Angular standalone SPA
│   ├── src/
│   │   └── app/                 # Features, core interceptors, guards
│   └── package.json
│
└── README.md
```

---

## Quickstart

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* [Node.js](https://nodejs.org/) (v20+) & npm
* SQLite3

### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd backend/api
   ```

2. Run database migrations:
   ```bash
   dotnet ef database update
   ```

3. Run the API:
   ```bash
   dotnet run
   ```
   The API will listen at `http://localhost:5000` (or configured HTTPS port). OpenAPI UI is available at `/openapi/v1.json` during development.

### Running Tests

Run the full backend test suite (unit and integration tests against isolated in-memory/temp SQLite databases):

```bash
cd backend
dotnet test
```

### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the Angular dev server:
   ```bash
   npm start
   ```
   Navigate to `http://localhost:4200/`. API requests are proxied via `proxy.conf.json`.

---

## Core Domain Rules

* **Exercise Ordering**: `WorkoutExercise.ExerciseOrder` and `WorkoutSet.SetOrder` are explicitly tracked and persisted so order survives edits and deletions without relying on implicit database sorting.
* **Effective Load**:
  * External weight exercises: `Effective Load = WorkoutSet.Load`
  * Bodyweight exercises: `Effective Load = Workout.BodyWeight + WorkoutSet.Load` (omitted from bodyweight progress analytics if workout bodyweight was not logged).
* **Cross-User Isolation**: Every read and mutation is strictly scoped to the authenticated `UserId` (including sub-resources like exercise sets).

---

