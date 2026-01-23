# Pre-Analysis — Story 3.5: Style Idiom Operators (Pop Rock Specifics)

## 1. Story Intent Summary
- What: Add five Pop-Rock-specific operator definitions that generate style-appropriate drum candidates (backbeat push, rock kick syncopation, chorus crash pattern, verse simplification, bridge breakdown).
- Why: Ensure the drummer agent can produce idiomatic Pop Rock moves that give the generated groove recognizable genre identity and section-aware behavior.
- Who: Beneficiaries include the generator (richer style outputs), product stakeholders (more authentic Pop Rock tracks), and developers (clear style-gated operators to tune and test).

## 2. Acceptance Criteria Checklist
1. Implement these operators (5 total):
   1. `PopRockBackbeatPushOperator` — snare slightly ahead for urgency
   2. `RockKickSyncopationOperator` — rock-style kick anticipations (4&→1)
   3. `PopChorusCrashPatternOperator` — consistent crash pattern for choruses
   4. `VerseSimplifyOperator` — thin out verse grooves for contrast
   5. `BridgeBreakdownOperator` — half-time or minimal pattern for bridges
2. Ensure operators are Pop Rock specific (style-gated).
3. For each operator:
   - Only applies when `StyleId == "PopRock"`.
   - Uses section type for relevance.
   - Generates style-appropriate candidates.
4. Unit tests: verify style gating works.

Notes: The above list groups operator creation, gating, operator behavior, and tests. Ambiguities are called out below.

## 3. Dependencies & Integration Points
- Dependent stories (by ID):
  - `1.4` (Style Configuration) — for `StyleId`, weights and gating rules.
  - `2.1` (Drummer Context) — for section type, hat mode, active roles.
  - `2.2` (Drum Candidate) — operator-produced candidate shape/schema.
  - `3.6` (Operator Registry) — registration and discovery of operators.
  - Potential: `2.5` (Drummer Memory) — if anti-repetition should affect idiom usage; `4.x` (Physicality) — to ensure playability.
- Integration points / existing types the story will interact with:
  - `StyleConfiguration` / `StyleConfigurationLibrary` (reads `StyleId`, weights, gating)
  - `DrummerContext` / `AgentContext` (section type, energy, active roles)
  - `DrumCandidate` (candidate output)
  - Operator discovery/registry and selection pipeline (where candidates are collected and scored)
- What this story provides for future stories:
  - Pop Rock-specific operator implementations for tuning, tests, and inclusion in the operator registry and PopRock style weights.

## 4. Inputs & Outputs
- Inputs (consumed):
  - `DrummerContext` / `AgentContext` (bar number, section type, energy, active roles)
  - `StyleConfiguration` (StyleId == "PopRock", allowed operators/weights)
  - `IAgentMemory` (optional: to avoid repeating idiomatic fills)
  - Deterministic RNG streams / seed (for variation)
- Outputs (produced):
  - Zero-or-more `DrumCandidate` instances per operator per bar (with operator id, candidate id, role, timing/velocity hints, score)
  - Diagnostic hints (operator id, rationale) for tests/diagnostics (if diagnostics enabled)
- Configurations/settings read:
  - `StyleId` and style-specific gating/weights from `StyleConfiguration` (PopRock)
  - Section-aware density/role caps (from style or global rules)

## 5. Constraints & Invariants
- Operators must only be active when `StyleId == "PopRock"` (style-gating invariant).
- Operators must respect section type relevance (e.g., chorus vs verse vs bridge).
- Determinism: same seed + same context → same candidates/variation.
- Do not violate physicality/sticking/role caps (either here or later in pipeline).
- Hard limits likely enforced elsewhere but relevant: max events per bar, role caps (kick/snare/hat limits).
- Order constraint (logical flow): operator generation → physicality/sticking/overcrowding checks → candidate grouping/selection.

## 6. Edge Cases to Test
- Style mismatch: `StyleId != "PopRock"` — operators must produce no candidates or be inactive.
- Section mismatch: operator requested in inappropriate section (e.g., chorus-only operator invoked in verse) — ensure it respects section gating.
- Disabled roles: role (e.g., Crash) not active in `ActiveRoles` — operator must not produce hits for that role.
- Memory interactions: recent identical idiom used in previous bars with `AllowConsecutiveSameFill == false` — operator should be suppressed or penalized.
- Overlap/conflict: multiple operators produce candidates for same onset causing limb conflict or overcrowding.
- Empty output: operator returns zero candidates — ensure pipeline handles gracefully.
- Low/high energy extremes: operators that should only apply at certain energy ranges either generate no candidates or scale behavior.
- Determinism: same seed/inputs produce identical candidate sets; different seed produces variation.
- Missing style configuration: null or incomplete `StyleConfiguration` for PopRock — define expected behavior (fail, no-op, or fallback).

## 7. Clarifying Questions
1. Precise gating semantics: should operators be registered globally but return no candidates when style != PopRock, or should they be excluded from the registry for non-PopRock styles?

