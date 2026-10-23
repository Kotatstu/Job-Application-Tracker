# Job Application Tracker

The originally idea is to have an app that (auto) keep track of every job application I have applied for. Without depend on the hiring posted on the website, this system catch the email confirmation everytime an application is sent.

On the first phase, there a fully working backend, frontend, that let user to manually create a new job application record. On the seccond phase, I have implemented a workflow that catch google cloud gmail API, feed it to a local LLM to classify and write it into a given scheme. Ultimately "auto" keeping track of every job application (That got send to gmail).

This is a full-stack project that I also use as a chance to learn software architect and new tech stacks. The LLM workflow doesn't have a UI yet. The project is still underdevelopment.

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
- [x] Gmail integration — detect application-related emails
