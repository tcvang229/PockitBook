# PockitBook

A desktop personal finance app: track anticipated bills/income, record actual transaction
history, and project your account balance forward in time on a chart that shows both the
"actual" track record and the "projected" future, split at today.

## Stack

- [Avalonia](https://avaloniaui.net/) (.NET 8) for the UI, with [ReactiveUI](https://www.reactiveui.net/) for MVVM/routing
- [SQLite](https://www.sqlite.org/) via [Dapper](https://github.com/DapperLib/Dapper) for storage
- [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection) for DI
- [Serilog](https://serilog.net/) for logging
- [LiveCharts2](https://livecharts.dev/) for the projection chart
- [xUnit](https://xunit.net/) + [NSubstitute](https://nsubstitute.github.io/) for tests

## Architecture

**MVVM.** `Models/` are plain data records, `ViewModels/` hold UI state and commands,
`Views/` are Avalonia `.axaml` + code-behind. Navigation between pages is handled by
ReactiveUI's `RoutingState`/`RoutedViewHost`, with `AppViewLocator.cs` mapping each ViewModel
to its View.

**Repository pattern.** `Repositories/` holds one class per entity (`AccountRepository`,
`ScheduledItemRepository`, `TransactionRepository`, `BalanceCheckpointRepository`,
`ImportBatchRepository`), each responsible for reading/writing exactly one SQLite table via
Dapper. `SqliteDatabase` (also in `Repositories/`) is the shared low-level piece every
repository depends on - it owns the connection string and creates the schema. It is
deliberately **not** itself a repository (it isn't scoped to one entity), which is why it's
named for what it actually is rather than being called something like `...Repository`.

`Services/` holds application-level logic that orchestrates repositories rather than owning a
table of its own: `CsvImportService` (parses/dedupes/imports a bank CSV) and
`ProjectionCalculator` (pure recurrence-expansion math for the projection chart, no DB
dependency - kept separate so it's unit-testable without touching SQLite).

**Dependency injection.** `Extensions/ServiceCollectionExtensions.cs` wires every
repository/service/ViewModel into a `Microsoft.Extensions.DependencyInjection` container,
built once in `App.axaml.cs`. Everything is constructor-injected - no service locators.

**Testing.** Two test projects, mirroring the codebase's own layering:
- `tests/PockitBook.UnitTests` - pure logic with no real database: ViewModel validation
  methods, `ProjectionCalculator`'s recurrence math, `CsvImportService`'s parsing/dedup-hash
  generation. Collaborators that would otherwise need a real database are NSubstitute fakes
  that are never actually exercised by the method under test.
- `tests/PockitBook.IntegrationTests` - real repository/database behavior against an
  in-memory SQLite database (`TestStartUp.BuildTestServiceProvider`), e.g. that adding a bill
  actually persists, or that the balance-checkpoint formula computes correctly across real
  rows.

## Data model

| Table | Purpose |
|---|---|
| `accounts` | A financial account (Checking/Savings/Credit). v1 seeds exactly three: Checking, Savings, Credit Card. |
| `scheduled_items` | An anticipated, recurring or one-time bill/income - the "recipe" the projection chart expands into future occurrences. |
| `transactions` | An actual, historical transaction - manually entered or CSV-imported. Carries `ExternalId` (an authoritative id from a source, when one exists) and a separate `DedupeHash`/`DedupeHashVersion` (our own computed fallback dedup key) - these are kept as distinct columns on purpose, so the hash algorithm can change later without ever touching an authoritative external id. |
| `balance_checkpoints` | A confirmed balance as-of a date. Replaces a single mutable "starting balance" with a timeline of confirmed points, so a manual correction or a reconciled import can fix drift without losing prior history. Current balance is always *computed* (latest checkpoint + transactions since), never stored. |
| `import_batches` | One row per CSV import, so a bad import can be audited/undone as a whole batch instead of by hand. |

Full design rationale (why this schema, the CSV dedup design, what was explicitly cut from
v1) lives outside the repo at `/mnt/storage/documents/notes/pockitbook/refactor-design.txt`.

## CSV import scope (v1)

Only the Wells Fargo checking/savings/credit-card export format is supported
(`DATE,DESCRIPTION,AMOUNT,CHECK #,STATUS`), and only rows with `STATUS = Posted` are imported -
`Pending` rows can still change amount before they post, which would break the dedupe key.
Other formats (Capital One, loan servicers) and bank-API sync are explicitly out of scope for
now; see the design doc above for the full cut list.

## Bill CSV import

The Bill Details page's "Import Bills CSV..." button lets `BillCsvImportService` add rows to
`scheduled_items` from a CSV file - it *extends* whatever bills/income already exist rather than
replacing them, so importing is always additive; use the grid's inline editing/Delete to clean up
anything the import got wrong. Expected format (header row required):

```
Name,DueDay,Amount,Type
Rent,1,2130,Bill
Phone,4,100,Bill
```

`Type` ("Bill" or "Income", case-insensitive) is optional and defaults to `Bill` if blank or
omitted. Every imported row becomes a Monthly-recurring, active `ScheduledItem` starting today,
same as a row added by hand through the Add Bill form - other recurrences aren't supported by
this import (or that form) yet. Unparseable rows are skipped and logged rather than failing the
whole import, same tolerance as the transaction CSV import above.

## Bill Details grid editing

The `scheduled_items` grid supports inline editing (click a cell) and a per-row Delete button.
`ScheduledItem`'s properties are mutable (`get; set;`, not `get; init;`) specifically so the
`DataGrid`'s two-way cell bindings can write straight into the bound object - a deliberate
departure from the other models, which stay effectively write-once after being loaded/inserted.
Two computed properties exist purely to make editing safe: `DueDay` (an `int` proxy over
`AnchorDate`'s otherwise-unsettable `Day`, clamped to the days in that month) and `ExpectedAmount`
itself clamps to a non-negative magnitude via `Math.Abs` in its setter, since the Bill/Income sign
is applied separately by `ProjectionCalculator` - a negative `ExpectedAmount` would double-negate.
A cell edit is committed to the database from `BillDetailsView`'s `DataGrid.CellEditEnded` handler.

## Recurrence and movable pay dates

The Add-bill row has a Recurrence picker (`Monthly`/`Weekly`/`Biweekly`/`OneTime`). For `Monthly`
the anchor input is still a day-of-month (1-31); for every other recurrence it's a full date
(e.g. `09/18/2026`), since interval-based cadences need a specific date to count from rather than
a day-of-month. `BillDetailsViewModel.BuildScheduledItem` handles both parsings and defaults
`recurrence` to `Monthly` so older call sites don't need updating.

`ScheduledItem.DateAdjustment` (a `DateAdjustmentRule`) controls whether a projected occurrence
date shifts when it lands on a weekend - `NearestPriorBusinessDay` moves a Saturday/Sunday
occurrence back to the preceding Friday, matching how real payroll systems handle a payday that
falls on a non-business day (e.g. a biweekly salary). `BuildScheduledItem` defaults this to
`NearestPriorBusinessDay` for `Weekly`/`Biweekly` and `None` for `Monthly`/`OneTime` - it isn't
user-configurable yet. `ProjectionCalculator` applies the shift per-occurrence, after the
StartDate/EndDate bounds check (which uses the *unshifted* date, so a first occurrence whose
StartDate equals its AnchorDate isn't excluded by its own shift) and before the window-range
check (which uses the *shifted* date, since that's what's actually displayed). The underlying
interval math always walks forward from the unshifted dates, so a shift never compounds or
drifts the recurrence. Bank-holiday shifting is out of scope for now (would need a holiday
calendar) - see the refactor design doc.

## Known issues / design notes

- `DateAdjustment` bank-holiday shifting isn't implemented, only the weekend case - a payday that
  falls on a bank holiday still projects on that exact date rather than shifting earlier.
- The projection window (`ProjectionWindowMonths` in `AccountProjectionViewModel`) is a
  hardcoded 2 months, not yet a user-facing setting.
- Linking an actual `Transaction` to the `ScheduledItem` it fulfilled (`ScheduledItemId`) is
  schema-supported but has no UI yet - it's manual-only, with no auto-matching heuristics.
- `BalanceCheckpointRepository.GetComputedBalanceAsync` is currently only exercised by
  `BalanceCheckpointRepositoryTests` - `GetBalanceHistoryAsync`'s last point supersedes it in
  the actual ViewModel. It's kept as a documented, separately-tested piece of the checkpoint
  formula for reuse once CSV-driven drift-detection is built (only possible for a CSV format
  that reports its own balance, unlike the current Wells Fargo format).

## Building and running

```bash
dotnet build
dotnet test
dotnet run --project src
```

Note: `dotnet run --project src` has been observed creating/using `pockitbook.db` in whichever
directory the command was invoked *from*, not always `src/` itself. If your data seems to have
vanished, check for a stray `pockitbook.db` at the repo root before assuming something broke -
running from inside `src/` (`cd src && dotnet run`) is the reliable way to always hit the same
database file.
