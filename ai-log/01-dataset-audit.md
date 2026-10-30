# AI Interaction 01 — Dataset Audit

## Metadata

| Field | Value |
|---|---|
| Phase | Discovery / Requirements Analysis |
| Tool | Codex Desktop |
| Model | Luna — Medium |
| Date | 2026-10-28 |
| Initial Status | Pending human review |
| Final Status | Redirected → Accepted after correction |

---

## 1. Objective

Inspect the provided database schema and seed data before defining the implementation plan.

The purpose of this interaction was to understand the dataset, identify potential aggregation pitfalls, clarify product assumptions, and assess whether the data supports a five-week activity comparison.

---

## 2. Original Prompt

### Prompt 01 — Dataset Audit

**Role:** Senior Software Engineer reviewing a take-home challenge repository.

**Task:**

Inspect `schema.sql`, `seed.sql`, and the existing `README.md`. Perform a read-only data audit before implementation.

**Analyze:**

1. Exact number of accounts and activity events.
2. Minimum and maximum event timestamps.
3. Distinct event types and outcome values, including `NULL` values.
4. Number of locations per account and accounts with sparse data.
5. Missing or unusual values, duplicate IDs, and potential aggregation pitfalls.
6. Whether the dataset supports comparing the latest complete week with the four preceding complete weeks, using each account's timezone.
7. Recommended representative account and reporting week for a dashboard demo.

Present findings with supporting counts, dates, and evidence. Distinguish verified findings from assumptions.

**Constraints:**

- Do not modify any files.
- Do not implement application code.
- Do not create `PLAN.md` yet.
- Do not invent missing data or regenerate the seed.
- Identify open product questions and recommended defaults.

End with a concise technical assessment and any blockers.

---

## 3. Original Agent Response

### Dataset Audit

**Scope:**

Read-only inspection of `schema.sql`, `seed.sql`, and `README.md`.

The agent also checked `docs/TICKET.md` and `docs/PRODUCT_BACKGROUND.md` for product context.

No files were changed.

### 3.1. Verified Findings

#### Accounts and Events

- **Accounts:** 20
- **Activity events:** 12,626
- **Account IDs:** 1–20
- **Event IDs:** 1–12,626
- **Duplicate account IDs:** None
- **Duplicate event IDs:** None

#### Event Timestamps

| Metric | UTC Timestamp |
|---|---|
| Earliest event | 2026-02-01 10:57:44 |
| Latest event | 2026-07-27 22:20:34 |

#### Event Types

| Event Type | Count |
|---|---:|
| `call_received` | 7,780 |
| `lead_created` | 3,044 |
| `appointment_set` | 1,802 |
| **Total** | **12,626** |

#### Outcome Distribution

| Outcome | Count |
|---|---:|
| `connected` | 4,669 |
| `missed` | 1,937 |
| `open` | 1,871 |
| `completed` | 1,439 |
| `converted` | 1,073 |
| `voicemail` | 934 |
| `no_show` | 305 |
| `NULL` | 398 |
| **Total** | **12,626** |

Each event type contains some `NULL` outcomes:

| Event Type | NULL Outcomes |
|---|---:|
| Calls | 240 |
| Leads | 100 |
| Appointments | 58 |
| **Total** | **398** |

The observed values fit the examples in the schema, with `open` and `completed` appearing as additional values.

### 3.2. Locations and Event Volume by Account

| Account | Events | Locations |
|---|---:|---:|
| Summit Auto Group | 1,223 | 6 |
| Harbor Dental Partners | 730 | 3 |
| Bluebird HVAC | 477 | 2 |
| Cornerstone Vet Clinics | 797 | 4 |
| Pacific Smiles | 884 | 5 |
| Metro Collision Centers | 2,641 | 15 |
| Desert Springs Plumbing | 437 | 2 |
| Lakeside Physio | 261 | 1 |
| Ironwood Fitness | 546 | 3 |
| Gulf Coast Roofing | 342 | 2 |
| Northgate Optical | 354 | 2 |
| Redline Tire & Service | 1,304 | 7 |
| Willow Creek Counseling | 205 | 1 |
| Beacon Home Security | 638 | 4 |
| Sierra Pest Solutions | 499 | 3 |
| Old Town Barbers | 167 | 1 |
| Evergreen Landscaping | 323 | 2 |
| Capital City Storage | 588 | 5 |
| Riverbend Chiropractic | 210 | 1 |
| Quiet Harbor Spa | 0 | 0 |
| **Total** | **12,626** | — |

