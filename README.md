# Job Application Tracker

A full-stack web app for tracking job applications - company, role,
status history, and the original job posting, all in one place. Built
as a learning project to practice a clean, layered ASP.NET Core
backend paired with a modern React frontend, with real authentication
rather than a stubbed-out login.

A planned second phase will scan a connected Gmail inbox for
application-related emails and stage detected updates for manual
review, instead of writing to your data automatically.

## Features

- **Full CRUD** for job applications and companies, with duplicate
  company detection (case-insensitive, normalized) so the same
  company never gets created twice for one user
- **Status history as a real event log** — every status change is
  recorded with a timestamp, not just overwritten, so you can see the
  full timeline of an application (Applied → Phone Screen →
  Interviewing → ...) rather than just its current state
- **Job posting storage** — paste the full posting text against an
  application so it's preserved even after the original listing gets
  taken down
- **Real authentication** — ASP.NET Core Identity, JWT access +
  refresh token pair, httpOnly cookies, automatic silent token refresh
  on the frontend
- **Company-aware search** — a searchable company picker when logging
  a new application, with the option to add a new company inline

## Tech stack

**Backend**

- ASP.NET Core (.NET 10) Web API
- Entity Framework Core + SQL Server
- ASP.NET Core Identity
- JWT bearer authentication (httpOnly cookies, access/refresh rotation)

**Frontend**

- React + TypeScript (Vite)
- TanStack Query for data fetching and cache invalidation
- React Router
- Tailwind CSS + shadcn/ui
- Sonner (toast notifications), Lucide (icons)

## Architecture

The backend follows a layered structure — **Controller → Service →
EF Core** — with a strict rule enforced throughout the build: a
controller never touches `DbContext` directly, and a service never
knows it's being called over HTTP. Business rules (duplicate
detection, status transitions, ownership checks) live entirely in the
service layer, so they'd work identically if called from a background
job instead of a web request.

Every protected endpoint reads the current user from JWT claims via a
shared `ApiControllerBase`, not from a client-supplied value — no
endpoint trusts the caller to say who they are.

## Getting started

**Backend**

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

Requires a local SQL Server instance and a `DefaultConnection` string
in `appsettings.Development.json`.

**Frontend**

```bash
cd frontend
npm install
npm run dev
```

The dev server proxies `/api` requests to the backend — see
`vite.config.ts`.

## Roadmap

- [x] Core CRUD
- [x] Authentication (Identity, JWT, refresh rotation)
- [x] Frontend (all core pages, shadcn/ui design pass)
- [ ] Gmail integration — detect application-related emails
- [ ] Review queue for email-detected updates
