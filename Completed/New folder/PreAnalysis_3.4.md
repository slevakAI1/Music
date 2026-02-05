# Pre-Analysis — Story 3.4: Pattern Substitution Operators

## 1. Story Intent Summary
- What: Implement operators that swap entire groove patterns (backbeat variants, kick patterns, half-time/double-time feels) to create distinct section character through wholesale pattern replacement rather than additive variation.
- Why: Pattern substitution provides bold, section-defining changes that are more dramatic than micro-additions or subdivision changes, enabling clear musical contrast between verses, choruses, and bridges while staying within genre conventions.
- Who benefits: Generator (produces section-aware pattern changes), drummer agent (completes operator family coverage), end-users/listeners (experience clear section identity and dynamic contrast).

## 2. Acceptance Criteria Checklist
1. Implement 4 operators:
   1. `BackbeatVariantOperator` — snare articulation variants (flam, rimshot, offset)
   2. `KickPatternVariantOperator` — kick pattern swaps (four-on-floor, syncopated, half-time)
   3. `HalfTimeFeelOperator` — half-time snare pattern (backbeats on 3 only instead of 2 and 4)
   4. `DoubleTimeFeelOperator` — double-time feel with denser kick pattern
2. Each operator behavior:
   - Checks section type and energy level before applying
   - Generates complete pattern replacement for the entire bar
   - Uses style configuration to determine allowed variants
   - Scores based on section-change relevance (higher at boundaries)
3. Memory penalty warning: These operators should be used sparingly (high memory penalty)
4. Unit tests: pattern variants generate correctly

Notes: All operators in this family generate "replacement" patterns (not additive like MicroAddition or partial like SubdivisionTransform). The memory penalty warning suggests these should have lower frequency/higher repetition cost than other operator families.

Ambiguous/unclear ACs:
- "Complete pattern replacement for the bar" — does this mean replacing ALL roles (kick+snare+hat) or just the relevant role(s)?
- "Used sparingly (high memory penalty)" — what is the specific penalty multiplier compared to other operators?
- Half-time feel "2 and 4 become 3 only" — what happens to other beats? Are kicks adjusted too?
- Double-time feel "with double kicks" — does this double ALL kick positions or add specific patterns?

## 3. Dependencies & Integration Points
- Depends on completed stories / components:
  - Stage 1 common contracts: `IMusicalOperator`, `AgentContext`, `OperatorFamily` (Story 1.1)
  - Drummer context and builder (Story 2.1)
  - Drum candidate type and role enums (Story 2.2)
  - Drummer memory (Story 2.5) for high-penalty tracking
  - Operator registry/discovery (Story 3.6) to register these operators
  - Style configuration (Story 1.4, Story 5.1) for allowed variants and weights
  - Operator selection & memory weighting (Story 1.3) for penalty application
  - MicroAddition, SubdivisionTransform, PhrasePunctuation operators (Stories 3.1-3.3) as reference patterns

- Integration touchpoints in code:
  - `DrummerContext` (read: `SectionType`, `EnergyLevel`, `BarNumber`, `BeatsPerBar`, `BackbeatBeats`)
  - `DrumCandidate` (produced candidates with role, position, strength, score)
  - `StyleConfiguration` (read: `AllowedOperatorIds`, `OperatorWeights` for variants)
  - `DrummerMemory` (record pattern swaps with high penalty; anti-repetition)
  - `OperatorSelectionEngine` (operators contribute scored candidates)
  - `DrumOperatorRegistry` (register all 4 operators in PatternSubstitution family)

- Provides for future stories:
  - Complete PatternSubstitution operator family for registry (Story 3.6)
  - Pattern swap capability for Pop Rock style tuning (Stage 5)
  - Section-identity anchors for diagnostics (Stage 7)

## 4. Inputs & Outputs
- Inputs (consumed):
  - `DrummerContext` / `AgentContext` (bar number, section type, energy level, backbeat beats, beats per bar)
  - `StyleConfiguration` (allowed operator IDs, variant restrictions, operator weights)
  - `DrummerMemory` (recent pattern swaps, section signatures)
  - RNG/Seed for deterministic variant selection
  - Active roles from context (`ActiveRoles` set)
  - Backbeat positions for backbeat variant logic

- Outputs (produced):
  - Collections of `DrumCandidate` representing full-bar pattern replacements
  - Candidates with populated: `CandidateId`, `OperatorId`, `Role`, `BarNumber`, `Beat`, `Strength`, `VelocityHint`, `TimingHint`, `ArticulationHint` (for variants), `Score`
  - Pattern "fingerprints" for memory tracking (section signature updates)
  - Diagnostic entries describing pattern selection reasoning

- Configuration/settings read:
  - Style-specific allowed variants (e.g., PopRock allows four-on-floor, disallows blast beats)
  - Operator weights (PatternSubstitution family likely has lower weights than MicroAddition)
  - Section-based applicability rules (e.g., half-time feel only in bridges)
  - Memory penalty multiplier (how much higher penalty than other families)

