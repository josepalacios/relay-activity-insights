# AI Interaction 02 — Implementation Planning

## Metadata

| Field | Value |
|---|---|
| Phase | Architecture / Implementation Planning |
| Tool | Codex Desktop |
| Model | Luna — Medium |
| Date | 2026-10-28 |
| Objective | Create `PLAN.md` before implementation |
| Final Status | Redirected → Accepted |

---

## 1. Objective

Create a structured implementation plan for DASH-247 before writing any application code.

The plan should define the product interpretation, MVP scope, technical architecture, API contracts, aggregation strategy, testing approach, and deliberate exclusions.

The implementation must remain achievable within the challenge's **4–6 hour budget**.

---

## 2. Original Prompt

### Prompt 03 — Implementation Planning

We have completed the initial dataset audit and corrected the reporting-week selection logic.

Act as a Senior AI Engineer preparing the implementation plan for DASH-247.

**Task:**

Create `PLAN.md` BEFORE implementing any application code.

### Context

- **Product:** Relay, a multi-location B2B SaaS analytics dashboard.
- **Goal:** Help admins understand whether recent activity is typical.
- **Budget:** 4–6 hours.
- **Stack:** .NET 10, EF Core, PostgreSQL, React, TypeScript, Vite, Docker Compose, xUnit.

### Agreed Decisions

- Reference date: 2026-07-28, account-local.
- Reporting week: July 20–26, 2026.
- Baseline: four preceding complete weeks.
- Timezone-aware aggregation.
- Total events and event-type breakdown.
- Multi-location comparison.
- Explicit empty and insufficient-history states.
- Persist filters in URL query parameters.
- Avoid arbitrary statistical-normality claims.

### Required PLAN.md Sections

1. Problem statement and product interpretation.
2. Dataset facts versus assumptions.
3. MVP scope and acceptance criteria.
4. Architecture and technical decisions.
5. Database migrations and seed loading.
6. Aggregation and timezone strategy.
7. Proposed API contract.
8. Frontend behavior.
9. Testing and verification strategy.
10. Where would an LLM fit?
11. Risks, trade-offs, and implementation milestones.
12. Deliberately deferred work.

### Constraints

- Only create `PLAN.md`.
- Do not implement code.
- Do not change seed data.
- Do not add dependencies.
- Do not execute migrations.
- Keep the scope achievable in 4–6 hours.
- Identify unresolved assumptions.

After creating the plan, summarize the main trade-offs for human review.

**Do not start implementation.**

---

## 3. Original Agent Response

Codex reported:

> Created `PLAN.md`. No application code, dependencies, seed data, or migrations were changed.

### Main Trade-offs Identified

**1. Fixed historical demo date vs. live account-local date**

The agent identified the need to choose between a reproducible historical reporting date and a date derived dynamically during application execution.

**2. Four complete baseline weeks vs. partial historical comparisons**

The plan considered whether to require all four preceding weeks or allow fewer weeks with an explicit caveat.

**3. Event-derived locations vs. a dedicated location catalog**

The dataset does not contain a separate location catalog.

The proposed approach derives known locations from historical event records.

### Agent Outcome

- Generated artifact: `PLAN.md`.
- No application code was implemented.
- No dependencies were installed.
- No migrations were executed.
- No seed data was modified.
- Several product assumptions remained unresolved.

---

## 4. Human Review & Decisions

**Decision: Redirected → Accepted after correction.**

### 4.1. Accepted Decisions

The following architectural and product decisions were accepted:

- .NET 10, React, PostgreSQL, and EF Core architecture.
- Four-week historical baseline.
- Account-local timezone-aware aggregation.
- Explicit handling of empty and insufficient-data states.
- Deterministic aggregation without a runtime LLM dependency.

### 4.2. Issues Identified

Human review identified several ambiguities requiring clarification before implementation.

| Issue | Reason for Review |
|---|---|
| Historical coverage semantics | Zero observed events do not prove that source data was completely collected |
| Missing account selector endpoint | The frontend required an API to retrieve available accounts |
| Incomplete comparison fields | Baseline averages and percentage changes needed explicit definitions |
| Unresolved product assumptions | Implementation behavior needed to be deterministic |
| Incomplete LLM considerations | Trust boundaries, verification, fallback, cost, and latency needed clarification |

### 4.3. Reason for Redirection

The original plan provided a reasonable MVP architecture but left several behaviors underspecified.

