# ADR-0001: Service Layer with IDataService / DataService

**Date:** 2026-07-29
**Status:** Accepted

## Context

The original codebase had no separation between data access and UI logic.
`MapView` (an 1100-line Page) directly held an `AppDbContext` instance and
performed all CRUD operations inline. All service classes (`DatabaseService`,
`SettingsService`) were static, making them untestable and tightly coupled.

The codebase had:
- Raw `_db.Targets.Where(...).ToList()` scattered across 30+ call sites in `MapView`
- No interface abstractions for data access
- DbContext lifecycle tied to page lifetime
- No way to swap implementations or unit test

## Decision

Introduce a service layer with:

1. **`IDataService` interface** — defines all database operations (targets,
   lineups, tricks, profiles)
2. **`DataService` class** — concrete implementation wrapping `AppDbContext`
3. **App-level singleton** — `App.DataService` property, created at startup,
   disposed on exit
4. **Views access via `((App)Application.Current).DataService`** — no
   constructor injection (avoids cascading changes to WPF page constructors)

### Methods on IDataService

Each method encapsulates a common data operation:
- `GetTargets`, `GetAllTargets`, `GetTarget`, `AddTarget`, `DeleteTarget`
- `AddLineup`, `UpdateLineup`, `DeleteLineup`, `GetLineup`, `GetLineupsQuery`
- `GetTrick`, `GetTricks`, `AddTrick`, `UpdateTrick`, `DeleteTrick`
- `GetActiveProfile`, `SetActiveProfile`, `GetAllProfiles`, `CreateProfile`
- `SaveChanges`

### Key design choices

- Read queries use `AsNoTracking()` to avoid change tracker bloat
- Write methods load tracked entities internally, modify, and call `SaveChanges`
- Each write method (Add/Delete) calls `SaveChanges` internally to ensure
  atomicity and avoid stale-state issues
- `DataService` is `IDisposable` — disposes the underlying `AppDbContext` on
  app exit

## Consequences

### Positive

- **Testability**: `IDataService` can be mocked for unit tests
- **Separation of concerns**: MapView no longer writes raw LINQ queries
- **Consistent error handling**: All DB operations go through a single layer
- **Cleaner lifecycle**: DbContext is created once and disposed at app exit
  (not per-page)
- **Future-proof**: Easy to add caching, logging, or metrics around the
  service boundary

### Negative

- **Extra abstraction**: Small projects may not need this layer
- **Performance**: Add/Delete methods call `SaveChanges` internally, and
  some callers also call `SaveChanges` afterward (redundant but harmless)
- **Shared context**: Single DbContext means all operations share the same
  change tracker. Read-heavy pages should always use `AsNoTracking()`

## Alternatives Considered

### Full Repository + Unit of Work

Would have added `ITargetRepository`, `ILineupRepository`, etc. Rejected as
over-engineering for the current complexity level.

### Static service methods

The original approach. Rejected because it prevents mocking and DI.

### DI container (Microsoft.Extensions.DependencyInjection)

Rejected for now because the WPF codebase is small and constructor injection
would require significant changes to XAML page instantiation.
The `((App)Current).DataService` pattern is a pragmatic middle ground.