**Observations:**

- The 19 populated accounts have between 1 and 15 locations.
- Location labels range from `Site A` through `Site O`.
- Quiet Harbor Spa has no events or event-derived locations.
- The lowest nonzero event counts belong to:
  - Old Town Barbers: 167
  - Willow Creek Counseling: 205
  - Riverbend Chiropractic: 210

### 3.3. Missing or Unusual Values

The `duration_seconds` field contains:

- `NULL` for all 1,802 appointments.
- `NULL` for all 3,044 leads.
- `NULL` for 313 calls.
- Non-NULL values for the remaining 7,467 calls.

For non-NULL durations:

- Minimum: 20 seconds.
- Maximum: 1,500 seconds.
- No zero or negative durations were found.

The DDL permits `NULL` only for duration and outcome.

All event rows have:

- A location.
- An event type.
- A timestamp.
- An account ID.

### 3.4. Aggregation Pitfalls

The agent identified the following risks:

1. **Location scope**

   Location is a text label scoped to an account.

   Aggregations should group by:

   `(account_id, location)`

   Grouping only by `location` could combine unrelated locations from different accounts.

2. **Missing locations**

   A location absent from event rows cannot be counted from this dataset.

3. **Duration calculations**

   Duration averages should use call events with non-NULL durations.

4. **Outcome calculations**

   Outcome rates should clearly define their denominator and treatment of NULL outcomes.

5. **Join multiplication**

   Avoid joining event rows to other one-to-many records before counting because this could multiply events.

### 3.5. Weekly Coverage — Initial Assessment

The agent initially reported:

- The seed spans enough time for five complete Monday–Sunday weeks for all 19 populated accounts, interpreted in each account's timezone.
- The most recent complete week is generally July 20–26, 2026.
- Accounts whose last local event falls in the week beginning July 27 instead have July 13–19 as their latest complete week.
- Using local dates, 19 accounts have events in each of their five most recent complete weeks.

For Summit Auto Group, the agent reported:

| Reporting Week | Events |
|---|---:|
| July 20–26, 2026 | 53 |
| July 13–19, 2026 | 50 |
| July 6–12, 2026 | 43 |
| June 29–July 5, 2026 | 62 |
| June 22–28, 2026 | 39 |

Quiet Harbor Spa cannot support an event-based comparison because it has no events.

**Human review note:**

The agent's interpretation of reporting-week completeness was later challenged and corrected. An account's most recent event timestamp does not determine whether a calendar week is complete.

See Section 5 for the correction.

### 3.6. Demo Recommendation

The agent recommended:

| Field | Value |
|---|---|
| Account | Summit Auto Group |
| Reporting Week | July 20–26, 2026 |
| Timezone | America/Chicago |
| Total Historical Events | 1,223 |
| Locations | 6 |
| Selected-Week Events | 53 |

The account has activity in all five comparison weeks, making it a representative multi-location example.

This recommendation assumes that the demo should show a populated, multi-location account and use the newest complete week available in the account's local timezone.

### 3.7. Product Questions and Recommended Defaults

**1. What is the comparison baseline?**

The ticket says "normal" but does not specify a statistic.

Recommended default:

- Use the average of the four preceding complete weeks.
- Show each individual week.
- Clearly label the historical baseline.

**2. Which activity counts?**

Recommended default:

- Count activity events.
- Include breakdowns by event type and location.
- Keep outcome and call-duration metrics separate.

**3. How should NULL outcomes be handled?**

Recommended default:

- Exclude them from outcome-rate denominators.
- Disclose the resulting denominator.
- Do not silently treat NULL as success or failure.

**4. How should empty accounts and locations appear?**

Recommended default:

- Show a no-data state for Quiet Harbor Spa.
- Do not infer locations that have no event rows.

**5. What does "complete week" mean?**

Recommended default:

- Use Monday–Sunday calendar weeks.
- Interpret boundaries in the account's timezone.
- Exclude the current partial week.

