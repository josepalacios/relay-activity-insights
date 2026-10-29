# AI-Assisted Engineering Log

## Tools and Models
- ChatGPT: Requirements analysis, planning,
  architecture review.
- Codex Desktop: Repository inspection,
  implementation, testing.

## Interaction Index

| ID | Phase | Status | Details |
|---|---|---|---|
| 01 | Dataset Audit | Completed | [Dataset Audit](ai-log/01-dataset-audit.md) |
| 02 | Implementation Planning | Completed | [Implementation Planning](ai-log/02-implementation-planning.md) |
| 03 | Agent Configuration | Completed | [Agent Configuration](ai-log/03-agent-configuration.md) |
| 04 | Project Scaffolding | Completed | [Project Scaffolding](ai-log/04-project-scaffolding.md) |
| 05 | Database Persistence | Completed | [Database Persistence](ai-log/05-database-persistence.md) |
| 05 | Weekly Aggregation | Completed | [Weekly Aggregation](ai-log/06-weekly-aggregation.md) |


## Key Engineering Decisions

- Use .NET 10, React, TypeScript, PostgreSQL, and EF Core for the MVP.
- Compare the latest complete reporting week against four preceding complete weeks.
- Use account-local IANA timezones for weekly aggregation.
- Use July 28, 2026, as the fixed demo reference date for reproducible results.
- Preserve the original seed data and explicitly handle missing historical coverage.
- Use deterministic calculations rather than runtime LLM-generated metrics.
- Keep authentication, forecasting, and anomaly detection outside the MVP scope.

## Human Review and AI Corrections

### 01 — Dataset Audit

Codex initially selected the reporting week based on each account's latest event timestamp.

Human review identified that this could incorrectly shift reporting windows for accounts with no recent activity.

The agent was redirected to use a fixed reference date and account-local calendar weeks.

**Final decision:** Redirected → Accepted.

### 02 — Implementation Planning

The initial implementation plan required clarification of:

- Historical data coverage and zero-activity semantics.
- API contracts for account selection and activity summaries.
- Baseline average and percentage-change calculations.
- Unresolved product assumptions.
- LLM integration boundaries and failure handling.

Human review requested targeted corrections. Codex updated `PLAN.md`, and the revised plan was approved before implementation.

**Final decision:** Redirected → Accepted.

## Engineering Reflections

AI assistance accelerated dataset exploration and architecture planning.

Human verification was essential for identifying incorrect temporal assumptions, clarifying data limitations, and ensuring that implementation decisions remained aligned with the challenge requirements.

The implementation and testing phases will be documented as development progresses.
