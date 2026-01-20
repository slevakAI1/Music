# Pre-Analysis: Story F1 — Implement Override Merge Policy Enforcement

## 1) Story Intent Summary
- **What:** Ensure segment-level overrides follow a clear policy that controls whether list-style overrides replace or merge, whether protected onsets may be removed, whether caps can be relaxed, and whether feel/swing can be changed by segment overrides.
- **Why:** Prevent unexpected behavior when segments apply overrides; keep generation predictable and safe for users and downstream consumers.
- **Who:** Developers (implementers/maintainers), the groove generator at runtime, and end-users relying on stable, predictable segment overrides.

## 2) Acceptance Criteria Checklist
1. Implement `OverrideReplacesLists` for:
   - variation tags
   - protection tags
   - any per-segment list-based overrides used by groove
2. Implement `OverrideCanRemoveProtectedOnsets`:
   - If false, protected onsets never removed even under override
   - If true, allow removal logic (where pipeline supports removal)
3. Implement `OverrideCanRelaxConstraints`:
   - If true, segment caps can increase `MaxHits`/`MaxEvents` for the segment
   - If false, base caps apply and overrides cannot increase them
4. Implement `OverrideCanChangeFeel`:
   - If false, ignore segment feel/swing overrides
   - If true, allow feel/swing override to affect timing
5. Unit tests cover all four booleans in both states with a small matrix

> Note: "any per-segment list-based overrides" and "small matrix" are slightly ambiguous - see clarifying questions.

## 3) Dependencies & Integration Points
- Depends on other stories/features:
  - Story A3 (policy hook / `GroovePolicyDecision`)
  - Stories B/C (variation catalog, selection, caps)
  - Story G1/G2 (diagnostics & provenance)
  - Stories E1/E2 (feel & role timing)
- Existing code/types to interact with:
  - `SegmentGrooveProfile`, `GroovePolicyDecision`, `IGroovePolicyProvider`
  - `GrooveProtectionPolicy`, `ProtectionPolicyMerger`, `ProtectionPerBarBuilder`, `ProtectionApplier`
  - `GrooveBarPlan`, `GrooveOnset`, `GrooveVariationCatalog`, `GrooveCandidateGroup`
  - `FeelTimingEngine`, `RoleTimingEngine`
  - Test helpers: `GrooveTestSetup`, deterministic RNG via `Rng`
- What this story provides for future work:
  - Stable override semantics required by drummer policy, UI editors, and diagnostics

## 4) Inputs & Outputs
- Inputs consumed:
  - Segment override payloads (from `SegmentGrooveProfile`)
  - Base preset/catalog lists (variation, protection), cap values, feel & swing
  - Existing per-bar protections/anchors (`GrooveBarPlan.BaseOnsets`)
  - Pipeline context (bar index, enabled tags, role)
- Outputs produced/affected:
  - Effective per-segment lists (merged or replaced) used downstream
  - Potential modifications to `GrooveBarPlan` (if removals allowed)
  - Diagnostics entries indicating applied vs ignored overrides
  - Effective caps/limits computed for selection/pruning
- Config/settings read:
  - `OverrideReplacesLists`, `OverrideCanRemoveProtectedOnsets`, `OverrideCanRelaxConstraints`, `OverrideCanChangeFeel`

## 5) Constraints & Invariants
- Rules that must always hold:
  - Never prune `IsMustHit` or `IsNeverRemove` onsets when `OverrideCanRemoveProtectedOnsets` is false
  - Determinism: same inputs + overrides + seed => same outputs
  - If `OverrideCanChangeFeel` is false, feel and swing used by timing engines must remain base values
  - When `OverrideReplacesLists` is true for a list-kind, the override fully replaces the base list (no implicit union)
- Hard limits:
  - Base cap values (Role/Group/Candidate `MaxHitsPerBar`, `MaxHitsPerBeat`) apply unless `OverrideCanRelaxConstraints` permits increases
  - Any cap increases via override should respect global absolute constraints or validation rules if present
- Required operation order (high-level):
  1. Resolve effective lists (merge or replace)
  2. Apply protections (mark must-hit / never-add / never-remove)
  3. Compute caps/target counts (respecting relaxation)
  4. Run selection engine
  5. Prune if caps exceeded (respecting protected-onset removal rules)

## 6) Edge Cases to Test
- Empty or null override lists (treat as no override vs explicit clear depending on `OverrideReplacesLists` semantics)
- Override containing tags unknown to the catalog (should yield empty candidate groups or be ignored)
- Attempts to remove protected onsets when `OverrideCanRemoveProtectedOnsets=false`
- Overrides increasing caps to invalid values (negative, zero, or extremely large)
- Partial list replace (override shorter) vs full replace semantics
- Feel/swing overrides supplied when `OverrideCanChangeFeel=false` (no effect expected)
- Interaction with phrase-hook protections (anchors near phrase boundaries)
- Combined booleans effects, e.g., replace-lists + relax-constraints together

## 7) Clarifying Questions
1. "Any per-segment list-based overrides": which list kinds beyond variation & protection should be included (e.g., enabled roles, density targets, operator allowlists)?
2. Is `OverrideReplacesLists` a single global flag or configurable per-list-kind? Preferred behavior?
3. If `OverrideReplacesLists=true` and the override list is empty, should that clear the base list or be treated as "no override provided"?
4. When `OverrideCanRemoveProtectedOnsets=true`, which protection flags are allowed to be removed (`IsProtected`, `IsMustHit`, `IsNeverRemove`)? Any exceptions?
5. If `OverrideCanRelaxConstraints=true` permits cap increases, are there upper bounds or validation rules to enforce?
6. If `OverrideCanChangeFeel=false`, should swing be ignored independently of feel, or only when feel differs from base?
7. Should ignored overrides always produce a diagnostic entry, or only when they would have changed behavior?
8. Are overrides transient for generation only, or should they be persisted back into segment metadata when applied?

## 8) Test Scenario Ideas
- Unit test name examples:
  - `When_OverrideReplacesLists_VariationTags_True_ReplacesBaseList`
  - `When_OverrideReplacesLists_VariationTags_False_MergesWithBaseList`
  - `When_OverrideCanRemoveProtectedOnsets_False_ProtectedNotRemoved`
  - `When_OverrideCanRemoveProtectedOnsets_True_ProtectedRemovedIfAllowed`
  - `When_OverrideCanRelaxConstraints_False_CapsRemainBase`
  - `When_OverrideCanRelaxConstraints_True_CapsIncreased`
  - `When_OverrideCanChangeFeel_False_FeelIgnored`
  - `OverrideMatrix_AllBooleans_Combinations_ProducesExpectedBehavior` (parameterized)
- Test setups:
  - Base preset with deterministic anchors (must-hit and removable), base caps, and a small variation catalog
  - Segment override payloads exercising: replace vs merge lists, empty list, cap increase, feel change
  - Use deterministic RNG seeds and `GrooveTestSetup` fixture
- Determinism verification points:
  - Run generation twice with same seed and assert identical `GrooveBarPlan.FinalOnsets`
  - Verify diagnostics record whether override was applied or ignored

// AI: product=Groove; story=F1; focus=clarify override semantics and test matrix; no implementation advice
