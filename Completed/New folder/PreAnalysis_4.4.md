# Pre-Analysis: Story 4.4 — Add Overcrowding Prevention

## 1) Story Intent Summary
- What: Add deterministic overcrowding prevention to the drum physicality filter so bars exceeding hit caps are pruned deterministically.
- Why: Prevents unplayable/muddy bars by enforcing hard caps on hits per beat/bar/role; preserves playability and mix clarity.
- Who: Developers implementing `PhysicalityFilter`/rules, the groove/agent selection pipeline, and ultimately listeners/end-users via better musical output.

## 2) Acceptance Criteria Checklist
1. Add new fields to `PhysicalityRules`:
   1. `MaxHitsPerBeat` (default: 3)
   2. `MaxHitsPerBar` (default: 24 for 16th grid in 4/4)
   3. `MaxHitsPerRolePerBar` (Dictionary<string,int>)
2. Implement deterministic pruning inside `PhysicalityFilter`:
   1. When caps exceeded, prune lowest-scored candidates first.
   2. Never prune protected onsets (e.g., must-hit / protected tags).
   3. Use a deterministic tie-break when scores equal.
3. Add unit tests verifying overcrowded bars are thinned correctly.

Notes: Grouped criteria: (1) config additions, (2) pruning behavior, (3) tests. Ambiguous items are highlighted in section 2.1 below.

## 2.1 Ambiguous or unclear acceptance criteria
- The default numbers are suggested but could vary by meter/grid; clarity needed on whether defaults are global or grid-dependent.
- Definition of "hit" is not explicit (candidate vs final onset vs overlapping sustained events).
- Precise deterministic tie-break rule (operatorId/candidateId ordering) is not stated.

## 3) Dependencies & Integration Points
- Depends on completed stories: 4.1 (Limb Model), 4.2 (Sticking Rules), 4.3 (Physicality Filter basic behavior).
- Integrates with:
  - `Generator/Agents/Drums/Physicality/PhysicalityFilter.cs`
  - `Generator/Agents/Drums/Physicality/PhysicalityRules.cs`
  - `DrumCandidate` / `GrooveOnsetCandidate` and mapping layers (`DrumCandidateMapper`)
  - `DrummerCandidateSource` and downstream Groove selection pipeline
  - Protection tags and `GrooveOnset.IsProtected` / `IsMustHit` semantics
  - Diagnostics: `GrooveBarDiagnostics` / `GrooveDiagnosticsCollector` for prune events
- Provides: deterministic overcrowding pruning behavior for later stories (PopRock style caps, selection engine interactions, and snapshot tests).

## 4) Inputs & Outputs
- Inputs:
  - Candidate set(s) for a bar (list of `DrumCandidate` or `GrooveOnsetCandidate` groups)
  - `PhysicalityRules` configuration (max caps, role caps, strictness)
  - Protection metadata on candidates/onsets
  - Scores assigned by operators/selection engine
- Outputs:
  - Pruned candidate lists (respecting caps and protections)
  - Prune diagnostics (which candidates removed, reasons, preserved protected items)
- Config/settings read:
  - `MaxHitsPerBeat`, `MaxHitsPerBar`, `MaxHitsPerRolePerBar` and strictness level
  - Role→limb mapping (for interplay with limb conflicts)

## 5) Constraints & Invariants
- Must never prune protected onsets (`IsProtected` / `IsMustHit`).
- Pruning must be deterministic: same inputs→same pruned set.
- Pruning order: apply limb conflict & sticking validation first, then overcrowding pruning (ordering must be confirmed).
- Hard limits must be enforced per rule (per-beat, per-bar, per-role per-bar).
- When pruning, preserve groove anchors (kick/snare/backbeat) even if caps exceeded.

## 6) Edge Cases to Test
- Empty candidate lists (no-op).
- All candidates are protected and exceed caps: behavior must be defined (cannot prune protected; result may exceed cap).
- Some candidates have null/NaN/identical scores — ensure deterministic tie-break applied.
- Candidates that span bar boundaries (long durations) — clarify whether they count toward multiple bars.
- Odd meters or different subdivision grids (3/4, 5/4, 6/8) — how `MaxHitsPerBar` maps to different meters.
- Role caps conflicting with total caps (sum role caps < total cap) — how enforced.
- Simultaneous multi-role events at same beat (e.g., kick + snare + hat) — counting per-beat limit should allow multi-limb hits.
- Protected vs unprotected mixed on a beat where caps would force pruning; ensure protected preserved.
- Strictness modes (Loose/Normal/Strict) interactions with pruning semantics.

