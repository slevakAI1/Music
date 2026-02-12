# Epic: Bass Cleanup Operators (post-pass)

## Goal

Implement the 3 bass cleanup/constraint operators defined in `Music/AI/Plans/BassOperators.md` and ensure they always run **after** all other bass operators have been applied.

These operators are not part of the random operator selection pool; they are an enforced post-processing pass to keep bass output playable and internally consistent.

## Scope

- Implement 3 cleanup operators:
  - `BassResolveOverlapsAndOrder`
  - `BassSnapBeatsToSubdivision`
  - `BassPreventOverDensity`
- Wire them into the bass operator pipeline so they run after the normal random operator sequence.
- No unit tests.
- No architecture changes beyond adding a post-pass hook to the existing bass applicator/pipeline.
- Operator scores must be uniform (`score: 1.0`) if they are represented in the registry.
- Ensure each story wires its operator into `BassOperatorRegistryBuilder` so it is audible immediately.

## Non-Goals

- Humanization operators.
- New operator families.
- Changing how harmony is computed.
- Reworking drum operator pipeline.

## Assumptions (from codebase + plan)

- Input is `List<GrooveOnset>` with `MidiNote`, `DurationTicks`, `Velocity`, and `TimingOffsetTicks`.
- Bass operators extend `OperatorBase`, register in `BassOperatorRegistryBuilder`, and are selected/applied by `BassOperatorApplicator` (or equivalent).
- `SongContext` and `BarTrack` provide bar/beat to tick conversion.
- Bass anchor beats are available via `BassOperatorHelper.GetBassAnchorBeats(songContext, barNumber)`.

## Design Requirements

- Cleanup operators MUST run after all other bass operators, even if no other operators ran.
- Cleanup operators MUST be safe on partially-specified onsets (null `MidiNote`, zero/negative durations).
- Cleanup operators MUST preserve the existence of a beat-1 onset when possible (especially for density cap).
- Prefer deterministic behavior (avoid randomness) so repeated runs are stable.
- Keep changes minimal and consistent with existing operator patterns.

## Implementation Approach

- Add a cleanup post-pass in the bass pipeline:
  - Apply normal random operators as today.
  - Then apply cleanup operators in a fixed deterministic order:
    1) `BassSnapBeatsToSubdivision`
    2) `BassResolveOverlapsAndOrder`
    3) `BassPreventOverDensity`

  Rationale: snap first to avoid micro-beats; resolve overlaps after any beat changes; density cap last to trim additions.

- Cleanup operators can be implemented as regular operators, but they are executed by an explicit post-pass call rather than random selection.

## Stories

### Story 1: Implement `BassSnapBeatsToSubdivisionOperator`

**Files:**
- `Music/Generator/Bass/Operators/CleanupAndConstraints/BassSnapBeatsToSubdivisionOperator.cs` (new)
- `Music/Generator/Bass/Operators/BassOperatorRegistryBuilder.cs` (modify)

**Behavior:**

Snaps bass onset `Beat` positions to a fixed grid to prevent drift across stacked operators.

- OperatorId: `BassSnapBeatsToSubdivision`
- OperatorFamily: choose an existing `OperatorFamily` that matches constraints/cleanup (do not add a new enum value).
- For each bass onset in the phrase:
  - `Beat = round(Beat / gridBeats) * gridBeats`
  - If snapped beat becomes `0`, set to `1`.
  - Clamp to bar length (if available); otherwise clamp to `>= 1`.
- Default `gridBeats = 0.25` (16th note in 4/4).

**AC:**
- Operator compiles and is registered.
- No null reference risks (guards for missing bar/meter context).

---

### Story 2: Implement `BassResolveOverlapsAndOrderOperator`

**Files:**
- `Music/Generator/Bass/Operators/CleanupAndConstraints/BassResolveOverlapsAndOrderOperator.cs` (new)
- `Music/Generator/Bass/Operators/BassOperatorRegistryBuilder.cs` (modify)

**Behavior:**

Ensures monophonic bass: no overlaps, strictly increasing start times.

- OperatorId: `BassResolveOverlapsAndOrder`
- OperatorFamily: choose an existing `OperatorFamily` that matches constraints/cleanup.
- Operate on onsets sorted by `(barNumber, beat)`.
- Convert each onset to `(startTick, endTick)` using `BarTrack` and `TimingOffsetTicks`.
- Use policy: `shorten_previous`.
  - If `curStart < prevEnd`, set previous `DurationTicks = max(minDurationTicks, curStart - prevStart)`.
- Ensure each onset has `DurationTicks >= minDurationTicks` after adjustments.
- Default `minDurationTicks = 60`.

**AC:**
- Operator compiles and is registered.
- Resulting onsets are non-overlapping in tick space within each bar.

---

### Story 3: Implement `BassPreventOverDensityOperator`

**Files:**
- `Music/Generator/Bass/Operators/CleanupAndConstraints/BassPreventOverDensityOperator.cs` (new)
- `Music/Generator/Bass/Operators/BassOperatorRegistryBuilder.cs` (modify)

**Behavior:**

Caps maximum onsets per bar by removing least-important notes.

- OperatorId: `BassPreventOverDensity`
- OperatorFamily: choose an existing `OperatorFamily` that matches constraints/cleanup.
- Group onsets per bar.
- If `count <= maxOnsetsPerBar`, do nothing.
- Score each onset for removal:
  - +1 if beat is weak/offbeat (not 1 or 3)
  - +1 if duplicate pitch vs immediate neighbor (same `MidiNote`)
  - +1 if very short (`DurationTicks < shortThresholdTicks`)
  - +1 if beat != 1 (protect beat 1)
- Remove highest-score onsets until `count == maxOnsetsPerBar`.
  - Never remove beat-1 onset if there is any other onset to remove.
- Defaults:
  - `maxOnsetsPerBar = 10`
  - `shortThresholdTicks = 60`

**AC:**
- Operator compiles and is registered.
- Never removes beat-1 onset unless it is the only onset in the bar.

---

### Story 4: Apply Cleanup Operators as Post-Pass

**Files:**
- `Music/Generator/Bass/BassOperatorApplicator.cs` (modify) **or** the equivalent file that executes bass operators
- (If needed) `Music/Generator/Bass/Operators/BassOperatorRegistryBuilder.cs` (modify)

**Behavior:**

Ensure cleanup operators are run after the normal random operator sequence.

- Add an explicit cleanup pass call after the normal operator application.
- Execute cleanup operators in fixed order:
  1) `BassSnapBeatsToSubdivision`
  2) `BassResolveOverlapsAndOrder`
  3) `BassPreventOverDensity`
- Cleanup pass should run even if zero random operators were selected.
- Cleanup pass should be skipped only if no bass onsets exist.

**AC:**
- Cleanup operators run after any other operator output.
- Cleanup operators are NOT selected randomly (unless the existing design forces registry selection; in that case, exclude them from the random selection pool).
- Solution builds clean.

---

### Story 5: Verify End-to-End (manual)

**AC:**
- Solution builds clean.
- Bass phrase generation produces playable monophonic bass without dense spirals.
- Repeated generations show variation from main operators but consistently cleaned output.
