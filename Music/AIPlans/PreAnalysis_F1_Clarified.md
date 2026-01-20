# Pre-Analysis: Story F1 — Implement Override Merge Policy Enforcement (CLARIFIED)

## 1) Story Intent Summary
- **What:** Ensure segment-level overrides follow a clear policy that controls whether list-style overrides replace or merge, whether protected onsets may be removed, whether caps can be relaxed, and whether feel/swing can be changed by segment overrides.
- **Why:** Prevent unexpected behavior when segments apply overrides; keep generation predictable and safe for users and downstream consumers.
- **Who:** Developers (implementers/maintainers), the groove generator at runtime, and end-users relying on stable, predictable segment overrides.

## 2) Acceptance Criteria Checklist
1. Implement `OverrideReplacesLists` for:
   - variation tags (EnabledVariationTags in SegmentGrooveProfile)
   - protection tags (EnabledProtectionTags in SegmentGrooveProfile)
   - density targets (DensityTargets list in SegmentGrooveProfile)
2. Implement `OverrideCanRemoveProtectedOnsets`:
   - If false, `IsProtected`, `IsMustHit`, and `IsNeverRemove` onsets never removed even under override
   - If true, allow removal logic for `IsProtected` only (never remove `IsMustHit` or `IsNeverRemove`)
3. Implement `OverrideCanRelaxConstraints`:
   - If true, segment can specify density targets that increase `MaxEventsPerBar` beyond base policy
   - If false, effective caps = min(segment override, base caps) for all cap types
4. Implement `OverrideCanChangeFeel`:
   - If false, ignore `OverrideFeel` and `OverrideSwingAmount01` in SegmentGrooveProfile
   - If true, allow FeelTimingEngine to use segment overrides
5. Unit tests cover all four booleans in both states with a small matrix (2^4 = 16 test combinations)

## 3) Dependencies & Integration Points
- Depends on other stories/features:
  - Story A3 (policy hook / `GroovePolicyDecision`)
  - Stories B/C (variation catalog, selection, caps)
  - Story G1/G2 (diagnostics & provenance) - diagnostics should record when overrides are applied/ignored
  - Stories E1/E2 (feel & role timing) - E1 feel timing must respect `OverrideCanChangeFeel`
- Existing code/types to interact with:
  - `SegmentGrooveProfile` (has EnabledVariationTags, EnabledProtectionTags, DensityTargets, OverrideFeel, OverrideSwingAmount01)
  - `GroovePolicyDecision` / `IGroovePolicyProvider` (separate layer; not affected by F1)
  - `GrooveProtectionPolicy` and its `MergePolicy: GrooveOverrideMergePolicy` (holds the 4 booleans)
  - `ProtectionPolicyMerger`, `ProtectionPerBarBuilder`, `ProtectionApplier`
  - `GrooveVariationLayerMerger` (may need update for `OverrideReplacesLists`)
  - `GrooveBarPlan`, `GrooveOnset`, `GrooveVariationCatalog`, `GrooveCandidateGroup`
  - `FeelTimingEngine` (`ResolveEffectiveFeel`, `ResolveEffectiveSwingAmount`)
  - `GrooveDensityCalculator`, `GrooveCapsEnforcer`
  - Test helpers: `GrooveTestSetup`, deterministic RNG via `Rng`
- What this story provides for future work:
  - Stable override semantics required by drummer policy, UI editors, and diagnostics

## 4) Inputs & Outputs
- Inputs consumed:
  - Segment override payloads (from `SegmentGrooveProfile`): EnabledVariationTags, EnabledProtectionTags, DensityTargets, OverrideFeel, OverrideSwingAmount01
  - Base preset/catalog values: variation/protection lists, cap values, feel & swing (from GrooveProtectionPolicy)
  - Override merge policy settings (4 booleans from `GrooveProtectionPolicy.MergePolicy`)
  - Existing per-bar protections/anchors (`GrooveBarPlan.BaseOnsets`)
  - Pipeline context (bar index, enabled tags, role)
- Outputs produced/affected:
  - Effective per-segment lists (merged or replaced) used downstream by selection/constraints
  - Potentially filtered/modified `GrooveBarPlan` (if removals allowed for IsProtected)
  - Diagnostics entries indicating which overrides were applied/ignored
  - Effective caps/limits computed for selection/pruning
  - Effective feel/swing used by FeelTimingEngine
