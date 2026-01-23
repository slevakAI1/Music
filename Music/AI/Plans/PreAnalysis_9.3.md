# Pre-Analysis: Story 9.3 — Motif integration with accompaniment (call/response + ducking infrastructure)

## 1) Story Intent Summary
- What: Provide a deterministic, queryable service (`MotifPresenceMap`) and integrate motif-awareness into the drummer agent so accompaniment thins (ducking bias) when motifs are active.
- Why: To make accompaniment make musical room for motifs (hooks/melody) while preserving groove anchors and determinism; this improves mix clarity and musicality.
- Who benefits: Generator (agent selection & policy), end-user (clearer mixes, better hooks), and developers (well-defined integration points for future agents).

## 2) Acceptance Criteria Checklist
1. Part A — `MotifPresenceMap` service:
   1. Constructor consumes `MotifPlacementPlan` and `BarTrack`.
   2. `IsMotifActive(int barNumber, string? role = null) -> bool`.
   3. `GetMotifDensity(int barNumber, string? role = null) -> double` returning [0.0-1.0].
   4. `GetActiveMotifs(int barNumber) -> IReadOnlyList<MotifPlacement>`.
   5. Deterministic for same placement plan.
   6. Role-aware filtering (e.g., Lead/Guitar/Bass).

2. Part B — DrummerAgent integration:
   1. `DrummerPolicyProvider` accepts optional `MotifPresenceMap`.
   2. When motif active in current bar: reduce density target by 10-15%.
   3. Add "MotifPresent" to `EnabledVariationTagsOverride` when motif active.
   4. Update MicroAddition operators to query motif presence in `CanApply()` or `Score()` and reduce their scores:
      - `GhostClusterOperator`: reduce score by 50%.
      - `HatEmbellishmentOperator`: reduce score by 30%.
      - `GhostBeforeBackbeatOperator` / `GhostAfterBackbeatOperator`: reduce score by 20%.
   5. Operators receive `MotifPresenceMap` via `DrummerContext` (extend context with optional field).

3. Ducking rules:
   1. Never affect kick/snare anchors, backbeats, downbeats (groove-protective).
   2. Affect only optional embellishments (ghosts, clusters, hats, similar).
   3. Bounded: max 20% density reduction in any bar.

4. Part C — Infrastructure hooks:
   1. Add `MotifPresenceMap?` field to `AgentContext` (base) so DrummerContext inherits it.
   2. Guitar/Keys/Bass contexts should be able to inherit the field for future use.
   3. Document the pattern in `ProjectArchitecture.md` with suggested reduction ranges.

5. Part D — Tests:
   1. Unit tests for `MotifPresenceMap` covering queries, density, role filtering, empty plan, multiple motifs.
   2. Integration tests for DrummerAgent ensuring determinism and that ducking:
      - Reduces optional event density but preserves anchors.
      - Stays within 20% bound.
      - Causes operators to lower scores as specified.
   3. Snapshot test comparing drums with and without motif presence (anchors identical, optional events reduced).

Ambiguities noted in ACs:
- "reduce density target by 10-15%" vs "bounded: max 20%" — need a precise rule for computing reduction and how it interacts with multiple motifs.
- How `GetMotifDensity` maps to numeric values (what formula? raw note density, normalized by instrument role, or heuristics?).

## 3) Dependencies & Integration Points
- Story dependencies:
  - Story 9.1 (MotifPlacementPlanner) — provides `MotifPlacementPlan` (explicit dependency).
  - Story 9.2 (MotifRenderer) — expected downstream consumer (rendering already implemented).
  - Stage 10 Stories 2.1-2.4 (DrummerContext, DrummerPolicyProvider, CandidateSource) — existing drummer hooks must be extended.
  - Groove system primitives (BarTrack, OnsetGrid, GroovePolicyDecision) — used for density/role info.

