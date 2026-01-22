# Pre-Analysis — Story 2.3: Implement Drummer Policy Provider

## 1) Story Intent Summary
- What: Provide a concise policy layer for the drummer agent that, given a `barContext` and `role`, produces a `GroovePolicyDecision` that drives enabled variations, density/limits, operator allow-lists, timing/velocity biases and other per-bar overrides.
- Why: Policies centralize stylistic and contextual decisioning so the groove system can select candidates that match musical intent, ensure consistency across bars, and allow deterministic reproduction for the same inputs.
- Who benefits: Groove system integrators and downstream candidate selection/renderer (developers and automated generators) and ultimately end-users through more coherent, style-consistent drum tracks.

## 2) Acceptance Criteria Checklist
1. `DrummerPolicyProvider` implements `IGroovePolicyProvider`.
2. `GetPolicy(barContext, role)` returns a `GroovePolicyDecision` containing:
   2.1. `EnabledVariationTagsOverride` computed from style + context + memory.
   2.2. `Density01Override` computed from energy level + section type.
   2.3. `MaxEventsPerBarOverride` taken from style caps.
   2.4. `OperatorAllowList` (operators enabled for this bar based on context).
   2.5. `RoleTimingFeelOverride` when style dictates.
   2.6. `VelocityBiasOverride` when energy dictates.
3. Policy decisions are deterministic for same inputs (seed/context).
4. Unit tests:
   4.1. Same bar context → same policy.
   4.2. Different energy levels → different density overrides.
   4.3. Fill windows → fill operators enabled.

Notes on ambiguities in ACs:
- "Computed from style + context + memory" is high-level; precedence rules among style vs memory vs immediate context are unspecified.
- Exact ranges/units for `Density01Override`, `VelocityBiasOverride`, and `MaxEventsPerBarOverride` are not defined.

## 3) Dependencies & Integration Points
- Depends on (story IDs / features):
  - Stage 1 artifacts: `IGroovePolicyProvider`, `GroovePolicyDecision`, style configuration stories (1.1, 1.4), operator selection engine (1.3), and agent memory (1.2).
  - Drummer-specific artifacts: `DrummerContext` and `DrummerContextBuilder` (2.1), `DrumCandidate` shape (2.2), and `DrummerMemory` (2.5) for memory-informed overrides.
  - Downstream consumers: `DrummerCandidateSource` (2.4) and operator registry (3.6) will read the policy returned by this provider.
- Existing code/types it will interact with:
  - `GrooveBarContext` (input), `GroovePolicyDecision` (output), `StyleConfiguration` / `PopRockStyleConfiguration`, `IAgentMemory` / `DrummerMemory`, deterministic RNG utilities.
- What this story provides for future stories:
  - Centralized per-bar policy decisions that candidate generation and selection use to enable/disable operators, enforce density/caps, and tune timing/velocity. Enables testing of operator gating and physicality interplay.

## 4) Inputs & Outputs
- Inputs consumed:
  - `barContext` / `DrummerContext` (bar index, section type, energy/tension, IsFillWindow, HatSubdivision, BackbeatBeats, etc.).
  - `role` (Kick, Snare, Hat, etc.).
  - `StyleConfiguration` (PopRock or other style rules and caps).
  - `IAgentMemory` / `DrummerMemory` state for repetition penalties and historical decisions.
  - Deterministic seed/RNG stream key where deterministic decisions are required.
  - Optional global settings (diagnostics enabled flag).
- Outputs produced:
  - `GroovePolicyDecision` populated with: `EnabledVariationTagsOverride`, `Density01Override` (0.0–1.0 expected), `MaxEventsPerBarOverride` (int), `OperatorAllowList` (list of operator IDs), `RoleTimingFeelOverride` (qualitative/quantitative descriptor), `VelocityBiasOverride` (numeric bias), and any evidential metadata for diagnostics.
- Configuration/settings read:
  - Style weights, role caps, density defaults, physicality caps, memory lookback settings, and any environment-wide policy toggles (e.g., override for testing).

## 5) Constraints & Invariants
- Determinism: Same `barContext` + `role` + same memory + same seed must always yield identical `GroovePolicyDecision`.
- Non-destructive: Policy provider must not mutate `barContext` or memory state as part of `GetPolicy` (pure/read-only behavior) unless explicitly stated.
- Respect style caps: `MaxEventsPerBarOverride` and `OperatorAllowList` must never violate style-defined hard caps.
- Range invariants: `Density01Override` must be clamped to [0.0, 1.0]; event caps must be >= 0.
- Precedence: There must be a stable precedence order when multiple inputs suggest different overrides (style config vs memory vs explicit context). This is currently unspecified and must be defined.