- Configuration/settings read:
  - `OverrideReplacesLists`, `OverrideCanRemoveProtectedOnsets`, `OverrideCanRelaxConstraints`, `OverrideCanChangeFeel` (all from `GrooveProtectionPolicy.MergePolicy`)

## 5) Constraints & Invariants
- Rules that must always hold:
  - **Never prune `IsMustHit` or `IsNeverRemove` onsets under ANY circumstances** (even when `OverrideCanRemoveProtectedOnsets=true`)
  - Only `IsProtected` onsets may be removed when `OverrideCanRemoveProtectedOnsets=true`
  - Determinism: same inputs + overrides + seed => same outputs
  - If `OverrideCanChangeFeel` is false, feel and swing used by FeelTimingEngine must use base policy values (ignore segment overrides)
  - When `OverrideReplacesLists` is true for a list-kind, the segment override fully replaces the base list (no implicit union)
  - When `OverrideReplacesLists` is false, segment override is unioned/merged with base list
- Hard limits:
  - Base cap values (RoleRhythmVocabulary.MaxHitsPerBar/MaxHitsPerBeat, RoleMaxDensityPerBar) apply unless `OverrideCanRelaxConstraints` permits increases
  - When `OverrideCanRelaxConstraints=false`, effective caps = min(segment override, base caps)
  - When `OverrideCanRelaxConstraints=true`, segment can increase caps but should still respect reasonable upper bounds (e.g., 32 hits/bar max)
- Required operation order (high-level):
  1. Read merge policy booleans from `GrooveProtectionPolicy.MergePolicy`
  2. Resolve effective lists (merge or replace based on `OverrideReplacesLists`)
  3. Resolve effective caps (based on `OverrideCanRelaxConstraints`)
  4. Resolve effective feel/swing (based on `OverrideCanChangeFeel`)
  5. Apply protections (mark must-hit / never-add / never-remove)
  6. Compute density targets/caps (respecting cap relaxation policy)
  7. Run selection engine
  8. Prune if caps exceeded (respecting protected-onset removal rules)
  9. Apply E1 feel timing (using resolved effective feel/swing)
  10. Apply E2 role timing

## 6) Edge Cases to Test
- Empty segment override lists (null or empty list):
  - When `OverrideReplacesLists=true`: treat as clearing the list (no variations enabled)
  - When `OverrideReplacesLists=false`: treat as no override (use base list only)
- Segment override containing tags unknown to the catalog: should yield empty candidate groups after filtering
- Attempts to remove `IsMustHit` or `IsNeverRemove` onsets: should never succeed regardless of policy
- Attempts to remove `IsProtected` onsets when `OverrideCanRemoveProtectedOnsets=false`: should be prevented
- Segment overrides increasing caps to invalid values (negative, zero, or > 32): clamp to [0..32] regardless of policy
- Segment overrides attempting to decrease caps when `OverrideCanRelaxConstraints=false`: should be allowed (min wins)
- Partial list replace (segment list shorter than base): when replace=true, should result in shorter effective list
- Feel/swing overrides supplied when `OverrideCanChangeFeel=false`: should be ignored by FeelTimingEngine
- Interaction with phrase-hook protections: segment overrides should not affect phrase-hook augmented protections
- Combined effects: replace-lists + relax-constraints both true, replace-lists + cannot-remove-protected

## 7) Clarifying Questions — ANSWERED

### Q1: "Any per-segment list-based overrides": which list kinds beyond variation & protection should be included?
**ANSWER:** Three list-based overrides exist in `SegmentGrooveProfile`:
1. `EnabledVariationTags` (List<string>)
2. `EnabledProtectionTags` (List<string>)
3. `DensityTargets` (List<RoleDensityTarget>)

`OverrideReplacesLists` should apply to all three. If additional list properties are added to SegmentGrooveProfile in the future, they should follow the same policy.

### Q2: Is `OverrideReplacesLists` a single global flag or configurable per-list-kind? Preferred behavior?
**ANSWER:** Single global boolean flag in `GrooveOverrideMergePolicy`. All segment list overrides use the same replace vs merge semantics for consistency and simplicity. This avoids configuration explosion and keeps behavior predictable.

