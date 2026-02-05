# Pre-Analysis: Story 6.2 — Implement Drummer Timing Nuance

**Story ID:** 6.2  
**Epic:** Human Drummer Agent (Stage 6 — Performance Rendering)  
**Status:** Pending (Story 6.1 completed)

---

## 1. Story Intent Summary

**What:**  
This story implements drummer-specific timing hints that allow drum candidates to express micro-timing nuances (playing slightly ahead, behind, or on-grid) using normalized intents that are genre-agnostic at the drummer layer, with style configuration mapping those intents to actual tick offsets.

**Why:**  
Human drummers don't play perfectly on-grid—they intentionally "push" or "lay back" relative to the beat to create pocket and feel. The snare typically lays slightly behind the beat (pocket feel), while the kick stays on-top (anchor). This micro-timing variation is what separates mechanical-sounding drums from human-like performance. Without this, even well-varied drum patterns sound robotic.

**Who Benefits:**
- **End-users**: Hear drums that sound more natural and groove-oriented
- **Developers**: Have a consistent pattern for normalized performance intent → style-mapped numeric values (same pattern established in Story 6.1 for velocity)
- **Generator**: Provides timing hints that the groove system's `RoleTimingEngine` can use as input for final timing calculations

---

## 2. Acceptance Criteria Checklist

### A. TimingIntent Enum (Normalized Intents)
1. ☐ Define `TimingIntent` enum with genre-agnostic values
2. ☐ `OnTop` = no offset (0 ticks)
3. ☐ `SlightlyAhead` = pushing feel (negative ticks)
4. ☐ `SlightlyBehind` = laid-back feel (positive ticks)
5. ☐ `Rushed` = aggressive push (more negative)
6. ☐ `LaidBack` = deep pocket (more positive)

### B. DrummerTimingHintSettings Record (Style-Specific Mapping)
7. ☐ Create `DrummerTimingHintSettings` record for per-style numeric mapping
8. ☐ Field: `SlightlyAheadTicks` (default: -5)
9. ☐ Field: `SlightlyBehindTicks` (default: +5)
10. ☐ Field: `RushedTicks` (default: -10)
11. ☐ Field: `LaidBackTicks` (default: +10)
12. ☐ Field: `MaxTimingJitter` (default: 3 ticks for humanization)
13. ☐ Field: `RoleTimingIntentDefaults` (Dictionary<role, TimingIntent>)
14. ☐ Default mapping: Snare → SlightlyBehind (universal pocket feel)
15. ☐ Default mapping: Kick → OnTop (universal anchor)
16. ☐ Default mapping: ClosedHat → OnTop (consistent timekeeping)
17. ☐ Default mapping: Fill candidates → context-dependent (rush toward climax)
18. ☐ Provide static presets: `ConservativeDefaults`
19. ☐ Integrate per-style presets into `StyleConfigurationLibrary`

### C. StyleConfiguration Extension
20. ☐ Extend `StyleConfiguration` with optional `DrummerTimingHints` field

### D. DrummerTimingShaper Implementation (Hint Provider)
21. ☐ Create `DrummerTimingShaper` class (hint-only, not final timing authority)
22. ☐ Input: `DrumCandidate` list
23. ☐ Input: `StyleConfiguration`
24. ☐ Input: context (fill position, energy)
25. ☐ Output: updated `DrumCandidate.TimingHint` values (still nullable)
26. ☐ **Constraint**: Must NOT write final timing; groove `RoleTimingEngine` remains final authority
27. ☐ Classify each candidate's timing intent from: role + FillRole + context
28. ☐ Map intent to tick offset via style settings
29. ☐ When `TimingHint` already set: adjust minimally toward intent target
30. ☐ When `TimingHint` is null: provide style-based hint
31. ☐ Fill timing behavior: FillStart/FillBody get slight rush (builds tension)
32. ☐ Fill timing behavior: FillEnd gets on-top timing (clean resolution)
33. ☐ **Determinism**: same inputs → same hints

### E. Unit Tests
34. ☐ Test: Timing hints respect style-provided targets
35. ☐ Test: Role-based intent classification correct (snare behind, kick on-top)
36. ☐ Test: Fill timing progression correct (rush→on-top)
37. ☐ Test: Determinism verified (same inputs → same hints)
38. ☐ Test: Clamping within reasonable bounds

---

## 3. Dependencies & Integration Points

