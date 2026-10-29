# Relay Activity Insights — DASH-247 scaffold

Minimal local scaffolding for the DASH-247 activity insights milestone.

## Prerequisites

- .NET 10 SDK
- Node.js 22+ and npm
- Docker Compose

## Run locally

Start the API and PostgreSQL with Compose:

```sh
docker compose up --build
```

The API health check is available at <http://localhost:8080/health>. The React/Vite placeholder is at <http://localhost:5173>.

To run projects directly, start PostgreSQL with the credentials in `backend/Relay.Api/appsettings.json`, then run:

```sh
dotnet run --project backend/Relay.Api
cd frontend && npm install && npm run dev
```

Run backend checks with:

```sh
dotnet restore tests/Relay.Api.Tests/Relay.Api.Tests.csproj
dotnet build tests/Relay.Api.Tests/Relay.Api.Tests.csproj
dotnet test tests/Relay.Api.Tests/Relay.Api.Tests.csproj
```

Compose uses a named PostgreSQL volume. Remove it with `docker compose down -v` to reset the local database. No schema or seed scripts are automatically applied.

## Not implemented yet

This scaffold does not include database entities, migrations, schema or seed loading, reporting endpoints, account selection, dashboard metrics, authentication/authorization, or production deployment configuration. The connection string uses demo local credentials and must be replaced for any shared or deployed environment.
