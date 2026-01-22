# Pre-Analysis — Story 2.5: Implement Drummer Memory

## 1. Story Intent Summary
- What: Add a drummer-specific memory component (`DrummerMemory`) that extends the shared `AgentMemory` to track recent fills, crash patterns, hat-mode history, and ghost-note frequency for anti-repetition and stylistic consistency.
- Why: Persistent, deterministic memory prevents robotic repetition, preserves section-level identity, and enables style-consistent decisions (fills, crashes, hat changes) across bars/sections.
- Who benefits: Developers (clear contract + tests), the generator/selection engine (uses memory to penalize/reward candidates), and end-users/listeners (more musical, less repetitive output).

## 2. Acceptance Criteria Checklist
1. Create `DrummerMemory` extending `AgentMemory` with fields/properties:
   1. `LastFillBar` (which bar had the last fill)
   2. `LastFillShape` (FillShape: roles used, density, ending beat)
   3. `ChorusCrashPattern` (consistent crash placement for this song's choruses)
   4. `HatModeHistory` (track hat subdivision changes)
   5. `GhostNoteFrequency` (rolling average of ghost usage)
2. Enforce anti-repetition for fills: do not repeat the exact same fill shape in adjacent sections.
3. Section signature: remember what worked for each section type.
4. Unit tests: memory tracks and recalls correctly.

Notes: The ACs above are grouped by data model, behavioral rule, and test coverage. "Anti-repetition for fills" and "Section signature" are behavioral ACs that interact.

Ambiguous/unclear ACs:
- "ChorusCrashPattern": format, granularity, and update policy are not fully specified (e.g., absolute beats vs relative positions).
- "GhostNoteFrequency": window size and update rule (simple average, exponential decay) are unspecified.

## 3. Dependencies & Integration Points
- Story dependencies (explicit sequence in epic):
  - Stage 1 stories: `1.1` (Contracts), `1.2` (AgentMemory), `1.3` (Selection), `1.4` (StyleConfig)
  - Stage 2 prior stories: `2.1` (DrummerContext), `2.2` (DrumCandidate), `2.3` (DrummerPolicyProvider), `2.4` (DrummerCandidateSource)
- Types and code this story will interact with:
  - `AgentMemory` and `IAgentMemory` (base memory contract)
  - `FillShape` (representation of fills)
  - `DrummerContext` / `GrooveBarContext` (bar/section metadata)
  - `StyleConfiguration` (memory-related settings)
  - Operator registry / selection engine (reads memory for repetition penalties)
  - Diagnostics (to report memory state in traces)
- What this story provides for future stories:
  - Memory APIs and persistent state consumed by operators (Stage 3) and physicality/selection engines.
  - Section signature data for style tuning (Stage 5) and diagnostics (Stage 7).

## 4. Inputs & Outputs
- Inputs (consumed):
  - Recorded decisions/events: operatorId, candidateId, barNumber, fillShape metadata
  - Bar/section metadata: `SectionType`, `BarNumber`, `PhrasePosition` from `DrummerContext`
  - Style/config settings: lookback window sizes, decay rules, whether to enforce anti-repetition
  - RNG seed/stream keys only insofar as determinism requirements imply deterministic updates
- Outputs (produced / exposed):
  - Memory state accessible via `IAgentMemory`/`DrummerMemory` methods: `LastFillBar`, `LastFillShape`, `ChorusCrashPattern`, `HatModeHistory`, `GhostNoteFrequency`, and section signatures
  - Read-only snapshots used by selection engine, diagnostics, and policy provider
- Configuration/settings read:
  - Memory lookback lengths (bars for operators, fills)
  - Decay policy/parameters for repetition penalty and ghost-note rolling averages
  - Flags controlling whether consecutive same-fill is allowed

## 5. Constraints & Invariants
- Determinism: Given the same sequence of recorded events and same seed/context, memory state must be identical (deterministic updates).
- Anti-repetition invariant: An exact same `FillShape` must not be selected in two adjacent sections when `AllowConsecutiveSameFill` is false.
- Immutability for reads: Memory consumers must not mutate memory state directly; state updates occur only via defined record methods.
- Hard limits (to be provided or defaulted by style/config):
  - Max lookback bars for operator/fill memory (e.g., configurable N bars)
  - Allowed rate for ghost notes (upper bound enforced by physicality/style rules)
- Order of operations: Memory must be consulted before final selection (to compute penalties) and updated after selection/commit of an operator to reflect the decision for subsequent bars.

## 6. Edge Cases to Test
- Empty memory (fresh song) — queries should return sensible defaults (no exceptions).
- Multiple fills in one bar — `LastFillBar` and `LastFillShape` semantics when several fills occur in same bar.
- Rapid successive section boundaries — ensure anti-repetition rule compares correct adjacent sections.
- Conflicting updates (e.g., simultaneous hat-mode change and fill recorded on same tick) — deterministic resolution order.
- Rolling average window shorter than observed events — behavior when fewer samples than window.
- Out-of-order bar records or negative/zero bar numbers — validate or guard against invalid inputs.
- Very long songs — memory size limits and bounded growth.
- Style changes mid-song — how section signatures and chorus patterns adapt (persist or reset?).

## 7. Clarifying Questions

1. What exact shape/schema does `FillShape` use? Which fields determine "exact same fill" for anti-repetition checks?

**Answer:** `FillShape` is defined in `Generator/Agents/Common/FillShape.cs` as an immutable record with: `BarPosition`, `RolesInvolved` (list of role strings), `DensityLevel` (0.0-1.0), `DurationBars` (decimal), and optional `FillTag`. For anti-repetition, we compare `RolesInvolved` (sorted), `DensityLevel` (within ±0.1 tolerance), `DurationBars`, and `FillTag`. Bar position is excluded since fills in different bars can have the same "shape".

2. How should `ChorusCrashPattern` be represented (absolute bar offsets, beat numbers, pattern per-chorus index)?

**Answer:** Use relative beat positions within a bar (e.g., beats 1.0, 3.0) stored as a sorted list of decimals. This is section-relative so the same pattern can apply across all chorus occurrences. Store as `IReadOnlyList<decimal>` for the beat positions where crashes typically occur.

3. What is the preferred algorithm for `GhostNoteFrequency` (simple sliding-window average, exponential decay, or other)? What window size or decay parameter should be defaulted?

**Answer:** Use a simple sliding-window average with a default window of 8 bars (matching the base `AgentMemory` window size). Store ghost counts per bar in the window and compute average on demand. This is simpler and deterministic.

4. Are memory settings (lookback bars, decay) exposed per-style only, or can they be overridden per-song/part?

**Answer:** Per-style via `StyleConfiguration`. Memory settings are part of style configuration (Story 5.4 defines Pop Rock memory settings). No per-song override is planned for initial implementation—style drives memory behavior.

5. When multiple fills occur in the same bar, which one should `LastFillShape` record (first, last, densest)?

**Answer:** Record the last fill in the bar (by call order). Operators call `RecordFillShape` as they commit decisions, so the final fill wins. This matches chronological execution order.

6. Should memory persist across song sections when editing (in-memory only) or be serializable/persisted with the song/session?

**Answer:** In-memory only for initial implementation. Memory is cleared at generation start and rebuilt during generation. Serialization is out of scope for Story 2.5—focus on runtime behavior.

7. Is thread-safety required for memory operations (concurrent reads/updates) or is generation single-threaded?

**Answer:** Single-threaded. The generator processes bars sequentially; no concurrent access to memory. Thread safety is not required.

8. What is the expected behavior when `AllowConsecutiveSameFill` is false and no alternative fills are available — should the system force a weaker variation or allow the repeat?

**Answer:** Allow the repeat with a logged warning (via diagnostics if enabled). The anti-repetition is advisory—the selection engine applies a penalty, but doesn't hard-block. If only one fill candidate exists, it's selected despite penalty.

9. How should section signatures be keyed (by `SectionType` enum only, or include section index/label)?

**Answer:** By `SectionType` enum only (inherited from base `AgentMemory.GetSectionSignature`). This ensures consistent identity across all choruses, verses, etc. Section index is not included.

## 8. Test Scenario Ideas
- `WhenMemoryIsEmpty_RecordDecision_ThenLastFillBarAndShapeUpdated`
  - Setup: new memory, record a fill on bar 8 with a known FillShape
  - Assert: `LastFillBar == 8` and `LastFillShape` equals the recorded shape
- `AntiRepetition_DoesNotAllowSameFill_InAdjacentSections`
  - Setup: record FillShape A in section X, attempt to apply same FillShape A in next section
  - Assert: memory indicates prohibition or selection engine would receive a repetition penalty flag
- `ChorusCrashPattern_PersistsAcrossChoruses`
  - Setup: record crash hits in chorus occurrences; verify a chorus-pattern summary is produced
- `HatModeHistory_RecordsModeChangesAndOrder`
  - Setup: simulate hat subdivision changes across bars; verify history order and timestamps
- `GhostNoteFrequency_ComputesRollingAverage`
  - Setup: simulate a known sequence of ghost counts per bar and verify computed rolling average matches expected
- `Determinism_SameSequence_ProducesSameMemoryState`
  - Setup: apply identical sequence of record operations twice (same seed/context) and compare serialized memory snapshots

---

// End of pre-analysis for Story 2.5