## 5. Constraints & Invariants
- Pattern replacement invariant: operators generate COMPLETE bar-length patterns, not individual additive hits
- Determinism invariant: same seed + context → identical pattern variant selection
- Memory invariant: pattern swaps incur higher repetition penalty than other operator families (avoid robotic alternation)
- Section-awareness invariant: pattern swaps should respect section type (e.g., half-time in bridges, four-on-floor in choruses)
- Style-gating invariant: variants must be allowed by `StyleConfiguration.AllowedOperatorIds` and variant-specific rules
- Backbeat preservation: even with pattern swaps, backbeat identity should be recognizable (unless intentionally altered like half-time)
- Physicality compatibility: generated patterns must still pass physicality filter (limb feasibility, sticking)
- Coordination with other operators: pattern substitution should suppress or coordinate with subdivision/micro-addition operators to avoid conflicts
- Ordering: pattern substitution likely has priority/influence over other families (establishes the "base" groove for a section)

## 6. Edge Cases to Test
- Empty or invalid `DrummerContext` fields (null section type, energy out of range)
- Style configuration disallows ALL variants for this family (what happens?)
- Pattern swap requested but recent memory shows same pattern just used (high penalty should block)
- Section boundary vs mid-section: pattern swaps should score higher at boundaries but may still apply mid-section
- Energy extremes:
  - Energy = 0.0: should pattern swaps apply? (likely suppressed)
  - Energy = 1.0: which variants are more appropriate?
- Time signature edge cases:
  - 3/4: how does four-on-floor adapt? Half-time feel?
  - 5/4, 7/4: backbeat positions undefined or non-standard
- Conflicting operators:
  - HatLiftOperator (subdivision) + KickPatternVariantOperator (pattern swap) → which wins?
  - Should pattern substitution suppress other families in the same bar?
- Determinism with multiple valid variants: tie-breaking must be stable
- All active roles missing (no kick, no snare): graceful degradation or skip?
- Consecutive pattern swaps across bars: memory should prevent "ping-pong" effect
- Half-time feel combined with double-time feel: contradiction, should be mutually exclusive
- Articulation hints (rimshot, flam) on backbeat variants: ensure articulation enum supports these

## 7. Clarifying Questions
1. "Complete pattern replacement for the bar" — does this replace ALL roles (kick+snare+hat) or just the primary role(s) relevant to the pattern (e.g., kick-only for KickPatternVariant)?

**Answer:** Each operator replaces only its primary role(s). BackbeatVariantOperator replaces snare backbeats only. KickPatternVariantOperator replaces kick pattern only. HalfTimeFeelOperator replaces snare (backbeat on 3 only) and optionally includes a complementary sparse kick pattern. DoubleTimeFeelOperator adds denser kick pattern. Other roles (hats, cymbals) remain unaffected by these operators.

2. What is the specific memory penalty multiplier for PatternSubstitution operators compared to other families? (e.g., 2x, 5x?)

**Answer:** PatternSubstitution operators use the standard memory penalty mechanism but have a higher base weight (1.5x-2.0x compared to MicroAddition). This is handled via scoring: operators produce lower base scores (0.4-0.6) compared to MicroAddition (0.6-0.8), making them naturally less frequent. The repetition penalty from AgentMemory still applies normally.

3. For HalfTimeFeelOperator: "2 and 4 become 3 only" — should kicks also adjust to half-time feel, or just snare?

**Answer:** HalfTimeFeelOperator primarily affects snare (backbeat on beat 3 only instead of 2 and 4). A complementary sparse kick pattern is generated (beat 1 only, or beats 1 and 3 only depending on energy) to complete the half-time feel. This operator generates both kick and snare candidates as a coordinated pattern.

4. For DoubleTimeFeelOperator: does "double kicks" mean doubling the frequency of existing kick pattern, or introducing a specific double-time pattern (e.g., 16th note kicks)?

**Answer:** DoubleTimeFeelOperator generates a specific double-time kick pattern with kicks on every beat (four-on-floor) plus 8th note offbeats at high energy (not 16th notes - that would be unrealistic for most styles). This creates the driving "double-time" feel without becoming physically impossible.

5. Should pattern substitution operators suppress other operator families in the same bar, or can they coexist?

**Answer:** Pattern substitution operators can coexist with other families. The selection engine handles candidate scoring - pattern substitution candidates compete with other candidates normally. However, operators set appropriate scores so that when a pattern substitution is selected, its complete pattern is preferred over piecemeal additions from other families.

6. Are pattern swaps section-locked (once set for a section, they apply to all bars in that section), or can they vary per-bar within a section?

**Answer:** Pattern swaps are per-bar decisions, but the memory system and scoring encourage consistency within a section. Higher scores at section boundaries make initial pattern swaps likely at section starts. Memory penalties discourage changing patterns mid-section. This creates natural section-locked behavior without hardcoded enforcement.

7. For BackbeatVariantOperator: does "offset backbeat" mean timing offset (slightly early/late) or pitch offset (cross-stick, rim)?

**Answer:** "Offset backbeat" refers to timing offset (slightly early/behind the grid) to create urgency or laid-back feel. This is implemented via TimingHint. Pitch/technique variations (rimshot, sidestick, flam) are separate articulation variants handled by ArticulationHint. The operator generates candidates for different articulation types; timing offset is a separate property.

