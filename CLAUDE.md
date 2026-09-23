# Architecture standards — K-12 attendance replacement

Binding for this build. Apply all of it. If any item is wrong for this problem, say so rather than following it silently.

## Layering and modularity

- Four projects: `Domain`, `Application`, `Infrastructure`, `Api`. Dependencies point inward only.
- `Domain` references nothing — no EF, no ASP.NET, no attributes from either. Entities, value objects, policies, invariants.
- `Application` — use cases, request/response contracts, repository abstractions, a `Result` type. No EF types leak in.
- `Infrastructure` — EF Core mappings, repositories, clock, identity. `internal` by default.
- `Api` — **ASP.NET Core Web API with controllers**: `[ApiController]` classes deriving from `ControllerBase`, attribute routing, `ActionResult<T>`, problem-details mapping, and the composition root. Controllers stay thin adapters over the application services — no business logic in an action method.
- Any rule a district changes by policy goes behind an interface, not an `if`.

## Performance

- Set-based only. No per-row database round trips.
- One request = one `SaveChanges` = one transaction. Build the change set in memory, commit once.
- Every query bounded and index-backed. The year tally filters an indexed date range; it does not scan a student's whole history.
- `AsNoTracking` on reads. Track only what you mutate.
- No N+1. Load lookups once per request and dictionary them.

## Correctness and data integrity

- Invariants belong in the schema, not in procedural checks: unique index on `(StudentId, Date)`, real foreign keys, a filtered unique index for one open alert per student per school year.
- Validate the entire batch before writing anything. One invalid entry rejects the whole submission, and every error returns at once keyed to the offending row.
- Expected failures are values (`Result<T>`), not exceptions. Exceptions mean faults.
- A lost race on a unique index is a `409`, never a `500`.
- Derived over stored. Do not keep a counter that can drift from the records.

## Authentication, security, audit

- No string-concatenated SQL. Parameterised only.
- Typed JSON contracts. No XML blobs, no hand-built markup.
- One authentication seam: `ICurrentUser`, reading an authenticated principal when present. Full auth is out of scope — say so in the README rather than half-implementing it.
- Every write carries who and when, from injected `ICurrentUser` and `IClock`. No `DateTime.Now`, no `SYSTEM_USER` in business logic.
- Audit log of submissions. No PII in application logs.

## Design patterns — use these

- **Value Object** for the school year. The September rollover exists exactly once.
- **Strategy** behind an interface for the chronic-absenteeism rule, selected from configuration.
- **Repository + Unit of Work**, narrow interfaces — not a generic `Repository<T>`.
- **Result / operation-result** for expected failures.
- **Domain factories throw; they do not return `Result<T>`.** A violated domain invariant is a programmer error, not user input - the application layer validates before it ever constructs an entity. `Result<T>` lives in `Application` and never appears in `Domain`, which keeps the dependency pointing inward.
- **Options object** for judgement calls: default present code, batch cap, whether future dates are allowed.
- **Factory methods** on entities so an invalid entity cannot be constructed.
- Separate read and write services. CQRS-lite, no event sourcing.

**Do not add:** a mediator or event bus, AutoMapper, a generic repository, microservices, or a caching layer. If one is genuinely warranted, argue for it first.

## Tests

- **Budget: 45–60 tests in total, and no more than 20 in `Domain`.** This is a hard cap, not a guide. Three boundary cases for a value object is the point; thirty is padding that proves nothing extra and costs real time.
- One named regression test per legacy defect, with the defect named in the test name. These are the tests that matter — spend the budget here first.
- `Domain` tests pure and fast. `Application` tests run the real services over the real repositories against in-memory SQLite, so indexes and transactions are genuinely exercised.
- `Api` tests drive the real DI graph over HTTP.
- Tests are the gate: passing between stages, not all at the end.
- **Attendance dates in tests must be at or before the injected clock's `Today`**, or advance the clock first. The future-date rule will reject a date in the future and the test then fails for the wrong reason — which looks like a bug in the code and is not.

## Pace — this is a timed exercise

Generation time is roughly linear in how much text you produce. Treat every one of these
as a hard rule, not a preference.

- **No commentary.** Build it, then report the test and warning counts and stop. Do not summarise what you just wrote, do not explain the design, do not restate my requirements back to me. I can read the diff, and I will ask if I want the reasoning.
- **No XML doc comments.** A one-line `//` comment where the intent genuinely is not obvious from the code. Nothing else. The patterns are visible without being announced.
- **No source file over 150 lines.** If one is heading past that, split it or you are writing too much.
- **`dotnet test` builds. Never run `dotnet build` separately** - that doubles the compile for no information.
- **Always `dotnet test --no-restore`.** The solution is pre-restored; restoring on every run is wasted seconds.
- **One test project, not one per layer.** Three test projects means three test-host startups on every run.
- **Four test runs in the whole session, maximum:** one to show red, one to reach green, one after the mutation, one at the end. While iterating inside a step, run the single affected project, not the solution.
- **Run the suite once per step**, at the end, not between sub-steps. Each run costs real seconds across every project.
- **Write files with the file-writing tool, never shell heredocs or quoted multi-line strings.** On Windows these silently mangle backslashes and quoting, and each failure costs a full round trip to discover.
- **Do not gold-plate.** When a layer satisfies its stated requirements, stop and say so. No extra cases, overloads, helpers or abstractions nobody asked for.

## Platform

- **A `global.json` pins the .NET 8 SDK for this solution.** Do not remove it and do not target anything newer. An SDK 10 toolchain silently changes defaults - `dotnet new sln` emits `.slnx`, which the .NET 8 SDK cannot open at all (`MSB4068`) - so the pin is what keeps the build matching the stated target.

.NET 8 LTS, C# 12. **ASP.NET Core Web API with controllers** (`AddControllers` / `MapControllers`), not minimal APIs. EF Core 8, SQLite only, so it runs on a clean machine with nothing installed. Do not wire a second provider: keep the provider choice behind configuration and note in the README that SQL Server is a one-line swap. xUnit, no mocking framework. Swagger on. Nullable enabled, warnings as errors — a zero-warning build.

`[ApiController]`'s automatic model-state 400 must be re-shaped to the same problem contract as a rejected roster, so a client has one error format rather than two.

## The hard constraint

**Build clean from scratch. Do not port, translate, wrap or call any legacy code.** The stored procedures are input to understanding and nothing else. The XML payload dies with the VB6 form; the API takes typed JSON.