The agent was redirected to clarify these decisions before implementation.

---

## 5. Follow-up Prompt

### Prompt 04 — Targeted Planning Corrections

Review the existing `PLAN.md` and apply only the following targeted changes before implementation:

### 1. Historical Data Coverage

Clarify historical data coverage:

- Distinguish zero observed events from missing coverage metadata.
- Use the known seed date range as an explicitly documented approximation.
- Do not claim proof of account/location operational coverage.

### 2. Account Selector API

Add:

`GET /api/accounts`

This endpoint supports the frontend account selector.

### 3. Activity Summary API Contract

Complete the activity-summary API contract with:

- `baselineAverage`
- `percentageChange`

Both values must be nullable when comparison is unavailable or the baseline is zero, as applicable.

### 4. Resolve Five Open Product Assumptions

Resolve the following assumptions:

1. Use a fixed historical demo reference date, configurable for production.
2. Require four baseline weeks.
3. Show known locations with zero selected-week events.
4. Allow historical Monday selections only within documented seed coverage.
5. Do not implement authentication; use a demo account selector.

### 5. Expand the LLM Section

Expand "Where an LLM fits" with one concrete future use case:

Generating an optional narrative summary from verified backend aggregates.

Explain why runtime LLM integration is deferred, including:

- Trust boundaries.
- Validation.
- Failure fallback.
- Cost.
- Latency.

### Constraints

- Preserve the existing structure.
- Preserve original planning decisions.
- Preserve the 4–6 hour scope.
- Do not rewrite unrelated sections.
- Only modify `PLAN.md`.
- Do not generate application code.
- Do not install dependencies.
- Do not run migrations.
- Do not start implementation.

After editing, summarize exactly what changed and identify any remaining risks.

---

## 6. Follow-up Agent Response

Codex reported:

> Updated only `PLAN.md`.

### 6.1. Changes Applied

**Historical coverage**

Clarified the distinction between:

- Zero observed events.
- Unknown source-data coverage.

**Account selector API**

Added:

`GET /api/accounts`

**Comparison API contract**

Defined:

- `baselineAverage`
- Nullable `percentageChange`

**Product assumptions**

The five previously open assumptions were converted into explicit implementation decisions.

**Future LLM integration**

Expanded the LLM section to describe:

- A future narrative generated from verified metrics.
- Input trust boundaries.
- Output validation.
- Deterministic fallback.
- Cost and latency controls.

### 6.2. Remaining Risks

**Source coverage**

The seed does not record source-coverage metadata.

Consequently, zero activity cannot be reliably distinguished from missing source data beyond the known seed interval.

**Authentication**

Authentication remains outside the MVP scope.

**Future LLM integration**

An optional LLM-generated summary would introduce additional cost and latency, even with appropriate safeguards.

---

## 7. Verification & Outcome

### Agent-Reported Results

| Verification | Result |
|---|---|
| `PLAN.md` created before implementation | Reported as completed |
| Initial architecture and MVP scope defined | Completed |
| Historical coverage semantics clarified | Completed |
| Account selector endpoint specified | Completed |
| Baseline and percentage-change fields defined | Completed |
| Five unresolved assumptions addressed | Completed |
| Future LLM use case documented | Completed |
| Application code generated | No |
| Dependencies installed | No |
| Database migrations executed | No |
| Seed data modified | No |

### Human Review

The initial plan was reviewed and redirected to address incomplete requirements and implementation assumptions.

The corrected plan was subsequently approved for implementation.

### Final Decision

**Redirected → Accepted.**

The revised `PLAN.md` was accepted as the implementation reference for DASH-247.


---

### Git History Verification

The implementation plan was committed before application development.

- **Planning commit:** `38e4e90`
- **Commit message:** `docs: finalize DASH-247 implementation plan and AI logs`
- **First implementation commit:** `15083d6`
- **Commit message:** `chore: scaffold application and validate frontend build`

The Git history confirms that `PLAN.md` was committed before
application scaffolding and feature implementation began.

Verification command:

`git log --oneline --reverse`

## 8. Engineering Takeaway

Human review identified ambiguous data coverage, incomplete API contracts, and unresolved product assumptions before implementation.

Codex incorporated the targeted corrections, and the revised `PLAN.md` was approved.

This interaction demonstrates the importance of reviewing AI-generated architectural decisions before implementation, particularly when product requirements are underspecified.

It also establishes that the implementation plan preceded application development, rather than being reconstructed after the code was written.