- Files / types to interact with (existing):
  - `Generator/Material/MotifPlacementPlan` / `MotifPlacement` types (inputs).
  - `Song/Bar/BarTrack` (timing ruler).
  - `Generator/Material/MotifPresenceMap.cs` (new file to create).
  - `Generator/Agents/Common/AgentContext.cs` (add optional `MotifPresenceMap?`).
  - `Generator/Agents/Drums/DrummerContext.cs` (inherit MotifPresenceMap field).
  - `Generator/Agents/Drums/DrummerPolicyProvider.cs` (density override logic).
  - `Generator/Agents/Drums/Operators/MicroAddition/*` (operators to consult MotifPresenceMap).
  - `ProjectArchitecture.md` (documentation update).

- Provides for future stories:
  - Motif-aware ducking for Guitar/Keys/Bass/Vocals.
  - Cross-role spotlight manager and global density budget.

## 4) Inputs & Outputs
- Inputs:
  - `MotifPlacementPlan` (list of `MotifPlacement` entries).
  - `BarTrack` (to map bar → absolute ticks if needed).
  - `DrummerContext` / `AgentContext` per-bar, per-role.
  - Style/Policy inputs via `DrummerPolicyProvider` and `StyleConfiguration`.
  - Seed not required for presence queries but relevant for determinism of placement plan.

- Outputs:
  - `MotifPresenceMap` instance with query methods.
  - `GroovePolicyDecision` changes (Density01Override, EnabledVariationTagsOverride) from `DrummerPolicyProvider`.
  - Operator behavior changes (score adjustments) for selected MicroAddition operators.

- Configuration/settings read:
  - PopRock (or style) density targets and role caps from `StyleConfiguration`.
  - Policy settings (fill window sizing, energy) from existing `DrummerPolicySettings`.
  - Ducking limits (max percent reduction) — must be specified (global default 20%).

## 5) Constraints & Invariants
- Determinism: same `MotifPlacementPlan` + inputs → identical `MotifPresenceMap` responses.
- Never remove or lower scores for groove anchors: kick/snare anchors, backbeats, must-hit onsets must be preserved.
- Ducking is a bias (probability/score reduction), not a hard block — operators still generate candidates.
- Density reduction per bar must be bounded (hard cap) to 20% maximum.
- Role-aware queries must not leak cross-role motif presence unless explicitly requested (filter by role).
- `GetMotifDensity` must return a normalized value in [0.0, 1.0].
- API must be thread-safe for read-only queries (if generator parallelizes bar processing in future).

Operation ordering that must be observed:
1. MotifPlacementPlanner produces `MotifPlacementPlan`.
2. `MotifPresenceMap` constructed from plan + `BarTrack` before operator generation.
3. `DrummerPolicyProvider.GetPolicy()` consulted when building per-bar policy decisions.
4. Operators consult `DrummerContext.MotifPresenceMap` in `CanApply()`/`Score()` prior to candidate selection.

## 6) Edge Cases to Test
- Empty `MotifPlacementPlan` → `MotifPresenceMap` queries return false/0.0 and no change to policies.
- Motif starts/ends mid-bar or spans multiple bars (partial coverage): how density is computed for that bar.
- Multiple motifs active same bar (same or different roles) — ensure reductions do not stack beyond cap.
- Motif for role X queried by role Y (role filter) — should return false or 0 density.
- Motif overlapping with protected onsets (must-hit) — ensure no protected onsets are removed or reduced.
- Bars already at maximum density/hard caps — ducking bias should not cause negative pruning of anchors.
- Operators that normally run on anchors must not be affected (verify by operator id or candidate protection flag).
- Rapid motif toggling across adjacent bars — memory or smoothing behavior if any.
- Floating-point rounding when applying percentage reductions near bounds.

## 7) Clarifying Questions

1. Exact reduction semantics: should the policy reduce the numeric density target by a flat percent (e.g., density *= 0.85) or subtract an absolute amount? Which is preferred?

   **Answer:** Use multiplicative percentage reduction: `density *= (1.0 - reductionFactor)`. This is cleaner and scales naturally with the base density value. Default `reductionFactor = 0.15` (15% reduction) when motif active.

2. When multiple motifs are active in a bar, do reductions stack, max to bound, or is the highest single reduction used?

   **Answer:** Use the *highest single reduction* from active motifs. Multiple motifs in same bar should not double-penalize. The density from `GetMotifDensity()` returns a single value [0..1] representing overall motif presence intensity; policy applies reduction once based on this density. Final reduction is capped at 20%.

