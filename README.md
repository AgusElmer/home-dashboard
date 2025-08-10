# Home Dashboard

Personal home dashboard with an ASP.NET Core backend and a React (Vite) frontend. The repo includes a Docker/Caddy setup for HTTPS by default in development.

## Features

- Backend: ASP.NET Core Minimal API
- Frontend: React + Vite static build served by ASP.NET
- Database: PostgreSQL (via Docker Compose) or SQLite for non‑compose dev
- HTTPS in dev: Caddy with internal CA, reverse proxy to the app

## Quick Start (Docker)

Prerequisites: Docker (and Compose).

1) Configure secrets in `.env` (these override appsettings):

```
Jwt__Key=<strong-random-key>
Google__ClientId=<your-google-client-id>
Google__ClientSecret=<your-google-client-secret>
```

2) Start everything:

```
docker compose up --build
```

3) Open the app on:

- https://localhost
- Health: https://localhost/__caddy_health (returns “ok”)

## Remote Dev From Another PC

Google OAuth does not allow IPs as origins, use an SSH tunnel to keep the browser origin on localhost.

(PowerShell):

```
ssh -L 8443:localhost:443 <ubuntu-user>@<machine-ip>
```

Then browse on the gaming PC to:

- https://localhost:8443

Google OAuth setup (Google Cloud → APIs & Services → Credentials → your Web client):

- Authorized JavaScript origins:
  - https://localhost
  - https://localhost:8443
- Authorized redirect URIs:
  - https://localhost/auth/google-callback
  - https://localhost:8443/auth/google-callback

## Configuration

- `docker-compose.yml` (dev defaults):
  - App listens on `http://0.0.0.0:8080` (ASPNETCORE_URLS)
  - CORS `FrontUrl`: `https://localhost,https://localhost:8443`
  - Caddy proxies HTTPS → app:8080 and exposes 80/443
- `backend/appsettings*.json`: base config; use `.env` for secrets in dev
- Health endpoint: `/__caddy_health` (served by Caddy)

## Development (without Docker)

If you prefer running locally:

- Backend
  - `cd backend && dotnet restore && dotnet run`
  - SQLite by default (see `ConnectionStrings:Default`)
- Frontend
  - `cd frontend && npm install && npm run build` (static files go to backend `wwwroot` via Docker build) or `npm run dev` during pure frontend dev

## Production

- See `Caddyfile.prod` – set your real domain and let Caddy obtain public certs.
- Set `FrontUrl` to your public origin(s), and provide production secrets via env.

## Troubleshooting

- Ping health: https://localhost/__caddy_health
- 502 from Caddy: app not reachable on `app:8080` – ensure the app container is up and listening (ASPNETCORE_URLS) and check `docker compose logs app`.
- Windows over IP TLS errors: we disable HTTP/3 and set a default SNI in dev; prefer the SSH tunnel for OAuth and stability.

## Project Structure

- `backend/`: ASP.NET Core API, auth endpoints, static file hosting
- `frontend/`: React app (Vite)
- `Dockerfile`: Multi-stage build (frontend + backend)
- `docker-compose.yml`: App, Caddy, PostgreSQL
- `Caddyfile`: Dev config (internal TLS, reverse proxy)
- `Caddyfile.prod`: Example production config (public TLS)