### Depends On (Completed):
- **Story 6.1** (DrummerVelocityShaper): Establishes the pattern of normalized intents → style-mapped numeric values
- **Story 2.2** (DrumCandidate): Provides `DrumCandidate.TimingHint` field and `FillRole` enum
- **Story 1.4** (StyleConfiguration): Provides style configuration infrastructure
- **Story E2** (RoleTimingEngine from groove system): The final authority for timing offsets that this story provides hints to

### Interacts With:
- `DrumCandidate` type: Reads existing `TimingHint`, `Role`, `FillRole`, updates `TimingHint`
- `StyleConfiguration`: Reads optional `DrummerTimingHints` field
- `DrummerContext`: Context includes fill position, energy level
- `StyleConfigurationLibrary`: Where per-style timing hint presets are stored
- `RoleTimingEngine` (groove layer): Consumes timing hints produced by this story

### Provides For Future Stories:
- **Story 6.3** (Articulation Mapping): Completes the performance rendering layer
- **Story 7.1** (Diagnostics): Timing hints should be traceable in diagnostics
- **Stage 8** (Integration): Timing hints feed into final drum track generation

---

## 4. Inputs & Outputs

### Inputs:
- **`List<DrumCandidate>`**: List of drum candidates with optional existing `TimingHint` values
- **`StyleConfiguration`**: Contains optional `DrummerTimingHints` field with style-specific settings
- **Context fields** (from `DrummerContext` or similar):
  - `FillRole` (FillStart, FillBody, FillEnd, None)
  - Energy level (0.0-1.0)
  - Section type
  - Bar number / phrase position
- **Seed** (for deterministic jitter/variation)

### Outputs:
- **Updated `DrumCandidate.TimingHint` values**: Still nullable, but now populated with style-appropriate hints
- **NO FINAL TIMING WRITES**: This story only provides hints; `RoleTimingEngine` computes final offsets

### Configuration Read:
- `DrummerTimingHintSettings` from style configuration:
  - `SlightlyAheadTicks`, `SlightlyBehindTicks`, `RushedTicks`, `LaidBackTicks`
  - `MaxTimingJitter`
  - `RoleTimingIntentDefaults` (Dictionary<role, TimingIntent>)
- Style-specific presets from `StyleConfigurationLibrary` (PopRock, Jazz, Metal)

---

## 5. Constraints & Invariants

### MUST ALWAYS BE TRUE:
1. **Hint-only authority**: `DrummerTimingShaper` NEVER writes final timing; only provides `TimingHint`
2. **Groove system remains final authority**: `RoleTimingEngine` from Story E2 is the only component that writes actual timing offsets to `GrooveOnset`
3. **Normalized intents at drummer layer**: `TimingIntent` enum values are genre-agnostic (SlightlyBehind works for all genres)
4. **Style maps to numeric values**: All tick offsets come from `DrummerTimingHintSettings`, not hardcoded in shaper
5. **Determinism**: Same inputs → identical hints (no random behavior without seed)
6. **Minimal adjustment when hint exists**: When `TimingHint` is already set by an operator, adjust toward target, don't replace entirely

### Hard Limits:
- **`MaxTimingJitter`**: Default 3 ticks, configurable per style
- **Tick offsets bounded**: Must stay within reasonable range (e.g., ±20 ticks typical max)
- **Clamping required**: Final hints must be clamped to avoid extreme offsets

### Operation Order:
1. Classify candidate's timing intent (from role + FillRole + context)
2. Map intent to tick offset via style settings
3. If `TimingHint` already exists: adjust minimally toward target
4. If `TimingHint` is null: provide style-based hint
5. Apply jitter (deterministic, seeded)
6. Clamp final hint to reasonable bounds
7. Return updated candidates (with `TimingHint` populated)

### Universal Mappings (Genre-Agnostic):
- **Snare**: `SlightlyBehind` (pocket feel, universal across all genres)
- **Kick**: `OnTop` (anchor, universal across all genres)
- **ClosedHat**: `OnTop` (consistent timekeeping)
- **Fill progression**: rush → on-top (tension → resolution)

---

## 6. Edge Cases to Test

### Boundary Conditions:
1. **Empty candidate list**: What happens when `List<DrumCandidate>` is empty?
2. **All hints already set**: When every candidate has existing `TimingHint`, minimal adjustment logic must work
3. **No hints set**: When all `TimingHint` values are null, must provide sensible defaults
4. **Unknown roles**: What happens when a role is not in `RoleTimingIntentDefaults`? (e.g., custom drum kit pieces)
5. **Zero energy**: Does energy level = 0.0 affect timing decisions?
6. **Extreme energy**: Does energy = 1.0 push timing more aggressively?

