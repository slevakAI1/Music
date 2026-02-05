# Epic: DrummerContext de-duplication and bar-derived context

## Goal
Remove redundant fields from `Music.Generator.Agents.Drums.Context.DrummerContext` so it contains only non-bar-derivable state, while keeping runtime behavior intact.

## Constraints
- Minimum-change refactor; preserve behavior.
- OK to introduce temporary breaking changes if it speeds delivery, but note them and when they get fixed.
- Unit tests: do not add/update tests in this epic.
- Output of this epic is structural cleanup; a later epic will address test updates.

## Story 1 (Breaking): Inventory DrummerContext usage and define the target contract
**Intent:** Identify every current consumer of `DrummerContext` and classify each property as either (a) available via `Bar`/`SongContext` object graph, (b) trivially computable from `Bar`, or (c) truly cross-bar state.

**Scope / Tasks**
1. Search repo for:
   - `DrummerContext.` property access
   - `new DrummerContext {` initializers
   - `CreateMinimal(` usage
2. Locate `SongContext`/`AgentContext` definitions and confirm which fields already exist there or are reachable from `Bar`.
3. Produce the “new minimal DrummerContext contract”:
   - Keep: `LastKickBeat`, `LastSnareBeat`
   - Remove: all other drum properties currently in `DrummerContext` that are bar-derivable or reachable.
4. Decide replacement access patterns for removed properties, e.g.:
   - `context.Bar` and its object graph
   - helper/extension methods already in repo (prefer reuse)

**Acceptance Criteria**
- A concrete list of `DrummerContext` properties to delete and the exact replacement expression(s) per usage site.

**Notes**
- This story intentionally allows breaking changes because the next story will do the mechanical refactor.

## Story 2: Refactor code to eliminate redundant DrummerContext properties
**Intent:** Remove redundant properties from `DrummerContext` and update all call sites to use `Bar` / existing context sources.

**Scope / Tasks**
1. Edit `DrummerContext`:
   - Delete redundant properties (everything except `Bar` (inherited/required), plus `LastKickBeat`, `LastSnareBeat`).
   - Remove `ResolveEnergyLevel` and any energy-level storage in `DrummerContext`.
   - Simplify `CreateMinimal` accordingly.
2. Update all usage sites:
   - Replace property reads with bar-derived equivalents.
   - Replace property writes/initializers with no-ops or bar-derived sources.
3. Build and run the existing smoke path (`dotnet build`, optional `dotnet run --project Music/Music.csproj --no-build`).

**Acceptance Criteria**
- `DrummerContext` contains no redundant fields.
- Solution builds successfully.
- WinForms app still starts via existing smoke command.

## Story 3: Cleanup and documentation alignment
**Intent:** Ensure no dead code remains and the codebase reflects the new contract.

**Scope / Tasks**
1. Remove any now-unused constants/helpers/imports made obsolete by the refactor.
2. Update any AI-facing comments that describe `DrummerContext` semantics to match the new minimal contract.
3. Confirm `CreateMinimal` is still usable by existing callers (or adjust callers).

**Acceptance Criteria**
- No references to removed `DrummerContext` properties remain.
- Build succeeds.

## Implementation Summary (post-epic)
Write summary to: `Music\\AI\\Completed\\<date>-Epic-DrummerContext-Dedup.md`
- Mention removed fields and the new access pattern (`context.Bar...`).
- Mention any breaking changes introduced and how/when resolved within the epic.