### Q3: If `OverrideReplacesLists=true` and the override list is empty, should that clear the base list or be treated as "no override provided"?
**ANSWER:** Empty/null lists are treated differently based on policy:
- When `OverrideReplacesLists=true`: Empty list **clears** the base list (explicit "disable all" intent)
- When `OverrideReplacesLists=false`: Empty/null list is treated as "no override" (use base list unchanged)

Deterministic rule:
```csharp
if (OverrideReplacesLists)
    effectiveList = segmentList ?? new List<string>(); // Empty means clear
else
    effectiveList = segmentList != null && segmentList.Count > 0 
        ? Union(baseList, segmentList) 
        : baseList; // Null/empty means use base
```

### Q4: When `OverrideCanRemoveProtectedOnsets=true`, which protection flags are allowed to be removed? Any exceptions?
**ANSWER:** Three-tier protection system with explicit removal rules:
- **`IsMustHit`**: NEVER removable (even when policy=true). These are structural anchors (downbeats, core pattern).
- **`IsNeverRemove`**: NEVER removable (even when policy=true). These are style-defining (e.g., backbeats in rock).
- **`IsProtected`**: Removable ONLY when `OverrideCanRemoveProtectedOnsets=true`. These are preferred but not required.

Pipeline removal logic (e.g., GrooveCapsEnforcer pruning):
```csharp
bool canRemove = !onset.IsMustHit && !onset.IsNeverRemove &&
    (mergePolicy.OverrideCanRemoveProtectedOnsets || !onset.IsProtected);
```

### Q5: If `OverrideCanRelaxConstraints=true` permits cap increases, are there upper bounds or validation rules to enforce?
**ANSWER:** Yes, enforce reasonable upper bounds even when relaxation is allowed:
- `MaxHitsPerBar`: clamp to [0..32] (32 hits/bar = 32nd notes, reasonable physical limit)
- `MaxHitsPerBeat`: clamp to [0..8] (8 hits/beat = 32nd note subdivisions)
- `Density01`: already clamped to [0.0..1.0] in GrooveDensityCalculator

Effective cap computation:
```csharp
if (mergePolicy.OverrideCanRelaxConstraints)
{
    // Segment can increase, but enforce absolute ceiling
    effectiveCap = Math.Min(segmentCap ?? baseCap, 32); // 32 = absolute ceiling
}
else
{
    // Segment can only tighten (decrease) caps
    effectiveCap = Math.Min(segmentCap ?? baseCap, baseCap);
}
```

### Q6: If `OverrideCanChangeFeel=false`, should swing be ignored independently of feel, or only when feel differs from base?
**ANSWER:** Both `OverrideFeel` and `OverrideSwingAmount01` are treated as a **single policy decision**. When `OverrideCanChangeFeel=false`:
- Ignore `SegmentGrooveProfile.OverrideFeel` → use `GrooveSubdivisionPolicy.Feel`
- Ignore `SegmentGrooveProfile.OverrideSwingAmount01` → use `GrooveSubdivisionPolicy.SwingAmount01`

Rationale: Feel and swing are coupled (swing only applies to Swing feel). Allowing swing override while blocking feel override would create confusing edge cases.

Implementation (update `FeelTimingEngine.ResolveEffectiveFeel/ResolveEffectiveSwingAmount`):
```csharp
public static GrooveFeel ResolveEffectiveFeel(
    GrooveSubdivisionPolicy subdivisionPolicy,
    SegmentGrooveProfile? segmentProfile,
    GrooveOverrideMergePolicy mergePolicy)
{
    return mergePolicy.OverrideCanChangeFeel
        ? segmentProfile?.OverrideFeel ?? subdivisionPolicy.Feel
        : subdivisionPolicy.Feel; // Ignore segment override
}
```

### Q7: Should ignored overrides always produce a diagnostic entry, or only when they would have changed behavior?
**ANSWER:** **Only when behavior would have changed** (optimization to avoid diagnostic spam).

Diagnostic emit rules:
- Emit when `OverrideCanChangeFeel=false` AND segment supplies non-null `OverrideFeel`/`OverrideSwingAmount01` that differs from base
- Emit when `OverrideReplacesLists=false` AND segment list would have cleared a non-empty base list
- Emit when `OverrideCanRelaxConstraints=false` AND segment would have increased a cap
- Emit when `OverrideCanRemoveProtectedOnsets=false` AND pruning would have removed a protected onset