### Null/Missing Configuration:
7. **Missing `DrummerTimingHints` in style**: Must use conservative defaults
8. **Null `RoleTimingIntentDefaults`**: Must have fallback mapping
9. **Missing style preset**: What if `StyleConfigurationLibrary.GetStyle()` returns null?

### Fill Timing Edge Cases:
10. **Fill at bar 1**: FillStart timing when at beginning of song
11. **Fill at section boundary**: Fill timing when crossing sections
12. **Non-fill candidates mixed with fill candidates**: Verify fill timing only affects fill candidates
13. **FillRole = None**: Should use role-based defaults, not fill timing rules

### Combination Scenarios:
14. **Snare in fill**: Does snare in FillBody get rush timing or SlightlyBehind pocket timing? (Fill timing should win)
15. **Kick in fill**: Does kick in FillEnd stay OnTop or follow fill timing rules?
16. **Multiple fill roles in same bar**: FillStart + FillBody + FillEnd timing progression
17. **Operator-provided hint + style hint conflict**: When operator already set `TimingHint`, how much adjustment is "minimal"?

### Determinism Edge Cases:
18. **Same inputs, different runs**: Verify identical hints across multiple runs
19. **Seed variation**: Different seeds should produce different jitter (within bounds)
20. **Context variation**: Same candidates, different context → different timing classification

### Clamping Edge Cases:
21. **Extreme style settings**: What if `RushedTicks = -50`? Must clamp to reasonable bounds
22. **Jitter overflow**: `TimingHint + MaxTimingJitter` exceeds reasonable bounds
23. **Negative + negative**: SlightlyAhead (-5) + jitter (-3) = -8 ticks, still reasonable?

### Performance/Scale Edge Cases:
24. **Large candidate lists**: 100+ candidates per bar (stress test)
25. **All candidates are fills**: Entire bar is FillBody candidates

---

## 7. Clarifying Questions

### A. Minimal Adjustment Logic (AC #29):
1. **Q**: When `TimingHint` already exists, what defines "adjust minimally toward intent target"?
   - Is there a maximum adjustment delta (e.g., ±5 ticks per call)?
   - Should adjustment be proportional to distance from target?
   - Example: If hint is -2 and target is +5, do we move to +3 (+5 delta) or +1 (+3 delta)?

   **A**: Following the pattern from Story 6.1 (`DrummerVelocityShaper`): Use a configurable `MaxAdjustmentDelta` (default: 5 ticks). If the distance to target is within `MaxAdjustmentDelta`, move directly to target. Otherwise, move by `MaxAdjustmentDelta` toward target. Example: hint=-2, target=+5, delta=5 → move to +3 (moving +5 toward target).

2. **Q**: Should minimal adjustment respect existing operator intent?
   - If an operator deliberately set `TimingHint = -10` (rushed), should we preserve that aggressiveness?
   - Or do we always pull toward style-default targets?

   **A**: We pull toward style-default targets but respect operator intent via `MaxAdjustmentDelta`. An operator-set hint of -10 with target +5 and delta=5 would become -5 (not +5). This preserves aggressive intent while nudging toward style. This mirrors Story 6.1's velocity behavior.

### B. Fill Timing Behavior (AC #31-32):
3. **Q**: How is "slight rush" quantified for FillStart/FillBody?
   - Is rush timing a fixed offset (e.g., -5 ticks)?
   - Or is it proportional to `RushedTicks` setting?
   - Does rush intensity increase from FillStart → FillBody?

   **A**: Use `SlightlyAheadTicks` for FillStart and `RushedTicks` for FillBody. This creates a natural progression: FillStart = slight push, FillBody = more aggressive push. The specific values come from `DrummerTimingHintSettings`, not hardcoded.

4. **Q**: For FillEnd "on-top for clean resolution", does this mean:
   - Exactly 0 ticks offset? Or
   - Use the role's default timing intent (e.g., snare's SlightlyBehind)?

   **A**: Exactly 0 ticks offset (`OnTop`). FillEnd represents the resolution moment and should land precisely on-grid regardless of role. This provides a clean cadential feel that contrasts with the rushed fill body.