## 7) Clarifying Questions

1. Definition: Does a "hit" equal an individual candidate/onset, or do sustained events (long notes) count differently? Do overlapping hits (same onset time, different roles) each count separately?

   **Answer:** A "hit" equals one candidate/onset. Each candidate counts as exactly one hit regardless of duration. Multiple roles at the same beat each count separately toward per-beat caps (e.g., kick+snare+hat at beat 1 = 3 hits for that beat).

2. Meter/grid sensitivity: Are default caps (3 per beat, 24 per bar) fixed, or should they scale by beats-per-bar and subdivision (e.g., 6/8, 3/4)?

   **Answer:** Defaults are fixed values. Different meters use the same cap values; the caller (style configuration) can override via `PhysicalityRules` if needed. This keeps the filter simple and deterministic.

3. Tie-break determinism: What exact tie-break ordering should be used when scores equal? (operatorId asc, candidateId asc, bar/beat order?)

   **Answer:** Established in Story 4.3: Score desc → OperatorId asc → CandidateId asc. This ordering is already implemented in `PhysicalityFilter.OrderByScoreForPruning()`.

4. Protected items precedence: Are `IsProtected`, `IsMustHit`, and `IsNeverRemove` equivalent for pruning purposes? Which flags must be preserved at all costs?

   **Answer:** For overcrowding pruning, the single `DrumCandidateMapper.ProtectedTag` is the authoritative check. Candidates with this tag are never pruned. The tag encompasses must-hit and never-remove semantics.

5. Interaction with earlier filters: Should overcrowding pruning run before or after limb conflict and sticking-rule pruning? Are prune decisions reversible by later stages?

   **Answer:** Overcrowding pruning runs FIRST (as a density cap), then limb conflict and sticking validation run on the reduced set. This ensures expensive conflict detection works on a manageable candidate count. Prune decisions are final.

6. Diagnostics: What level of detail is required in prune diagnostics (full candidate metadata, score, reason) and where should it be recorded?

   **Answer:** Use existing `GrooveDiagnosticsCollector.RecordPrune(candidateId, reason, wasProtected)`. Reason strings should indicate the cap type: "Overcrowding:MaxHitsPerBeat", "Overcrowding:MaxHitsPerBar", "Overcrowding:MaxHitsPerRole:{role}".

7. Behavior when only protected onsets exceed caps: Should the system allow caps to be exceeded, or attempt alternative adjustments (shift timing, reduce velocity)?

   **Answer:** Allow caps to be exceeded. Protected candidates are never pruned; if they alone exceed the cap, the output exceeds the cap. This preserves groove anchors at all costs.

8. Role caps semantics: Are role names normalized to the `GrooveRoles` constants? What happens if a role is missing in `MaxHitsPerRolePerBar`?

   **Answer:** Role names must match exactly (case-sensitive) as strings. If a role is missing from `MaxHitsPerRolePerBar`, it has no role-specific cap (unlimited for that role). The `PhysicalityRules.GetRoleCap(role)` method returns null for uncapped roles.

## 8) Test Scenario Ideas
- `PhysicalityFilter_EmptyCandidates_NoChange` — empty input returns empty output.
- `PhysicalityFilter_RespectsProtectedOnsets_WhenCapsExceeded` — protected items are never pruned.
- `PhysicalityFilter_PrunesLowestScore_WhenMaxHitsPerBarExceeded` — given N>MaxHits, lowest scored candidates removed deterministically.
- `PhysicalityFilter_DeterministicTieBreak_OnEqualScores` — two runs with same seed/order produce identical pruning when scores tie.
- `PhysicalityFilter_RoleCaps_Enforced` — role-specific caps prune across roles without affecting others.
- `PhysicalityFilter_CombinedCaps_Enforced` — combined per-beat and per-bar caps applied correctly.
- `PhysicalityFilter_StrictnessModes_RespectDiagnostics` — verify Normal vs Loose behavior and diagnostic records.
- `PhysicalityFilter_ProtectedExceedsCap_AllProtectedKept` — when only protected exceed cap, verify behavior defined (test expects either allow-exceed or defined fallback).

## Determinism Verification Points
- Inputs ordered lists vs unordered sets should produce same deterministic outcome when contents equal.
- Tie-break ordering must be explicitly asserted in tests (e.g., operatorId then candidateId).

---

// End of pre-analysis for Story 4.4
