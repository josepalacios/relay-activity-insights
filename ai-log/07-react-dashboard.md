# AI Interaction 07 — React Activity Dashboard

## 1. Objective

Implement a functional React and TypeScript dashboard for
DASH-247 using real data from the existing ASP.NET Core API.

The dashboard must allow users to explore weekly activity,
historical comparisons, and location-level performance.

## 2. AI Tool and Scope

- Tool: OpenAI Codex Desktop
- Role: Senior React and TypeScript Engineer
- Scope: Frontend implementation and API integration
- Backend: Existing .NET 10 API with PostgreSQL
- Constraints: No database, aggregation, or schema changes

## 3. Agent Instructions

The agent was instructed to:

- Read AGENTS.md, PLAN.md, and README.md.
- Replace the frontend placeholder with a functional dashboard.
- Consume existing API endpoints without introducing mock
  data into the production application.
- Implement account and reporting-week filters.
- Persist filters in URL query parameters.
- Display weekly totals, event breakdowns, historical
  comparisons, and location-level activity.
- Handle loading, error, empty-account, and incomplete-
  coverage states.
- Avoid arbitrary anomaly thresholds or invented scores.
- Verify the production frontend build.
- Avoid modifying planning documents and AI logs.
- Stop without committing or pushing changes.

The complete original prompt and agent response should be
preserved with the interaction record.

## 4. Agent Implementation

Codex implemented:

- Account selection through the existing accounts API.
- Reporting-week selection, defaulting to 2026-07-20.
- URL-persisted account and week filters.
- Weekly activity totals and event-type breakdowns.
- Four-week historical activity comparisons.
- Location-level activity comparisons.
- Loading, API error, and empty-account states.
- Missing-baseline and incomplete-coverage handling.
- Vite and Docker Compose API proxy configuration.

No backend aggregation or database schema changes were needed.

## 5. Human Verification

The complete application was started using Docker Compose
and manually verified through the following flow:

PostgreSQL → ASP.NET Core API → React Dashboard

| Verification | Result |
|---|---|
| Docker Compose services | Passed |
| PostgreSQL dataset | 20 accounts, 12,626 events |
| Backend health endpoint | HTTP 200 |
| Accounts API | HTTP 200 |
| Weekly summary API | HTTP 200 |
| Summit Auto Group weekly total | 53 |
| Summit historical average | 48.5 |
| Summit percentage change | +9.28% |
| Account selection | Passed |
| Reporting-week selection | Passed |
| Filter persistence after refresh | Passed |
| Location breakdown rendering | Passed |
| Quiet Harbor Spa empty state | Passed |
| Browser network requests | Verified |
| Frontend production build | Passed |
| git diff --check | Passed |

## 6. Limitations and Decisions

The dashboard relies on coverage metadata provided by the
backend. Dataset coverage is inferred from global event
timestamps and does not prove complete source ingestion.

Location comparisons use available historical averages
without introducing arbitrary attention thresholds.

The first frontend implementation did not include automated
component tests. This was identified as a quality gap and
addressed in a separate, focused testing interaction.

## 7. Final Decision

**Accepted — with a follow-up testing milestone.**

The dashboard was accepted after independent manual
end-to-end verification confirmed that it consumed real
PostgreSQL-backed API responses and preserved user filters.

## 8. Key Takeaway

Human verification established that the generated frontend
worked as an integrated application, not merely as a
successful TypeScript build.

The absence of automated frontend tests was identified and
handled as a separate engineering improvement.