## 6) Edge Cases to Test
- Missing or partial style configuration (null or incomplete PopRockStyleConfiguration).
- Memory unavailable or empty (first bars of song) — provider should produce reasonable defaults.
- Energy level at boundary values (0.0, 1.0) resulting in min/max densities.
- Conflicting directives: style says low density but context (e.g., chorus + high energy) suggests high density — how tie-breaks occur.
- Empty `OperatorAllowList` for a role — what fallback exists (allow minimal core operators?).
- Fill windows overlapping with other overrides (e.g., IsFillWindow true but memory disallows fills because of recent fill) — expected priority.
- Non-standard time signatures or unknown `role` values.
- Determinism verification when RNG/seed handling is missing or misapplied.
- Concurrency: multiple callers asking policy for same bar simultaneously — read-only behavior must hold.

## 7) Clarifying Questions (with Answers)

1. Precedence: If style config, memory, and immediate context disagree (e.g., style allows fills but memory forbids recent fills), which source wins? Is there a defined precedence order?
   **Answer:** Precedence order is: immediate context > memory > style config. Context wins because it reflects real-time musical needs (e.g., IsFillWindow). Memory takes second priority for anti-repetition. Style config provides defaults when no overrides apply. Fill operators are enabled only when IsFillWindow AND memory allows (no recent fill in memory window).

2. Numeric ranges/formats: Confirm expected value ranges and units for `Density01Override`, `VelocityBiasOverride`, and `RoleTimingFeelOverride` representation.
   **Answer:** Per `GroovePolicyDecision`: `Density01Override` is 0.0–1.0 (clamp required). `VelocityBiasOverride` is an additive int (-127 to +127 practical range). `RoleTimingFeelOverride` uses the `TimingFeel` enum (Ahead, OnTop, Behind, LaidBack).

3. OperatorAllowList semantics: Should the provider return only operator IDs to allow, or also per-operator strength/weight hints? Is an empty list treated as "no operators" or "use defaults"?
   **Answer:** Per `GroovePolicyDecision` contract: list contains only operator IDs. Empty list = all operators allowed (same as null). Weight hints remain in StyleConfiguration.OperatorWeights; the policy layer just gates which operators are eligible for the current bar.

4. Memory access: Is read-only access to `DrummerMemory` sufficient, or should the provider be allowed to record soft decisions for diagnostics? If diagnostics are enabled, what level of internal detail must be returned?
   **Answer:** Read-only access sufficient for GetPolicy (pure query). Recording decisions happens downstream when operators are actually selected (Story 2.4/3.x). Diagnostics are opt-in and separate (Story 7.1).

5. Determinism sources: Should the provider use the global deterministic RNG stream (seed) directly, or should it be purely functional and leave tie-breaking to the selection engine which also gets the seed?
   **Answer:** Policy provider is purely functional — no RNG calls needed. Determinism comes from same inputs → same outputs. RNG-based tie-breaking happens in OperatorSelectionEngine (Story 1.3).

6. Fill gating policy: When `IsFillWindow` is true, must at least one fill operator be enabled, or only if energy/section rules also permit it?
   **Answer:** Fill operators are enabled only when: (a) IsFillWindow is true AND (b) memory doesn't disallow fills (no recent fill in lookback window). Energy doesn't gate fills but affects fill density. At least one fill operator should be eligible when conditions are met.

7. Test expectations: Are the unit tests expected to exercise only pure functional outputs, or also the integration with the style config library (i.e., use live configs vs mocks)?
   **Answer:** Unit tests should use both approaches: mock StyleConfiguration for isolation and boundary testing; live PopRock config for integration verification. This aligns with existing patterns in StyleConfigurationTests.

8. Error handling: How should the provider behave on invalid inputs (null context, unknown role)? Throw or return a safe default `GroovePolicyDecision`?
   **Answer:** Follow C# guidelines: throw `ArgumentNullException` for null context. For unknown roles, return `GroovePolicyDecision.NoOverrides` (safe fallback). This matches DefaultGroovePolicyProvider pattern.

## 8) Test Scenario Ideas (unit-test name style + short setup)
- `GetPolicy_SameBarContext_ReturnsDeterministicPolicy` — same `DrummerContext`, same memory, same seed → identical decisions.
- `GetPolicy_HigherEnergy_IncreasesDensityOverride` — energy 0.2 vs 0.9 produce density differences matching configured modifiers.
- `GetPolicy_FillWindow_EnablesFillOperators` — `IsFillWindow == true` yields operator allow-list containing fill operators.
- `GetPolicy_MemoryDisallowsConsecutiveFills_RespectsMemory` — recent fill in memory causes `EnabledVariationTagsOverride` or `OperatorAllowList` to exclude fill operators.
- `GetPolicy_MissingStyle_UsesSafeDefaults` — null or partial style config returns safe defaults without throwing.
- `GetPolicy_EmptyOperatorAllowList_FallsBackToCoreRoles` — verify fallback behavior when no operators allowed for a role.
- `GetPolicy_RoleTimingFeelOverride_AppliedForStyle` — style-specific timing feel override appears in `GroovePolicyDecision` for appropriate roles/sections.
- `GetPolicy_MaxEventsPerBarClampedToStyleCaps` — provider never returns an override that exceeds style `RoleCaps`.

---

// End of pre-analysis for Story 2.3
