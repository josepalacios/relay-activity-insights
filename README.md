# Relay Activity Insights — DASH-247 scaffold

Minimal local scaffolding for the DASH-247 activity insights milestone.

## Prerequisites

- .NET 10 SDK
- Node.js 22+ and npm
- Docker Compose with its engine running

## Initialize the local database

The migration is the source of the PostgreSQL schema. `schema.sql` and `seed.sql` remain reference inputs and are not edited. The initializer runs `Database.Migrate()`, switches its PostgreSQL session to UTC, then loads the repository's unchanged `seed.sql` in one transaction only when both tables are empty. It recognizes the complete expected seed (20 accounts and 12,626 events) on later runs. A partial or unexpected database is left untouched and causes initialization to stop for manual inspection.

Start PostgreSQL, run initialization, then start the app:

```sh
docker compose up -d postgres
docker compose run --rm api --initialize-database
docker compose up --build -d api frontend
```

For a directly run API, start PostgreSQL with the connection string in `backend/Relay.Api/appsettings.json`, then run from the repository root:

```sh
dotnet run --project backend/Relay.Api -- --initialize-database
dotnet run --project backend/Relay.Api
```

The initializer applies migrations before seeding. To apply schema migrations without invoking seed loading:

```sh
dotnet ef database update --project backend/Relay.Api/Relay.Api.csproj
```

The container copies `seed.sql` beside the published API. The local initializer also finds the root `seed.sql`. Timestamp literals in that file have no explicit offset; the initializer sets the PostgreSQL session timezone to UTC before executing them, preserving the file's stated UTC meaning when inserted into `timestamptz` columns. Application `DateTime` values written to these columns must use `DateTimeKind.Utc` (Npgsql rejects local/unspecified `DateTime` for `timestamptz`).

`GET http://localhost:8080/api/accounts` returns the seeded account list with `id`, `name`, and `timeZone`. The health check is at <http://localhost:8080/health>; Vite is at <http://localhost:5173>.

## Weekly activity summary

Request a complete, account-local Monday–Sunday week (the date is the Monday):

```sh
curl "http://localhost:8080/api/accounts/1/activity-summary?weekStart=2026-07-20"
```

The optional `weekStart` defaults to the latest complete week before the fixed demo reference date `2026-07-28`. Dates must be ISO `yyyy-MM-dd`, Mondays, no later than that latest complete week, and inside the full-week range supported by the observed seed timestamps. The endpoint returns 400 for invalid or unsupported weeks and 404 for an unknown account. It computes local week boundaries with the account IANA timezone, converts both to UTC, and queries a half-open interval. Counts include event types `call_received`, `lead_created`, and `appointment_set`; historical locations are retained with zero counts in the selected week.

The response includes `summary`, four `baselineWeeks` with nullable counts when outside observed dataset extent, `coverage`, and per-location totals and baseline totals. `baselineAverage` is the arithmetic mean only when all four preceding weeks are within observed dataset extent; `percentageChange` is null when coverage is incomplete or the average is zero. For the supplied seed, Summit Auto Group (`accountId=1`) on `2026-07-20` has selected total 53 and baseline totals `[50, 43, 62, 39]` (average 48.5). Quiet Harbor Spa (`accountId=20`) returns zero observed events and no locations for that covered week.

Coverage is inferred from the global minimum and maximum event timestamps in the database because the schema contains no ingestion or account-specific coverage metadata. This indicates whether the week lies within the supplied dataset's observed time span; it does not prove that any source was operational or delivered all events. No empty period is described as real-world zero activity beyond that observed extent.

## Verify

The API test uses an already initialized PostgreSQL test database. Set `RELAY_TEST_CONNECTION` to that database connection string to enable it; otherwise xUnit reports it as skipped. It checks the real endpoint and the 20-row result. To verify both seed totals directly inside Compose:

```sh
docker compose exec postgres psql -U relay -d relay -c "SELECT (SELECT COUNT(*) FROM accounts) AS accounts, (SELECT COUNT(*) FROM activity_events) AS activity_events;"
```

Expected: `20` accounts and `12626` activity events, including Quiet Harbor Spa (which has no events). Backend checks:

```sh
dotnet restore tests/Relay.Api.Tests/Relay.Api.Tests.csproj
dotnet build tests/Relay.Api.Tests/Relay.Api.Tests.csproj
dotnet test tests/Relay.Api.Tests/Relay.Api.Tests.csproj
```

## Reset local data

Initialization is repeatable; rerun `docker compose run --rm api --initialize-database` to verify an already seeded database. For a genuinely fresh local database, the following command permanently deletes this Compose project's named PostgreSQL volume and all data in it. Confirm that the volume contains only disposable local data and preserve anything needed before running it:

```sh
docker compose down -v
docker compose up -d postgres
docker compose run --rm api --initialize-database
docker compose up --build -d api frontend
```

No database or volume is deleted by the application initializer.

## Frontend dashboard

Start the local stack after initializing the database as described above:

```sh
docker compose up --build -d api frontend
```

Open <http://localhost:5173>. The dashboard loads accounts from `/api/accounts`, then requests the selected account's activity summary. It defaults to the fixed demo reporting week of July 20–26, 2026. Account and week filters are stored in the URL as `?account=1&week=2026-07-20`, so refreshing or sharing a link restores the same filters. The week selector offers complete weeks through the demo reference date; unsupported weeks are reported from the API.

For development outside Compose, run the API on `http://localhost:8080` and start Vite:

```sh
cd frontend
npm ci
npm run dev
```

The Vite dev server proxies `/api` requests to `http://localhost:8080`. The frontend production build/type check is:

```sh
cd frontend
npm run build
```
### Frontend automated tests

The frontend includes automated tests using Vitest, React Testing Library, and jsdom.

Run the tests from the repository root:

```sh
cd frontend
npm ci
npm test
```

The six automated tests cover:

- Restoring account and reporting-week filters from URL parameters.
- Updating URL parameters when filters change.
- Rendering weekly totals and four-week historical data.
- Handling accounts with no observed activity.
- Suppressing misleading comparisons when baseline coverage is insufficient.
- Displaying API errors with a retry action.

These tests mock the API boundary and complement the PostgreSQL-backed backend tests.

### Manual dashboard verification

After starting the application with Docker Compose:

1. Open http://localhost:5173.
2. Select Summit Auto Group and the week starting 2026-07-20.
3. Confirm the total is 53 and the four baseline totals are 50, 43, 62, and 39.
4. Change the account and reporting week.
5. Refresh the browser and verify the URL filters are preserved.
6. Select Quiet Harbor Spa and verify the empty-account state.
7. Verify that API errors display an appropriate error message.

Backend test commands are documented in the Verify section above.

## Milestone scope

This milestone provides PostgreSQL persistence, local initialization, account retrieval, weekly account/location activity aggregation, and the responsive frontend dashboard. The account ID is demo context only, not an authorization boundary. Authentication/authorization, forecasting, anomaly detection, and production deployment are outside this milestone.
