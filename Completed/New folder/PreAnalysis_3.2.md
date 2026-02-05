# Pre-Analysis: Story 3.2 — Subdivision Transform Operators (Timekeeping Changes)

## 1) Story Intent Summary
- What: Add five subdivision-transform operators (`HatLift`, `HatDrop`, `RideSwap`, `PartialLift`, `OpenHatAccent`) that change the timekeeping texture (e.g., hi-hat subdivision) for an entire bar when applicable.
- Why: To enable the drummer agent to vary timekeeping density and color across sections and transitions, improving musical expressiveness and section differentiation.
- Who benefits: Generator (richer candidate pool), end-user (more musical, human-like grooves), and developers (reusable operator family for future styles).

## 2) Acceptance Criteria Checklist
1. Implement operators: `HatLiftOperator`, `HatDropOperator`, `RideSwapOperator`, `PartialLiftOperator`, `OpenHatAccentOperator`.
2. Each operator checks `HatSubdivision` in the `DrummerContext` before applying.
3. Each operator respects energy level (e.g., lift at higher energy, drop at lower energy).
4. Each operator generates a full-bar pattern representing the changed hat/ride subdivision.
5. Each operator emits candidates with valid scores and hints so selection/weighting can apply.
6. Scoring considers section transition relevance (operators should score higher at transitions where appropriate).
7. Unit tests verify subdivision changes produce correct patterns.

Notes: Criteria 2–6 are related (eligibility, gating, generation, scoring). Item 4's "full bar" and item 6's "section transition relevance" contain ambiguous specifics (see questions).

## 3) Dependencies & Integration Points
- Depends on completed stories / components:
  - Story 2.1: `DrummerContext` (provides `HatSubdivision`, `CurrentHatMode`, `IsFillWindow`, `EnergyLevel`, `ActiveRoles`, `BeatsPerBar`).
  - Story 2.2: `DrumCandidate` type and `IDrumOperator` contract (candidate shape, `CandidateId`, `Score`, velocity/timing hints).
  - Story 1.4 / Style config: `StyleConfiguration` (may gate operators via `AllowedOperatorIds` and provide style-specific weights).
  - Story 1.3: `OperatorSelectionEngine` (consumes operator candidates and final scores).
  - Story 3.6 (Operator Registry): registration surface for discovery (may be populated by builder later).
  - Groove system types: `GrooveBarContext`, `GrooveOnsetCandidate` mapping utilities (candidate → groove onset conversion).
- Interaction points:
  - `DrummerCandidateSource.GetCandidateGroups(...)` to contribute generated candidates.
  - Physicality filter (story 4.3) — generated full-bar hat patterns may later be filtered.
  - Diagnostics collector (optional) to record why subdivision operator applied or was rejected.
- Provides for future stories:
  - PopRock style tuning (weights/densities) and performance shaping (timing/velocity) can use these operators.
  - Registry entries enabling style gating and unit tests for rhythm texture variation.

## 4) Inputs & Outputs
- Inputs consumed:
  - `DrummerContext` (HatSubdivision, CurrentHatMode, EnergyLevel, IsFillWindow, BarNumber, BeatsPerBar, ActiveRoles)
  - `StyleConfiguration` (allowed operators, operator weights, grid rules)
  - `IAgentMemory` (optional: for gating if repeated subdivision changes should be penalized)
  - RNG streams (for deterministic internal variation within operator outputs)
  - Groove bar metadata (time signature / beats per bar) from `GrooveBarContext`/`BarTrack`
- Outputs produced:
  - A sequence/list of `DrumCandidate` objects covering the bar (hat/ride events) with:
    - `CandidateId`, `OperatorId`, `Role`, `BarNumber`, `Beat`, `Strength`, `VelocityHint`, `TimingHint`, `Score`.
  - Candidates will be translated downstream into `GrooveOnsetCandidate` / `GrooveOnset` by the mapper.
- Configuration read:
  - Style rules (allowed operator ids, role density defaults)
  - Grid/subdivision rules (allowed subdivisions in `StyleConfiguration` / `GrooveSubdivisionPolicy`)
  - Energy thresholds or parameterization (if present in style or provider settings)

## 5) Constraints & Invariants
- Must only apply when the `HatSubdivision` in context indicates the operator makes sense (e.g., do not `HatLift` if already `Sixteenth`).
- Generated candidates must be aligned to the project grid (use `TicksPerQuarterNote` and bar/beat resolution rules).
- Candidate scores must remain within [0.0, 1.0].
- Operators should not produce events for roles absent from `ActiveRoles`.
- Determinism: same seed + same inputs → same candidate outputs (no uncontrolled RNG use).
- Operators must not violate protections applied elsewhere (e.g., must not remove must-hit events — though these operators add alternatives, they must not conflict with anchors).
- Ordering: eligibility check (`CanApply`) → generate candidates for whole bar → score candidates → yield to candidate source/selection engine.

## 6) Edge Cases to Test
- `HatSubdivision == None` or unknown value — ensure operators skip or behave safely.
- `ActiveRoles` excludes hat/ride role — operator must emit no candidates.
- Very short bars or odd meter (e.g., 3/4, 5/4) — ensure "full bar" pattern respects beats per bar.
- Conflicting policies: style disallows operator (AllowedOperatorIds empty/absent) vs policy enabling it — precedence needs clarity.
- Energy at boundary values (0.0, 1.0) and mid thresholds — ensure gating thresholds don't flip inconsistently.
- Interaction with fills: if `IsFillWindow` true, do subdivision operators still apply or are they suppressed in favor of fill operators?
- Repeated application across consecutive bars (oscillation) — ensure memory/penalty could avoid thrashing.
- Null `DrummerContext` or missing subfields — validate inputs and fail fast.
- Generated candidate density exceeds role caps — downstream cap enforcement should prune; ensure operators don't assume unlimited capacity.