3. The story mentions "reduce density target by 10-15%" and "max 20% bound" — which precise value should be default and is it configurable per style?

   **Answer:** Default = 15% reduction when a motif is active (`MotifDensityReductionPercent = 0.15`). Add to `DrummerPolicySettings` for style-configurability. Max cap = 20% (`MaxMotifDensityReduction = 0.20`). Settings allow future styles to tune these values.

4. How is `GetMotifDensity` computed? Options: normalized note count for motif events per bar, average motif note occupancy, or a static per-motif density attribute from `MotifSpec`?

   **Answer:** Simple heuristic: return `0.5` when any motif is active in the bar (binary presence scaled), or sum the coverage ratio of motifs in that bar clamped to 1.0. For MVP, use count-based: `motifCount / 2.0` clamped to [0..1]. This gives 0.5 for one motif, 1.0 for two or more. Future refinement can use `MotifSpec` attributes.

5. Should `MotifPresenceMap` expose per-role density separate from overall density (the AC suggests role-aware queries, but confirm expected numeric meaning)?

   **Answer:** Yes. `GetMotifDensity(barNumber, role)` returns density for motifs with that `IntendedRole` only. `GetMotifDensity(barNumber, null)` returns overall density (all roles). This matches the `IsMotifActive(barNumber, role?)` pattern.

6. How should operators receive `MotifPresenceMap` — via `DrummerContext` at construction time or via a DI/service locator available to operators?

   **Answer:** Via `AgentContext.MotifPresenceMap` property (nullable). `DrummerContext` inherits this field naturally. Operators access it via `context.MotifPresenceMap?.IsMotifActive(...)`. No DI needed—property-based access keeps operators stateless and testable.

7. Tests: do we need golden snapshots for multiple styles or only PopRock now?

   **Answer:** PopRock only for now. Story scope limits integration to DrummerAgent, which only has PopRock style configuration complete. Future styles will add their own snapshots.

8. Thread-safety expectations: Is single-threaded generation assumed, or must reads be concurrently safe?

   **Answer:** Single-threaded generation is the current model. `MotifPresenceMap` is immutable after construction, so reads are inherently thread-safe if parallelism is added later. No locking needed.

## 8) Test Scenario Ideas (unit/integration names)
- `MotifPresenceMap_EmptyPlan_ReturnsFalseAndZeroDensity`
  - Setup: empty plan; assert IsMotifActive(bar)=false; GetMotifDensity(bar)=0.0.

- `MotifPresenceMap_SingleMotifInBar_IsActiveAndDensityMatches`
  - Setup: one motif covering bar; assert IsMotifActive true and density ≈ expected.

- `MotifPresenceMap_RoleFiltering_ReturnsOnlyRoleSpecificMotifs`
  - Setup: motif tagged as "Lead" and query role="Bass" → false/0.0.

- `DrummerPolicyProvider_WithMotif_ReducesDensityWithinBounds`
  - Setup: supply MotifPresenceMap with active motif; assert Density01Override reduced by configured percent and clamp ≤ 0.2 total reduction.

- `MicroAdditionOperators_ScoreReduced_WhenMotifActive`
  - Setup: operator Score() called with context containing MotifPresenceMap active → score falls by specified percent (50%, 30%, 20%).

- `DrummerGeneration_WithAndWithoutMotif_SnapshotComparison`
  - Setup: deterministic seed; generate drum part with MotifPresenceMap and without; verify anchors identical and optional events reduced in motif case.

- `MultipleMotifs_DoesNotExceedMaxDucking`
  - Setup: multiple motifs active in a bar; ensure computed density reduction never exceeds 20%.

- `AnchorsNeverRemoved_WhenMotifPresent`
  - Setup: anchors flagged as `IsMustHit` or similar; run selection/pruning; assert anchors present in final onsets in both cases.

- `MotifPresenceMap_Determinism_SamePlanSameResult`
  - Setup: same `MotifPlacementPlan` and BarTrack across runs; call queries multiple times and assert identical results.

---

// Notes: Implementation decisions required for several numeric semantics (density mapping and reduction aggregation).
// Clarifications requested above should be resolved before implementation to avoid rework.