Do NOT emit when segment override is null/empty (no user intent to override).

### Q8: Are overrides transient for generation only, or should they be persisted back into segment metadata when applied?
**ANSWER:** **Transient for generation only**. Segment overrides are input data; merge policy controls runtime behavior but never mutates source data.

- `SegmentGrooveProfile` is read-only during generation
- Effective resolved values (merged lists, computed caps, resolved feel) are ephemeral
- If user wants to "lock in" a computed override, they must explicitly save it via UI (future story)

This keeps generation pure/deterministic and avoids accidental config mutations.

## 8) Test Scenario Ideas

### Small Matrix Test (2^4 = 16 combinations)
Parameterized test covering all boolean states:
```
[DataRow(false, false, false, false)] // All disabled (safest/default)
[DataRow(true, false, false, false)]  // OverrideReplacesLists only
[DataRow(false, true, false, false)]  // OverrideCanRemoveProtectedOnsets only
[DataRow(false, false, true, false)]  // OverrideCanRelaxConstraints only
[DataRow(false, false, false, true)]  // OverrideCanChangeFeel only
... (11 more combinations)
```

For each combination:
- Setup: base preset with 2 variation tags, 1 protection tag, cap=8, feel=Straight
- Segment override: 1 new variation tag, 1 new protection tag, cap=12, feel=Swing
- Assert: effective lists, caps, feel match expected policy behavior
- Determinism check: run twice with same seed, assert identical outputs

### Individual Policy Tests
1. **OverrideReplacesLists**
   - `When_OverrideReplacesLists_True_EmptySegmentList_ClearsBase`
   - `When_OverrideReplacesLists_False_EmptySegmentList_KeepsBase`
   - `When_OverrideReplacesLists_True_NonEmptySegmentList_ReplacesBase`
   - `When_OverrideReplacesLists_False_NonEmptySegmentList_UnionsWithBase`

2. **OverrideCanRemoveProtectedOnsets**
   - `When_CanRemove_False_ProtectedNotPruned`
   - `When_CanRemove_True_ProtectedPruned_MustHitPreserved`
   - `When_CanRemove_Either_MustHitNeverPruned`
   - `When_CanRemove_Either_NeverRemoveNeverPruned`

3. **OverrideCanRelaxConstraints**
   - `When_CanRelax_False_SegmentCannotIncreaseCaps`
   - `When_CanRelax_False_SegmentCanDecreaseCaps`
   - `When_CanRelax_True_SegmentCanIncreaseCaps_CappedAt32`
   - `When_CanRelax_True_InvalidCap_ClampedToRange`

4. **OverrideCanChangeFeel**
   - `When_CanChange_False_FeelIgnored_UsesBase`
   - `When_CanChange_False_SwingIgnored_UsesBase`
   - `When_CanChange_True_FeelApplied`
   - `When_CanChange_True_SwingApplied`
   - `When_CanChange_False_DiagnosticsEmittedIfDifferent`

### Test Data Setups
- **Base preset:** PopRockBasic with:
  - Variation tags: ["Drive", "Pickup"]
  - Protection tags: ["CoreBackbeat"]
  - RoleMaxDensityPerBar["Snare"] = 8
  - Feel = Straight, SwingAmount01 = 0.0
  - MustHit: [1.0, 3.0], Protected: [2.0, 4.0], NeverRemove: [1.0]

- **Segment override:** Chorus segment with:
  - EnabledVariationTags: ["Fill"] or [] (empty test)
  - DensityTargets: Snare MaxEventsPerBar = 12
  - OverrideFeel = Swing, OverrideSwingAmount01 = 0.6

- Use deterministic RNG seed (42) and `GrooveTestSetup.BuildPopRockBasicGrooveForTestSong`

### Determinism Verification
- For each test, generate twice with same seed
- Assert `GrooveBarPlan.FinalOnsets` lists are identical (same count, same onsets in same order)
- Assert velocity/timing values are identical
- If diagnostics enabled, assert diagnostics entries are identical

// AI: product=Groove; story=F1; status=clarified; all 8 questions answered with explicit rules
