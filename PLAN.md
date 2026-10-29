# DASH-247 Implementation Plan

## 1. Problem statement and product interpretation

Relay customers currently see raw activity totals without context. DASH-247 should help an account admin judge recent activity against that account’s own recent history, and help multi-location customers see where activity differs. The MVP will report total events and counts by event type for the selected complete week, show the four preceding complete weeks as the baseline context, and compare locations within the account. It will describe observed counts and changes without claiming statistical normality, forecasting, or causality.

The agreed historical reference date is **2026-07-28 in each account’s timezone**. The displayed reporting week is **Monday July 20 through Sunday July 26, 2026**, with baseline weeks July 13–19, July 6–12, June 29–July 5, and June 22–28.

## 2. Dataset facts and assumptions

### Verified dataset facts

- `schema.sql` defines `accounts` with an IANA timezone and `activity_events` with UTC `occurred_at`, account, location, event type, nullable duration, and nullable outcome.
- `seed.sql` contains 20 account rows and 12,626 event rows. Event timestamps range from 2026-02-01 10:57:44 UTC to 2026-07-27 22:20:34 UTC.
- The event types are `call_received` (7,780), `lead_created` (3,044), and `appointment_set` (1,802).
- There are 19 accounts with event data and one empty account (Quiet Harbor Spa). Populated accounts have between 1 and 15 observed location labels.
- There are 398 NULL outcomes. Duration is nullable and is populated only on some calls.
- Summit Auto Group has six locations and 1,223 events. With the agreed window, its reporting-week total is 53 and preceding weekly totals are 50, 43, 62, and 39.

### Assumptions to confirm during implementation

- PostgreSQL is the source of truth; account IDs and seeded identifiers are stable.
- This challenge’s demo is pinned to the agreed historical reference date. Production behavior should use the current date in the account timezone, injected through a clock abstraction.
- Weeks start Monday and include the full local Sunday. API boundaries are half-open local week intervals converted to UTC.
- A week with no matching events is a zero observed-event count only when the queried time window is covered by the dataset. The seed has no ingestion/coverage metadata, so an empty result cannot prove either zero real-world activity or missing source data. Treat weeks inside the known seed interval as observed zero for this demo; mark dates outside it as unknown/insufficient coverage, not zero.
- Total event count includes all event types. Outcome and duration are not part of the initial comparison.
- Locations are derived from event data; no separate location dimension exists to represent a site that has never emitted an event.
- The challenge expects one account context per dashboard request; authorization and authentication are outside the supplied starter repository. Use the account selector for demo context, and state that it is not an authorization boundary.

## 3. MVP scope and acceptance criteria

### In scope

- Account dashboard with selected account and reporting week.
- Total events and event-type counts for the selected week.
- Four-week historical context, including total and event-type counts per week.
- Location comparison for the reporting week, with baseline context per location where available.
- Explicit empty-account, zero-week, and insufficient-history states.
- URL query parameters for selected account and week; direct links and browser back/forward restore the same selection.
- Docker Compose local development for PostgreSQL, API, and frontend, using the provided seed unchanged.

### Acceptance criteria

1. On the seeded historical demo, Summit Auto Group resolves July 20–26, 2026 in `America/Chicago`; total count is 53 and the four baseline totals are 50, 43, 62, and 39.
2. Counts by event type sum to the total for every returned week and location; account filters prevent cross-account leakage.
3. Event timestamps are bucketed by the account’s IANA timezone, including UTC conversion at local week boundaries.
4. A complete week inside known dataset coverage with no matching events returns zero counts for all supported event types and total. A week outside known coverage is identified as unknown/insufficient coverage, not zero.
5. Quiet Harbor Spa shows an explicit no-activity state. Insufficient history is identified separately from zero activity.
6. A multi-location account shows comparable per-location reporting-week counts and historical context; locations with no observed events in a period have zero counts within known locations.
7. Invalid account/week query parameters produce a clear validation response or safe UI fallback, without silently selecting a different account.
8. The dashboard contains no “normal”, “abnormal”, or predictive classification unless product later defines and validates such a rule.

## 4. Architecture and technical decisions

- **Backend:** .NET 10 ASP.NET Core API with EF Core and Npgsql provider. Keep endpoint, query/service, and persistence responsibilities small; avoid a broad clean-architecture scaffold for this timebox.
- **Data access:** EF Core entities for accounts and events; read-only aggregation query. Use parameterized SQL or provider-supported grouping where timezone conversion is clearest and testable. Avoid loading all events into application memory.
- **Frontend:** React + TypeScript + Vite. A compact dashboard page, URL-backed filter state, typed API client, and explicit loading/error/empty states.
- **Runtime:** Docker Compose for PostgreSQL, API, and Vite frontend, with health/dependency ordering sufficient for local startup.
- **API errors:** consistent validation response for unknown accounts, malformed week, and unsupported period. No arbitrary statistical score.
- **Time source:** inject a reporting clock/reference date rather than calling system time inside query code. Seed demo configuration supplies the agreed 2026-07-28 reference.