### 3.8. Initial Technical Assessment

The agent concluded that the dataset supports the requested five-week comparison for every populated account, using each account's timezone and local calendar weeks.

Quiet Harbor Spa was identified as a data-coverage blocker for any dashboard promising an activity comparison for every account.

The comparison statistic and metric definitions still required product decisions.

The proposed defaults were suitable for a demonstration but were not verified product requirements.

---

## 4. Human Review — Initial Assessment

**Decision:** Redirected — requires additional verification.

### Accepted Findings

- Dataset contains 20 accounts, including one account without activity, according to the agent's audit.
- Event types and nullable fields were identified.
- Location identifiers must be scoped to their accounts.

### Issue Identified

The agent proposed different reporting weeks based on each account's latest event timestamp.

This could incorrectly shift the reporting period backward despite the existence of a complete preceding calendar week.

### Human Intervention

Requested:

- A deterministic reporting reference timestamp.
- Timezone-aware reporting-week boundaries.
- Verification against representative accounts.

### Reason for Redirection

Reporting periods must be consistent and reproducible.

An account's last recorded event should not independently determine whether a calendar week is complete.

---

## 5. Follow-up Prompt and Correction

### Prompt 02 — Review and Correction

I reviewed your dataset audit and want to challenge one of your conclusions.

You stated that some accounts should use July 13–19 instead of July 20–26 because their last event falls in the week beginning July 27.

Please reconsider this. The last event timestamp of an account should not by itself determine whether a prior calendar week is complete.

**Tasks:**

1. Explain your original reasoning and whether it was incorrect.
2. Propose a deterministic reporting reference date or timestamp suitable for a historical seed dataset.
3. Define how to identify the latest complete Monday–Sunday week in the account's IANA timezone.
4. Explain how to select the four preceding baseline weeks.
5. Explain what happens if a selected week has zero events or historical coverage is insufficient.
6. Verify the proposed approach against Summit Auto Group and at least one other account.
7. Clearly distinguish verified dataset facts from assumptions.

**Constraints:**

- Do not modify any files or implement code.
- We are still in the planning phase.

### Agent Correction

Following the human challenge, the agent acknowledged the issue and proposed:

- **Historical reference date:** July 28, 2026.
- **Latest complete reporting week:** July 20–26, 2026.
- **Reporting calendar:** Monday–Sunday in the account's IANA timezone.
- **Historical comparison:** Four preceding complete weeks.

This corrected approach avoids determining reporting-week completeness from an account's most recent event.

### Verification

The corrected date-window logic was reviewed.

The percentage-change calculation was also independently checked using the agent-reported Summit Auto Group counts:

**Selected week:**

53 events.

**Previous four weeks:**

50, 43, 62, and 39 events.

**Baseline average:**

(50 + 43 + 62 + 39) / 4 = 48.5

**Percentage change:**

((53 - 48.5) / 48.5) × 100 ≈ 9.28%

Direct verification of event counts against PostgreSQL remained part of the subsequent implementation-testing phase.

---

## 6. Human Review — Final Decision

**Decision: Redirected → Accepted after correction.**

### Issue Identified

The agent originally determined reporting-week completeness using the last event timestamp for each account.

This could incorrectly exclude a complete reporting week.

### Human Intervention

Challenged the assumption and requested:

- A deterministic reporting reference date.
- Timezone-aware calendar boundaries.
- Verification against representative accounts.

### Agent Correction

The agent acknowledged the issue and proposed July 28, 2026, as the historical reference date, producing July 20–26 as the latest complete reporting week.

### Verification

Reviewed the corrected date-window logic and independently checked the percentage-change calculations using the agent's reported counts.

Direct database verification was deferred to implementation testing.

### Final Decision

**Accepted for implementation planning.**

---

## 7. Engineering Takeaway

AI-generated analysis requires validation of business assumptions, not only code correctness.

Explicit reporting boundaries make analytics reproducible and avoid misleading comparisons.

This interaction demonstrates human oversight by identifying a plausible but incorrect assumption, challenging the agent's reasoning, and requiring a correction before implementation planning.

---

## 8. Follow-up Actions

- Review findings against the seed dataset.
- Finalize product assumptions.
- Prepare `PLAN.md` before implementation.