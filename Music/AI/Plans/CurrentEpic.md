# Epic: Remove SongContext concerns from `AgentContext`

## Goal
Remove redundant song-level fields from `Music.Generator.Agents.Common.AgentContext` and refactor call sites to keep behavior intact. The removed values must remain accessible via their existing “SongContext” residence (no new persistent fields introduced).

## Non-goals
- Unit test updates (explicitly deferred to a later epic).
- Feature changes to generation logic (behavior should remain equivalent).

## Constraints / Guardrails
- Minimum changes to meet acceptance criteria.
- Avoid introducing new fields to replace the removed ones.
- Prefer adapters/extension methods only if necessary and if they don’t duplicate state.
- Keep compilation green after each story where feasible.

## Acceptance Criteria
1. `AgentContext` no longer contains the following properties:
   - `Bar`
   - `Beat`
   - `EnergyLevel`
   - `TensionLevel`
   - `MotifPresenceScore`
   - `MotifPresenceMap`
2. All code that previously read these values from `AgentContext` is refactored to read them from their existing Song/SongContext constructs.
3. Application builds and runs as before (functionality intact).
4. No unit tests are added/updated in this epic.

## Stories

### Story 1: Inventory and map ownership of removed fields
**Size:** Small
**Description:** Identify where each redundant `AgentContext` property is produced and what the canonical source is (SongContext/Bar/Section/arrangement context). Catalog all call sites that read/write these properties.

**Tasks:**
- Locate all references to `AgentContext.Bar`, `.Beat`, `.EnergyLevel`, `.TensionLevel`, `.MotifPresenceScore`, `.MotifPresenceMap`.
- Identify the canonical types/objects already holding these concepts (e.g., `SongContext`, arrangement state, bar timeline, motif map).
- Document mapping (old access → new access).

**Deliverable:**
- Updated plan section “Refactor Map” in this file with concrete symbols/files.

**Breaking change:** No.

---

### Story 2 (Breaking): Remove redundant properties from `AgentContext`
**Size:** Small
**Description:** Remove the redundant properties and update `CreateMinimal` accordingly.

**Tasks:**
- Modify `AgentContext` to remove the listed properties.
- Adjust/update/remove `CreateMinimal` so it still produces a valid minimal context for agents (without duplicating song state).
- Fix compilation errors in `Music.Generator.Agents.Common` that are direct fallout.

**Breaking change:** Yes.
- Break occurs when removing members used by call sites.
- Fixed in Story 3.

---

### Story 3: Refactor agent/operator call sites to use canonical SongContext sources
**Size:** Medium
**Description:** Update all callers and consumers to obtain `Bar`, beat position, energy/tension, and motif presence from the existing canonical context objects.

**Tasks:**
- Update agent pipelines/builders that create `AgentContext` to stop passing removed values.
- Update operators/agents reading those values to read them from canonical sources.
- If necessary, pass an existing “SongContext” reference through already-existing parameters/contexts (do not add new state fields). Prefer using existing context types already present in call chains.
- Ensure deterministic behavior remains (seed/RNG stream isolation unchanged).

**Breaking change:** No (restores build).

---

### Story 4: Validate build and runtime smoke path
**Size:** Small
**Description:** Ensure the solution builds and basic generation flows execute without regressions.

**Tasks:**
- Run full build.
- Run the app or a minimal generation CLI/task (whatever exists in repo) to ensure no runtime exceptions from null/changed access paths.

**Breaking change:** No.

## Refactor Map (to be filled in Story 1)
- `AgentContext.Bar`
  - Canonical source: `Music.Generator.Bar` (with section + phrase + TS context)
  - Canonical producer: `Music.Generator.BarTrack.RebuildFromTimingTrack(Timingtrack, SectionTrack, ...)`
  - Canonical residence: `Music.Generator.SongContext.BarTrack` (and sections via `SongContext.SectionTrack`)
  - Known consumers (examples): operators use `context.Bar.*` (e.g., `Music/Generator/Agents/Drums/Operators/PatternSubstitution/BackbeatVariantOperator.cs`)

- `AgentContext.Beat`
  - Canonical source: per-operator/per-candidate beat position (not song-global state)
  - Canonical residence (examples): `Music.Generator.Agents.Drums.DrumCandidate.Beat`
  - Time mapping uses song state via: `Music.Generator.BarTrack.ToTick(int barNumber, decimal onsetBeat)`
  - Known usage: `AgentContext.Beat` is currently set to `1.0m` by `DrummerContextBuilder` and is not the per-candidate beat.

- `AgentContext.EnergyLevel`
  - Canonical source: Stage 7 energy system (archived types still present under `Archives/Energy/*`)
  - Current producer (duplication): `Music/Generator/Agents/Drums/Context/DrummerContextBuildInput.EnergyLevel` passed into `DrummerContextBuilder`.
  - Migration intent for Story 3: read/derive from an existing song/arrangement intent object already in the call chain (avoid duplicating state in contexts).

- `AgentContext.TensionLevel`
  - Canonical source: Stage 7 tension system (archived, e.g., `Archives/Energy/DeterministicTensionQuery.cs`)
  - Current producer (duplication): `Music/Generator/Agents/Drums/Context/DrummerContextBuildInput.TensionLevel` passed into `DrummerContextBuilder`.
  - Migration intent for Story 3: read/derive from an existing song/arrangement intent object already in the call chain (avoid duplicating state in contexts).

- `AgentContext.MotifPresenceScore`
  - Canonical source: `Music.Generator.Material.MotifPresenceMap.GetMotifDensity(int barNumber, string? role = null)`
  - Current producer (duplication): `Music/Generator/Agents/Drums/Context/DrummerContextBuildInput.MotifPresenceScore` passed into `DrummerContextBuilder`.
  - Canonical residence: motif planning/material system (`MotifPlacementPlan` + `MotifPresenceMap`), likely reachable from `SongContext.MaterialBank` inputs.

- `AgentContext.MotifPresenceMap`
  - Canonical source/residence: `Music.Generator.Material.MotifPresenceMap` (constructed from `MotifPlacementPlan` + `SectionTrack`)
  - Common query APIs: `IsMotifActive`, `GetMotifDensity`, `GetActiveMotifs`
  - Known consumer path: operators/policy providers query motif presence for accompaniment ducking (see `Music/Generator/Material/MotifPresenceMap.cs` header notes).

## Implementation Summary (after epic execution)
Write to: `Music/AI/Completed/<timestamp>-AgentContext-SongContext-Refactor.md`