## 5. Database migrations and seed loading

- Create an initial EF Core migration for `accounts` and `activity_events`, matching the supplied schema’s primary key, foreign key, nullability, and lengths. Add an index on `(account_id, occurred_at)` and consider `(account_id, location, occurred_at)` only if query plans or the simple MVP query justify it.
- Store `occurred_at` as PostgreSQL `timestamp with time zone` (`timestamptz`) and normalize inputs as UTC. Preserve account timezone as IANA text; do not store a fixed offset in its place.
- Load the provided `seed.sql` as-is after migration in the local demo database. Do not regenerate or alter records. Make initialization repeatable by applying migrations only to a fresh/known local database and avoid running the seed twice against primary keys.
- Document reset behavior in the developer instructions. Do not add application-owned seed data that masks the empty account or missing-period cases.

## 6. Aggregation and timezone strategy

1. Resolve the requested account and its IANA timezone.
2. Interpret the agreed reference date as a date in that timezone. Find the Monday beginning its local week; the latest complete week is the preceding Monday–Sunday.
3. Compute the four immediately preceding Monday–Sunday windows by calendar arithmetic in local dates.
4. Construct local start-of-week and next-week-start boundaries, then convert each boundary to UTC using the IANA timezone. Query with `occurred_at >= startUtc AND occurred_at < endUtc`; this handles DST and avoids inclusive-end precision errors.
5. Aggregate by account, location, event type, and local week. Prefer conversion of timestamp to account zone in PostgreSQL for grouping, or query one bounded UTC interval and group by local date in application code only if correctness and data volume remain clear. Do not use server-local timezone.
6. Generate the expected week and event-type rows in the response layer so absent groups become zero. Generate location rows from locations observed for that account in the seed/result set; do not invent a location catalog.
7. Report total counts and event-type breakdown. Exclude outcome and duration from this MVP to avoid ambiguous denominators and irrelevant NULLs.

## 7. Proposed API contract

`GET /api/accounts/{accountId}/activity-summary?weekStart=2026-07-20`

`GET /api/accounts` returns the account selector options, with each account’s `id`, `name`, and `timeZone`. It may include `hasActivity` based on whether event rows exist, but that field describes observed rows only and is not a source-coverage guarantee.

`weekStart` is an optional ISO local Monday date. If omitted, derive the latest complete week from the configured reporting reference date. For this seeded demo, that reference is 2026-07-28 account-local.

Example response shape:

```json
{
  "account": { "id": 1, "name": "Summit Auto Group", "timeZone": "America/Chicago" },
  "reportingWeek": { "start": "2026-07-20", "end": "2026-07-26" },
  "baselineWeeks": [
    { "start": "2026-07-13", "end": "2026-07-19", "total": 50,
      "byEventType": { "call_received": 31, "lead_created": 12, "appointment_set": 7 } }
  ],
  "coverage": { "status": "complete", "baselineWeeksRequired": 4, "baselineWeeksAvailable": 4 },
  "summary": { "total": 53, "baselineAverage": 48.5, "percentageChange": 9.278,
    "byEventType": { "call_received": 33, "lead_created": 13, "appointment_set": 7 } },
  "locations": [
    { "name": "Site A", "total": 9,
      "byEventType": { "call_received": 5, "lead_created": 3, "appointment_set": 1 },
      "baseline": { "weeklyTotals": [8, 10, 7, 9] } }
  ]
}
```

The numbers in the example’s type/location breakdown are illustrative placeholders, not audited dataset facts. Production response must calculate all values. `baselineAverage` is the arithmetic mean of the four preceding weekly totals when all four are covered; otherwise it is `null`. `percentageChange` is `(reporting total - baselineAverage) / baselineAverage * 100`, rounded only for display, and is `null` when baseline coverage is incomplete or the baseline average is zero. Define errors for 404 unknown account and 400 invalid/non-Monday/future week selection. For known accounts with no event rows, return a successful empty-account state with zero observed counts within covered dates. For incomplete coverage, report coverage metadata and do not present zero as proof of no activity.

## 8. Frontend behavior

- Read account and week filters from query parameters; update the URL on changes using `history`-aware routing behavior.
- Provide an account selector and week selector or previous/next controls bounded to complete weeks. Keep account-local week labels visible.
- Show the selected week total, event-type breakdown, four weekly baseline values, and location comparison in a responsive layout.
- Use plain descriptive language such as “53 events; four-week average 48.5” only when the full baseline exists. Label the average as a descriptive comparison, not a normality verdict.
- Empty account: explain that no activity is present. Zero-event week: show zero explicitly. Insufficient history: state how many baseline weeks are available and suppress the comparison average.
- Add loading, API error, and invalid-query states. Include timezone and exact date range near the summary to avoid ambiguity.

