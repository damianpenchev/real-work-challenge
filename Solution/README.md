# School Attendance

## Decisions

- `SchoolYear` value object holds the 1 Sep rollover as a half-open `[1 Sep, 1 Sep)` range — legacy re-derived it in three places and made the read-path filter non-sargable.
- Whole batch validated before any write, all errors returned at once keyed to their row — legacy threw mid-cursor and left a partial day committed.
- One `SaveChanges` per submission — legacy autocommitted each row with no transaction.
- `IsAbsent`/`IsExcused` derived from the code inside the entity factory — legacy reused stale loop variables when a code lookup matched nothing.
- Unique index on `(StudentId, Date)` and a filtered unique index for one open alert per student per year, mapped to `409` — legacy used read-then-write checks that two clerks race straight through.
- Totals derived from the indexed date range on every read — legacy stored a counter that drifted from the rows.

## Could not determine

- Whether excused absences count toward chronic absenteeism — unknown; counted, matching legacy, behind `IAbsenteeismPolicy`.
- Attendance window/lock-after-N-days — unknown; only future dates rejected, via `AttendanceOptions`.
- Instructional calendar (weekends, holidays) — unknown; any past date accepted.
- Mid-year transfers — unknown; records keyed to the school that entered them.
- Alert resolution and re-raise policy — unknown; one open alert per student per year, never auto-reopened.

## Gaps

- Authentication is not implemented: `ICurrentUser` is the single seam; SQLite only, SQL Server is a one-line provider swap in `Program.cs`.