Answer 1: Register globally for discovery but implement style-gating in each operator's CanApply/GenerateCandidates. Registry returns all operators; `GetEnabledOperators(style)` in `DrumOperatorRegistry` filters by `StyleConfiguration` when the pipeline requests enabled operators.

2. For `PopRockBackbeatPushOperator`: what is the quantitative timing offset for "slightly ahead" (ticks or ms) and does it vary by energy or section?

Answer 2: Use a small tick offset consistent with project timing constants (default: -6 ticks = slightly ahead). Allow scaling by energy (energy>0.7 -> -8 ticks, energy<0.3 -> -4 ticks). Exact tuning belongs to performance stories; unit tests should assert offsets within [-10,-4].

3. For `RockKickSyncopationOperator`: which exact syncopation patterns are desired (e.g., 4&→1 only, or other anticipations), and how often should they occur (weight/caps)?

Answer 3: Primary pattern is 4& → 1 (anticipation on the "and" of four). Secondary variants may include 2& or 3& sparsely. Frequency controlled by style weight and role caps; default expectation: ~15-25% of eligible bars (tunable via PopRock weights). Tests should assert presence on 4& when enabled.

4. For `PopChorusCrashPatternOperator`: which beats in the chorus should receive crashes (every 1, every 2 bars, only phrase starts)? Should choruses have a configurable pattern or fixed one?

Answer 4: Default: crash on beat 1 of chorus bars and optionally on phrase starts every 2 bars. Make pattern configurable via PopRockStyleConfiguration (CrashPattern enum or list) but default to crash-on-1 with an option to enable "every-other-bar" behavior.

5. For `VerseSimplifyOperator`: what constitutes "thin out" quantitatively (percentage fewer hits, specific roles muted, or velocity reduction)?

Answer 5: Apply one or more of: reduce role density targets by 20% for verse, lower velocity hints by 10-20, and prefer disabling embellishment operators. PopRock config should expose a "VerseSimplifyIntensity" (default 0.2 = 20% thinning).

6. For `BridgeBreakdownOperator`: is half-time always preferred, or should it be configurable to produce "minimal" vs "half-time" variants?

Answer 6: Make it configurable: default to half-time variant (snare on beat 3) for musical consistency but allow a "minimal" mode that mutes non-essential roles and reduces density. Expose choice in PopRockStyleConfiguration Bridge settings.

7. How should these operators interact with memory/anti-repetition rules, especially for chorus crash consistency (do we want repetition here)?

Answer 7: Chorus crash placement is a persistent section signature (allowed repetition across chorus bars). Memory should treat chorus-crash as lower-penalty (SectionSignatureStrength) while fill operators remain high-penalty to avoid repetition. Operators must consult DrummerMemory before emitting protected repeated shapes.

8. What are the determinism/RNG stream requirements for operator variation (which RNG keys or streams should operators use)?

Answer 8: Operators should use dedicated RNG streams from `RandomPurpose` (e.g., `DrummerFillVariation`, `DrummerOperatorSelection`, `DrummerTieBreak`). Document which stream each operator uses for variation; tests should seed those streams for determinism.

9. What level of diagnostics are required for unit tests (do tests assert diagnostics entries exist, or only observable candidate outputs)?

Answer 9: Unit tests should primarily assert observable candidate outputs (roles, beats, offsets). Additional tests may assert diagnostics entries exist when diagnostics are explicitly enabled by the test fixture, but diagnostics are optional and zero-cost when disabled.

10. Are there explicit priority rules if multiple style-idiom operators generate overlapping candidates in the same bar?

Answer 10: Priority resolution is handled by selection engine: finalScore = baseScore * styleWeight * (1.0 - repetitionPenalty) and deterministic tie-breaks (score desc → operatorId asc → candidateId asc). Operators should not assume exclusive priority; selection engine enforces caps and pruning.

## 8. Test Scenario Ideas
- `Operator_IsInactive_When_StyleIsNotPopRock` — verify each style-idiom operator produces no candidates when style != PopRock.
- `PopRockBackbeatPush_Generates_SlightlyAhead_Snare_In_Chorus` — setup chorus context and assert snare timing offset is within expected small-ahead range.
- `RockKickSyncopation_Generates_Anticipation_On_4And` — ensure a kick candidate appears on the 4& position when enabled.
- `PopChorusCrashPattern_Applies_In_Chorus_Sections` — verify crash candidates appear in chorus bars per pattern.
- `VerseSimplify_Reduces_HitDensity_In_Verify` — compare candidate counts/role density between verse and default bars.
- `BridgeBreakdown_Produces_HalfTime_Or_Minimal` — assert generated candidates reflect reduced density or half-time feel in bridge.
- `Operators_Respect_ActiveRoles` — deactivate a role in context and verify no candidates for that role.
- `Operators_Respect_Memory_Penalties` — pre-fill memory with a recent idiom and verify suppression/penalty.
- `Operator_Determinism_SameSeed_SameOutput` — run generation twice with same seed and assert candidate sets identical.
- `Operator_Variation_DifferentSeed_DifferentOutput` — run with different seeds and assert some difference in candidates.

---

// End of pre-analysis for Story 3.5