## 9. Testing and verification strategy

- **Unit tests:** week selection from a fixed reference date; Monday boundary; local dates across UTC midnight; DST transition boundaries; four preceding windows; insufficient coverage; zero-filled periods.
- **Integration tests:** PostgreSQL-backed API aggregation for Summit Auto Group and a second account in another timezone; account scoping; event-type totals sum to total; empty Quiet Harbor Spa; explicit zero week; invalid week/account handling.
- **Frontend checks:** URL query state initialization/update/back-forward, loading/error/empty/insufficient states, and rendering returned totals and date labels.
- **End-to-end smoke path:** start Compose, apply migration, load seed once, open Summit dashboard, confirm date range and total, switch account, refresh URL and verify state persists.
- **Manual review:** inspect multi-location comparison, verify no misleading “normal” classification, check responsive layout and startup instructions.
- No test suite should depend on current wall-clock time; use an injected fixed reference date.

## 10. Where an LLM fits

An LLM may assist engineering during scaffolding, test generation, query review, and review of implementation against this plan. A future optional narrative-summary feature could turn already-computed metrics into a short account-admin explanation, for example: “This week had 53 observed events, 9% above the four-week average of 48.5; Site A had the highest count.” The model receives only a validated, minimal metrics payload; it must not query the database, infer missing activity, calculate or alter metrics, or make normality, causal, or predictive claims. Validate its output against the supplied facts (numbers, date range, location names, and permitted claims) before display; reject unsupported statements and fall back to a deterministic template using the same metrics. Keep the narrative optional, disclose when unavailable, and never block dashboard rendering on it. Measure and cap per-request cost and latency, use a short timeout and bounded output, and cache by account/week/metrics version where appropriate. Given the 4–6 hour MVP budget, this remains future work rather than an implementation milestone.

## 11. Risks, trade-offs, and milestones (4–6 hours)

### Risks and trade-offs

- Timezone conversion differs between PostgreSQL and .NET libraries; centralize the rule and test around UTC midnight and DST.
- A snapshot anchored to July 28 is reproducible but not live. Keep the reference injectable so switching to current account-local time is straightforward.
- The dataset has no location dimension or explicit account-specific coverage metadata. A missing event group cannot prove a location was operational with zero activity.
- Four prior weeks are a small descriptive baseline and may be noisy. Show the values without claiming statistical significance.
- Full UI polish and broad validation are at risk within six hours; prioritize correctness and legible empty/error states.

### Milestones

1. **0:00–0:30 — Repository and runtime scaffold:** confirm installed SDK/tooling, create minimal API/frontend/Compose structure.
2. **0:30–1:15 — Persistence:** EF entities, initial migration, PostgreSQL config, unchanged seed load path.
3. **1:15–2:15 — Reporting query:** timezone-aware week windows, aggregation DTOs, coverage/empty behavior.
4. **2:15–3:30 — API and tests:** endpoint validation and focused unit/integration tests against fixed reference date.
5. **3:30–5:15 — Frontend:** URL filters, summary, event breakdown, location comparison, explicit states.
6. **5:15–6:00 — Compose and verification:** launch smoke path, reconcile Summit totals, document setup and known limitations.

## 12. Deliberately deferred work

- Alerts, notifications, forecasting, ML, anomaly detection, statistical significance, and “normal/abnormal” classification.
- Cross-account or industry benchmarking.
- Outcome conversion funnels and call-duration analytics.
- Historical trend navigation beyond the bounded complete-week selector.
- A location master table, location metadata, and locations with no event records.
- Authentication/authorization, multi-user roles, production deployment, observability, caching, and performance tuning beyond basic indexes.
- Custom reporting periods, configurable baselines, and admin-defined metrics.

## Resolved assumptions for implementation

1. Use 2026-07-28 as the fixed seed/demo reference date; production uses the current date in the account timezone via the injected clock.
2. Require all four covered baseline weeks. Otherwise report the available count, set `baselineAverage` and `percentageChange` to `null`, and suppress the comparison.
3. Include locations observed anywhere in that account’s event history; return zero for a location-period only when that period is inside known dataset coverage. Do not claim unseen locations or infer operational status.
4. Accept only complete Monday-start weeks within the dataset’s known covered date range for the demo; reject invalid, future, or out-of-range selections with a clear validation response. Production range policy remains a separate product decision.
5. Use the account selector and URL account ID as demo context only; do not imply authorization. Real authentication and account access control are outside this challenge MVP.
