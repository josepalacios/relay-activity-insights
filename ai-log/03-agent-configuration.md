# AI Interaction 03 — Agent Configuration

**Tool:** Codex Desktop
**Model:** Luna
**Objective:** Establish repository-level instructions before implementation.

## 1. Original Prompt

Act as a Senior Software Engineer working on the DASH-247 take-home challenge.Read PLAN.md, AI_LOG.md, README.md, docs/PRODUCT_BACKGROUND.md, docs/TICKET.md, and the supplied SQL schema and seed files before proceeding.Create a concise AGENTS.md at the repository root to guide subsequent implementation tasks.Required instructions:1. Follow the approved PLAN.md and prioritize a correct, testable vertical slice within the 4–6 hour timebox.
2. Use the approved .NET 10, ASP.NET Core, EF Core, PostgreSQL, React, TypeScript, Vite, Docker Compose, and xUnit stack.
3. Work incrementally; implement only the explicitly requested task.
4. Preserve the original SQL schema and seed. Use real relational persistence.
5. Keep aggregations deterministic and account-scoped; respect IANA timezones, local Monday–Sunday weeks, UTC half-open intervals, four baseline weeks, and zero/unknown coverage semantics.
6. Use the fixed demo reference date 2026-07-28. Never select a reporting week using an account's latest event timestamp.
7. Avoid unnecessary architecture, authentication, forecasting, anomaly detection, and runtime LLM integration.
8. Add focused tests and run applicable checks. Never report tests as passed without running them.
9. After each task, report modified files, commands, test results, risks, and decisions requiring review.
10. Support authentic AI logging without fabricating interactions or overwriting existing logs.Constraints: Create or update only AGENTS.md. Do not implement code, install dependencies, run migrations, or modify existing project documents.Finally, display the complete AGENTS.md and explain any assumptions requiring human review.

## 2. Agent Response

Created AGENTS.md  

# Repository guidance

- Follow the approved `PLAN.md`. Prioritize a correct, testable vertical slice within the 4–6 hour timebox.
- Use the approved stack: .NET 10, ASP.NET Core, EF Core, PostgreSQL, React, TypeScript, Vite, Docker Compose, and xUnit.
- Work incrementally and implement only the task explicitly requested.
- Preserve `schema.sql` and `seed.sql` exactly. Use real relational persistence; do not replace it with in-memory or fabricated data.
- Keep aggregations deterministic and account-scoped. Use account IANA timezones, local Monday–Sunday weeks, and UTC half-open intervals. Compare with the four preceding complete weeks. Distinguish covered zero counts from unknown or insufficient coverage.
- Use the fixed demo reference date `2026-07-28`. Never choose a reporting week from an account's latest event timestamp.
- Avoid unnecessary architecture, authentication, forecasting, anomaly detection, and runtime LLM integration.
- Add focused tests and run applicable checks for implementation changes. Report a test as passed only if it was actually run; state clearly when checks were not run.
- After each task, report modified files, commands, test results, risks, and decisions that need review.
- Keep AI logs authentic: record only interactions that occurred, and append without fabricating entries or overwriting existing logs.

No tests or other checks were run because this task only created guidance and explicitly prohibited implementation work or other project changes. Human review assumptions: the plan’s resolved coverage rules and the fixed demo date are authoritative for subsequent implementation. Git status also showed four pre-existing untracked AI log files; I did not modify them.

## 3. Human Review & Decisions

**Decision:** Accepted.

**Accepted:**
- Repository-level instructions aligned with PLAN.md.
- Incremental implementation and scope control.
- Deterministic, timezone-aware aggregations.
- Preservation of the original SQL dataset.
- Focused testing and transparent reporting.

**Issues identified:** No blocking issues.

## 4. Outcome

**Final decision:** Accepted.

**Key takeaway:** Repository-level guidance was established before
implementation to constrain AI-generated changes and maintain
consistency with the approved architecture.