## 7) Clarifying Questions

1. What are the canonical `HatSubdivision` enum values and their ordering? Is it Eighth < Sixteenth < None? Are triplet or other subdivisions relevant?

   **Answer:** The enum in `DrummerContext.cs` defines: `None`, `Eighth`, `Sixteenth`. No triplet subdivisions are currently defined. Ordering for density purposes: None < Eighth < Sixteenth.

2. "Generates full bar's worth of changed hat pattern" — should operators always create a candidate per allowed grid position across the bar, or only the minimal changed onsets (e.g., only added 16th on intermediate positions)?

   **Answer:** These operators should generate full-bar replacement patterns (all hat/ride positions for the new subdivision). This follows the "pattern substitution" family model where the selection engine chooses between the full original pattern or the full transformed pattern. HatLift generates all 16th positions, HatDrop generates all 8th positions, etc.

3. How should `PartialLiftOperator` be interpreted precisely (beats 2-4 or last half of bar)? Is the exact pattern parametric or fixed?

   **Answer:** PartialLift should be fixed for initial implementation: 16ths only on beats 3-4 (last half of bar in 4/4). This creates a natural energy build within the bar. The first half remains at current subdivision, last half lifts to 16ths.

4. What numeric thresholds define "higher" vs "lower" energy for gating (explicit cut points or relative scaling)?

   **Answer:** Based on existing operators: energy >= 0.6 is "high" for lift operations; energy <= 0.4 is "low" for drop operations. Mid-range (0.4-0.6) allows either based on context. These thresholds match patterns in MicroAddition operators.

5. Should subdivision operators replace existing hat anchors (pattern substitution) or simply add candidates that compete with anchors? Which precedence is desired?

   **Answer:** Subdivision operators add candidates that compete with anchors in the selection engine. The selection engine chooses based on scores, style weights, and density targets. Operators don't replace; they provide alternatives. Protection system ensures must-hit anchors survive.

6. How should scoring reflect "section transition relevance"? Is there a provided quantitative input (bars until section end/start) or should operators inspect `BarWithinSection` / `BarsUntilSectionEnd`?

   **Answer:** Operators should use `DrummerContext.BarsUntilSectionEnd` and `DrummerContext.IsAtSectionBoundary`. Score boost (+10-20%) when BarsUntilSectionEnd <= 2 for lift/drop changes, as these mark section transitions. Also boost when IsAtSectionBoundary is true.

7. Are there stylistic constraints per `StyleId` (e.g., PopRock disallows `RideSwap` except in bridges)? If so, where are they declared?

   **Answer:** Style constraints are declared in `StyleConfiguration.AllowedOperatorIds`. If empty, all operators are allowed. The `DrummerPolicyProvider` gates operators via the policy's `OperatorAllowListOverride`. For PopRock, all five subdivision operators are allowed; style weights in StyleConfigurationLibrary control frequency.

8. Determinism: which RNG stream(s) should operators use for internal variation (naming convention from Appendix C)?

   **Answer:** Use the deterministic hash approach from existing operators: `HashCode.Combine(barNumber, beat, seed, operatorId)`. This avoids RNG stream dependency and ensures same inputs → same outputs. No RNG calls needed for current operator logic.

9. Unit test expectations: are there canonical reference patterns for 4/4 PopRock (expected 8th vs 16th hat patterns) to assert against?

   **Answer:** 8th pattern: beats 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5 (8 positions). 16th pattern: beats 1, 1.25, 1.5, 1.75, ..., 4.75 (16 positions). Tests should verify these position counts and grid alignment.

10. Compatibility with Physicality/Protection: if a hat/ride event conflicts with a must-hit onset (rare), should the subdivision operator avoid creating that candidate or let later filters prune it?

    **Answer:** Let later filters (PhysicalityFilter) handle conflicts. Operators generate all valid candidates; filtering is a downstream concern. This follows the established pattern in DrummerCandidateSource which applies PhysicalityFilter after candidate generation.

## 8) Test Scenario Ideas (unit test name suggestions)
- `HatLift_WhenHatSubdivisionIsEighthAndEnergyHigh_GeneratesSixteenthPatternForFullBar`
  - Setup: 4/4 bar, `HatSubdivision=Eighth`, `EnergyLevel=0.9`, hat role active. Assert: 16th-grid candidates for every 16th position with valid velocity hints.
- `HatDrop_WhenHatSubdivisionIsSixteenthAndEnergyLow_GeneratesEighthPattern`
  - Setup: 4/4 bar, `HatSubdivision=Sixteenth`, `EnergyLevel=0.1`. Assert: candidates only on 8th positions.
- `RideSwap_WhenStyleDisallowsRide_NoCandidatesProduced`
  - Setup: StyleConfiguration excludes `RideSwap`. Assert: operator skipped.
- `PartialLift_Generates16thsOnSpecifiedBeatsAndRemainsDeterministic`
  - Setup: deterministic seed, assert same candidate set for same inputs and different for different seeds when operator has variation.
- `OpenHatAccent_GeneratesOpenHatOnBeat1And3_WhenEnergyHighAndRolePresent`
  - Setup: verify articulation hint = `OpenHat` and velocity hint in higher range.
- `Operator_SkipsWhenActiveRolesDoNotContainTargetRole`
  - Setup: `ActiveRoles` missing hats; assert zero candidates.
- `Operators_DoNotProduceScoresOutsideZeroToOne`
  - Validate score normalization.
- `Operators_RespectBarLength_InOddMeter`
  - Use 3/4 and 5/4 bars to validate produced candidate positions.

---

// End of pre-analysis for Story 3.2. Keep clarifying Qs concise and resolve before implementation.
