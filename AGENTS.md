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