8. What is the priority/precedence if multiple pattern substitution operators want to apply in the same bar?

**Answer:** Multiple pattern substitution operators can apply simultaneously since they target different roles (BackbeatVariant→Snare, KickPatternVariant→Kick). HalfTimeFeel and DoubleTimeFeel are mutually exclusive (checked via CanApply guards based on energy thresholds). Standard operator selection scoring and caps handle conflicts.

9. Should pattern substitution operators coordinate with anchor layer (GroovePresetDefinition.AnchorLayer), or do they replace it?

**Answer:** Pattern substitution operators generate candidates that compete with anchor layer onsets during selection. They don't explicitly replace the anchor layer - the groove selection engine handles which candidates are selected. In practice, when a pattern substitution is selected, its candidates should score high enough to be preferred over sparse anchor patterns.

10. How should style configuration specify allowed variants? (e.g., tags like "FourOnFloor", "Syncopated", or explicit operator+variant IDs?)

**Answer:** Style configuration uses AllowedOperatorIds to gate operators. Variant-specific filtering within an operator uses hardcoded section/energy rules (e.g., four-on-floor in Chorus, syncopated in Bridge). Future enhancement could add variant tags to StyleConfiguration, but for Story 3.4, internal operator logic handles variant selection based on section type and energy.

## 8. Test Scenario Ideas (unit test name suggestions)
- `When_EnergyLow_PatternSubstitutionOperators_AreNotApplied`
  - Setup: Energy < threshold (e.g., 0.3), all pattern substitution operators
  - Expect: No candidates generated (or very low scores)

- `BackbeatVariant_GeneratesFlam_WhenStyleAllows`
  - Setup: `StyleConfiguration` allows flam variant, appropriate energy/section
  - Expect: BackbeatVariantOperator generates candidates with `ArticulationHint = Flam`

- `BackbeatVariant_GeneratesRimshot_WhenInVerse`
  - Setup: Section = Verse, style allows rimshot
  - Expect: Candidates with `ArticulationHint = Rimshot` on backbeat positions

- `KickPatternVariant_GeneratesFourOnFloor_WhenInChorus`
  - Setup: Section = Chorus, style allows four-on-floor
  - Expect: Kick candidates on all downbeats (1, 2, 3, 4)

- `KickPatternVariant_GeneratesSyncopated_WhenInBridge`
  - Setup: Section = Bridge, style allows syncopated
  - Expect: Kick candidates on offbeats/syncopated positions

- `HalfTimeFeel_MovesBackbeat_ToThree_Only`
  - Setup: Half-time feel enabled, 4/4 time
  - Expect: Snare candidate on beat 3 only (not 2 or 4)

- `HalfTimeFeel_AdjustsKicks_ToMatchFeelIfNeeded`
  - Setup: Half-time feel, check if kicks adapt
  - Expect: Kick pattern reflects half-time density/placement

- `DoubleTimeFeel_DoublesKickDensity`
  - Setup: Double-time feel enabled
  - Expect: Kick candidates at higher frequency (e.g., 8th or 16th density)

- `PatternSubstitution_HasHigherScore_AtSectionBoundary`
  - Setup: Same operator, two contexts: mid-section vs boundary
  - Expect: Boundary context produces higher scores

- `PatternSubstitution_UsesStyleConfiguration_ToFilterVariants`
  - Setup: Style disallows specific variant (e.g., no four-on-floor)
  - Expect: That variant is not generated

- `PatternSubstitution_AvoidsSamePattern_InConsecutiveBars`
  - Setup: Memory shows pattern just used, same operator applies again
  - Expect: High memory penalty suppresses selection OR different variant chosen

- `PatternSubstitution_IsDeterministic_ForSameSeed`
  - Setup: Fixed seed, same context, multiple runs
  - Expect: Identical pattern variant chosen each time

- `PatternSubstitution_GeneratesFullBar_NotSingleHits`
  - Setup: Any pattern substitution operator
  - Expect: Multiple candidates across the bar (full pattern), not single isolated hit

- `HalfTimeFeel_And_DoubleTimeFeel_AreMutuallyExclusive`
  - Setup: Both operators apply in same bar
  - Expect: Only one is selected (or conflict resolution rule)

- `PatternSubstitution_AdaptsTo_OddMeters_3Over4`
  - Setup: 3/4 time signature, pattern substitution operator
  - Expect: Pattern adapts to 3-beat structure (e.g., kicks on 1, 2, 3 for four-on-floor equivalent)

- `PatternSubstitution_AdaptsTo_OddMeters_5Over4`
  - Setup: 5/4 time signature
  - Expect: Pattern adapts gracefully (no out-of-range beat positions)

---

// Notes for implementer reviewers:
// AI: This analysis focuses on ambiguity, dependencies, edge cases, and test strategy. Pattern substitution is fundamentally different from other operator families: it replaces rather than adds. The "high memory penalty" warning and section-awareness requirements suggest these are bold, infrequent moves that define section identity rather than subtle variations.