5. **Q**: What about fills that don't follow FillStart→Body→End pattern?
   - Short fills (only FillStart + FillEnd)?
   - Setup hits (isolated FillRole without full pattern)?

   **A**: Each FillRole is handled independently based on its enum value. Short fills work correctly: FillStart gets slight rush, FillEnd gets on-top. Setup hits (`FillRole.Setup`) get `OnTop` timing as they are accent markers, not part of a rushing fill.

### C. Role vs Fill Timing Priority (Combination):
6. **Q**: When a candidate has both a role-based intent and fill-based intent, which wins?
   - Example: Snare (SlightlyBehind) with FillBody (rushed) — final intent?
   - Is there a priority order: FillRole > Role > Context?

   **A**: Priority order: **FillRole > Role**. If `FillRole != None`, use fill-based timing. Otherwise, use role-based timing. This ensures fill momentum is preserved regardless of which drum is playing the fill. Snare in FillBody → Rushed (not SlightlyBehind).

7. **Q**: For non-fill candidates in a fill bar, do they get affected by fill context?
   - Example: Kick anchor on beat 1, but bar has FillBody on beats 3-4

   **A**: No. Candidates with `FillRole.None` use role-based defaults only. Kick anchor on beat 1 with `FillRole.None` gets `OnTop` from its role default, regardless of other candidates in the same bar having fill roles.

### D. Context-Dependent Fill Timing (AC #17):
8. **Q**: "Fill candidates → context-dependent (rush toward climax)" — what context determines "rush toward climax"?
   - Is this based on phrase position (approaching cadence)?
   - Energy level (high energy = more rush)?
   - Section type (chorus fills rush more than verse)?

   **A**: Simplify for this story: Use `FillRole` enum as the context. `FillStart`/`FillBody` inherently represent "building toward climax" while `FillEnd` represents "resolution". Energy level can provide minor scaling (±2 ticks) but the primary driver is FillRole. Section-specific fill timing is out of scope for this story.

9. **Q**: How does "rush toward climax" translate to tick offsets?
   - Is it a scaling factor applied to `RushedTicks`?
   - Or a separate configuration field?

   **A**: Direct mapping: `FillStart` → `SlightlyAheadTicks`, `FillBody` → `RushedTicks`, `FillEnd` → 0, `Setup` → 0. No additional scaling factor needed. Energy-based adjustment (±2 ticks toward rush at high energy) is optional enhancement.

### E. Jitter Determinism (AC #12, #33):
10. **Q**: How is `MaxTimingJitter` applied deterministically?
    - Is jitter per-candidate (seeded by candidate ID)?
    - Or per-bar (same jitter for all candidates in a bar)?
    - What seed/RNG stream is used?

    **A**: Per-candidate, seeded by `HashCode.Combine(barNumber, beat, role, candidateId.GetHashCode())`. This provides humanization while ensuring determinism. No RNG stream needed—use deterministic hash-based jitter calculation: `jitter = (hash % (2 * MaxTimingJitter + 1)) - MaxTimingJitter`.

11. **Q**: Is jitter symmetric (±3 ticks) or biased by intent?
    - Example: SlightlyAhead candidates jitter more negative?

    **A**: Symmetric jitter around the target. The intent already determines the center point; jitter adds humanization equally in both directions. This simplifies implementation and testing while still providing natural feel.

### F. Unknown Role Handling:
12. **Q**: What happens when a role is not in `RoleTimingIntentDefaults`?
    - Use `OnTop` as fallback?
    - Skip timing hint entirely (leave `TimingHint = null`)?
    - Log a warning?

    **A**: Use `OnTop` as fallback. Unknown roles (custom kit pieces, future extensions) should have neutral timing. Provide a hint (don't skip), using the safest default. No warning needed—this is expected behavior for extensibility.

13. **Q**: Should there be a "default default" intent for custom/unknown roles?

    **A**: Yes, `TimingIntent.OnTop` is the universal fallback. This keeps custom instruments on-grid and musically safe.

### G. Conservative Defaults (AC #18):
14. **Q**: What makes a preset "conservative"?
    - Smaller tick offsets (e.g., ±3 instead of ±10)?
    - Less jitter?
    - OnTop for all roles?

    **A**: Conservative defaults = smaller offsets AND less jitter:
    - `SlightlyAheadTicks` = -3 (vs -5 for PopRock)
    - `SlightlyBehindTicks` = +3 (vs +5 for PopRock)  
    - `RushedTicks` = -6 (vs -10)
    - `LaidBackTicks` = +6 (vs +10)
    - `MaxTimingJitter` = 2 (vs 3)
    - Role defaults still apply (snare behind, kick on-top)

15. **Q**: When are conservative defaults used vs style-specific presets?
    - Only when style configuration is missing?
    - Or as a separate selectable preset?

    **A**: Used as fallback when `StyleConfiguration.DrummerTimingHints` is null. Also available as a static preset (`DrummerTimingHintSettings.ConservativeDefaults`) for explicit use. Matches Story 6.1 pattern.

### H. Integration with RoleTimingEngine (AC #26):
16. **Q**: How does this story's `TimingHint` interact with `RoleTimingEngine` from Story E2?
    - Does `RoleTimingEngine` use `TimingHint` as input?
    - Or does it compute timing independently and `TimingHint` is just a suggestion?
    - Is there a conflict resolution mechanism?

    **A**: `DrummerTimingShaper` provides `TimingHint` on `DrumCandidate`. When mapped to `GrooveOnsetCandidate` via `DrumCandidateMapper`, the hint can be preserved as a tag or used to influence the onset. `RoleTimingEngine` remains the final authority for `GrooveOnset.TimingOffsetTicks`. The hint is a suggestion that the groove layer may use as input. No direct conflict—different layers of abstraction.

17. **Q**: Can `RoleTimingEngine` override timing hints?
    - Example: Groove policy says "no timing variance" — does that null out hints?

    **A**: Yes, `RoleTimingEngine` is the final authority. If groove policy restricts timing variance, the engine can ignore hints. The drummer layer provides musical intent; the groove layer enforces constraints. This maintains clear separation of concerns.

### I. Per-Style Presets (AC #19):
18. **Q**: Which styles get timing hint presets initially?
    - PopRock, Jazz, Metal (matching velocity presets from Story 6.1)?
    - Are there genre-specific differences (e.g., Jazz laid-back, PopRock on-top)?

    **A**: Same three styles as Story 6.1: PopRock, Jazz, Metal.
    - **PopRock**: Standard pocket feel (snare +5, kick 0, moderate rush on fills)
    - **Jazz**: More laid-back overall (snare +8, kick +3, gentler fill rush)
    - **Metal**: Tighter/on-top feel (snare +2, kick 0, aggressive fill rush)

19. **Q**: Should different sub-styles have different timing feel?
    - Example: "Rock" vs "PopRock" vs "HardRock" — same or different timing?

    **A**: Out of scope for this story. The three preset styles (PopRock, Jazz, Metal) demonstrate the pattern. Sub-styles can be added later by creating additional `StyleConfiguration` entries in `StyleConfigurationLibrary`.

### J. Energy Influence (Context):
20. **Q**: Does energy level affect timing hints?
    - High energy = more rushed timing?
    - Low energy = more laid-back?
    - Or is energy independent of timing feel (pocket is pocket regardless)?

    **A**: Minor influence only: ±2 ticks based on energy level (0.0-1.0). High energy (>0.7) nudges hints 1-2 ticks earlier (toward rush). Low energy (<0.3) nudges 1-2 ticks later (toward laid-back). This is subtle and configurable. The primary timing character comes from role/fill classification, not energy.

---

## 8. Test Scenario Ideas

### Unit Tests (Role-Based Intent):
1. **`TimingIntent_Snare_DefaultsToSlightlyBehind`**: Verify snare gets SlightlyBehind intent
2. **`TimingIntent_Kick_DefaultsToOnTop`**: Verify kick gets OnTop intent
3. **`TimingIntent_ClosedHat_DefaultsToOnTop`**: Verify hat gets OnTop intent
4. **`TimingIntent_UnknownRole_FallbackBehavior`**: Test custom role not in defaults

### Unit Tests (Fill Timing):
5. **`FillTiming_FillStart_AppliesRush`**: FillStart candidates get rushed timing
6. **`FillTiming_FillBody_AppliesRush`**: FillBody candidates get rushed timing
7. **`FillTiming_FillEnd_AppliesOnTop`**: FillEnd candidates get on-top timing
8. **`FillTiming_NonFillCandidates_IgnoreFillRules`**: Candidates with FillRole.None use role defaults

### Unit Tests (Minimal Adjustment):
9. **`MinimalAdjustment_ExistingHint_AdjustsTowardTarget`**: When hint exists, adjust toward style target
10. **`MinimalAdjustment_ExistingHint_DoesNotReplace`**: Verify adjustment is incremental, not replacement
11. **`MinimalAdjustment_AlreadyAtTarget_NoChange`**: When hint already matches target, no adjustment

### Unit Tests (Style Mapping):
12. **`StyleMapping_PopRock_AppliesPopRockOffsets`**: PopRock style uses PopRock timing settings
13. **`StyleMapping_Jazz_AppliesJazzOffsets`**: Jazz style uses Jazz timing settings
14. **`StyleMapping_MissingStyle_UsesConservativeDefaults`**: Fallback when style not configured

### Unit Tests (Determinism):
15. **`Determinism_SameInputs_IdenticalHints`**: Run shaper twice with same inputs, verify identical output
16. **`Determinism_DifferentSeed_DifferentJitter`**: Different seeds produce different jitter
17. **`Determinism_SameContext_ConsistentClassification`**: Same candidate properties → same intent classification

### Unit Tests (Clamping):
18. **`Clamping_ExtremeStyleSettings_ClampsToReasonableBounds`**: Verify extreme config values clamped
19. **`Clamping_JitterOverflow_StaysWithinBounds`**: Hint + jitter doesn't exceed reasonable limits
20. **`Clamping_NegativeOverflow_ClampsToMinimum`**: Very negative offsets don't break timing

### Unit Tests (Null/Empty Handling):
21. **`EmptyList_ReturnsEmptyList`**: Empty candidate list returns empty list
22. **`NullTimingHints_ProvidesSensibleDefaults`**: All null hints get style-based defaults
23. **`MissingConfiguration_UsesBuiltInDefaults`**: Missing `DrummerTimingHints` in style config uses fallbacks

### Integration Tests (Combination Scenarios):
24. **`Integration_MixedFillAndNonFill_TimingProgression`**: Bar with anchors + fill, verify timing differences
25. **`Integration_SnareInFill_FillTimingWins`**: Snare in FillBody gets rush, not SlightlyBehind
26. **`Integration_KickInFillEnd_StaysOnTop`**: Kick in FillEnd stays anchored
27. **`Integration_FullFillProgression_RushToOnTop`**: Verify FillStart→Body→End timing progression

### Integration Tests (Context Influence):
28. **`Context_HighEnergy_AffectsTiming`**: Verify energy level influences timing decisions (if applicable)
29. **`Context_SectionBoundary_AffectsFillTiming`**: Fill at section end rushes more
30. **`Context_PhrasePosition_AffectsTiming`**: Phrase position influences timing hints

### Edge Case Tests:
31. **`EdgeCase_AllCandidatesAreFills`**: Entire bar is fill candidates
32. **`EdgeCase_NoFillsInBar_AllRoleBased`**: No fill candidates, all role-based timing
33. **`EdgeCase_SingleCandidate_GetsAppropriateHint`**: One candidate in bar
34. **`EdgeCase_HundredCandidates_PerformanceTest`**: Stress test with large candidate list

### Snapshot/Regression Tests:
35. **`Snapshot_PopRockStandardPattern_TimingHints`**: Verify standard pattern gets expected hints
36. **`Snapshot_FillBar_TimingProgression`**: Capture fill timing progression as golden test
37. **`Snapshot_CrossStyleComparison_PopRockVsJazz`**: Compare timing hints across styles

---

## Summary

**Story 6.2** adds drummer-specific micro-timing hints using normalized intents (OnTop, SlightlyAhead, SlightlyBehind, Rushed, LaidBack) that are genre-agnostic at the drummer layer. Style configuration maps these intents to actual tick offsets, following the same pattern as Story 6.1 (velocity). The shaper provides hints only—the groove system's `RoleTimingEngine` remains the final timing authority.

**Key challenges:**
- Defining "minimal adjustment" behavior when hints already exist
- Clarifying fill timing progression rules (rush→on-top)
- Resolving priority when role-based and fill-based timing conflict
- Ensuring deterministic jitter while maintaining humanization
- Integrating with existing `RoleTimingEngine` without conflicts

**Next steps:**
- Get clarifications on the questions above (especially minimal adjustment logic and fill timing rules)
- Review Story 6.1 implementation for pattern consistency
- Review Story E2 (`RoleTimingEngine`) to understand integration points
- Confirm style presets needed (PopRock, Jazz, Metal minimum?)
