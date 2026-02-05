# Bar Context Consolidation Epic

**Purpose:** Eliminate structural redundancy between `Bar`, `BarContext`, and `AgentContext` by consolidating all bar-related context into a single authoritative source (`Bar`).

**Problem Statement:** 
The codebase has evolved to have three overlapping types for bar context:
1. **`Bar`** (`Music\Song\Bar\Bar.cs`) - Contains timing/tick information (StartTick, EndTick, time signature)
2. **`BarContext`** (`Music\Generator\Agents\Common\BarContext.cs`) - Contains section context (Section, BarWithinSection, BarsUntilSectionEnd)
3. **`AgentContext`** - Contains duplicate properties (BarNumber, BarsUntilSectionEnd, PhrasePosition, SectionType)

This creates:
- Data duplication across types
- Multiple builders populating overlapping data
- Confusion about which type is the source of truth
- Maintenance burden when context needs change

**Solution:** Consolidate `BarContext` properties into `Bar`, making `Bar` the single source of truth (SSOT) for all bar-related context. Remove redundant properties from `AgentContext` and eliminate `BarContext` entirely.

**UnitTests:** Do not write or update unit tests as part of this epic. A separate epic will address test updates after the structural changes are complete.

**Implementation Summary:** Write the implementation summary to folder C:\Users\sleva\source\repos\Music\Music\AI\Completed\

---

## Current State

### Bar (timing-focused)
```csharp
public class Bar
{
    public int BarNumber;
    public long StartTick { get; set; }
    public long EndTick { get; set; }
    public int Numerator;
    public int Denominator;
    // Computed: TicksPerMeasure, TicksPerBeat, BeatsPerBar
}
```

### BarContext (section-focused) 
```csharp
public sealed record BarContext(
    int BarNumber,           // DUPLICATE of Bar.BarNumber
    Section? Section,
    int BarWithinSection,
    int BarsUntilSectionEnd);
```

### AgentContext (contains redundant fields)
```csharp
public record AgentContext
{
    public required int BarNumber { get; init; }           // DUPLICATE
    public required int BarsUntilSectionEnd { get; init; } // DUPLICATE
    public required double PhrasePosition { get; init; }   // Computed from BarContext
    public required MusicConstants.eSectionType SectionType { get; init; } // From BarContext.Section
    // ... energy, tension, seed, etc. (agent-specific, keep these)
}
```

---

## Target State

### Bar (consolidated SSOT)
```csharp
public class Bar
{
    // Existing timing properties
    public int BarNumber;
    public long StartTick { get; set; }
    public long EndTick { get; set; }
    public int Numerator;
    public int Denominator;
    public int TicksPerMeasure { get; }
    public int TicksPerBeat { get; }
    public int BeatsPerBar { get; }
    
    // NEW: Section context (from BarContext)
    public Section? Section { get; set; }
    public int BarWithinSection { get; set; }
    public int BarsUntilSectionEnd { get; set; }
    
    // NEW: Computed phrase position
    public double PhrasePosition { get; }  // Computed from Section/BarWithinSection
}
```

### AgentContext (simplified, references Bar)
```csharp
public record AgentContext
{
    public required Bar Bar { get; init; }  // Reference to consolidated Bar
    public required decimal Beat { get; init; }  // Beat within bar (for event-level context)
    
    // Agent-specific properties (keep these)
    public required double EnergyLevel { get; init; }
    public required double TensionLevel { get; init; }
    public required double MotifPresenceScore { get; init; }
    public required int Seed { get; init; }
    public required string RngStreamKey { get; init; }
    public MotifPresenceMap? MotifPresenceMap { get; init; }
}
```

### BarContext - DELETED

---

## Impact Analysis

### Files with BarContext references (454 total occurrences):

| File | Count | Impact |
|------|-------|--------|
| DensityTargetComputationTests.cs | 55 | Test file - update after main changes |
| DrummerContextTests.cs | 49 | Test file - update after main changes |
| DrummerPolicyProviderTests.cs | 49 | Test file - update after main changes |
| SelectionUntilTargetTests.cs | 40 | Test file - update after main changes |
| DrummerAgentTests.cs | 29 | Test file - update after main changes |
| GrooveCandidateSourceTests.cs | 28 | Test file - update after main changes |
| DrummerCandidateSourceTests.cs | 26 | Test file - update after main changes |
| DrummerPolicyProvider.cs | 25 | Core file - Story 2 |
| DrummerContextBuilder.cs | 19 | Core file - Story 3 |
| GrooveOutputContractsTests.cs | 19 | Test file - many are commented out |
| PhraseHookWindowResolverTests.cs | 17 | Test file - update after main changes |
| GroovePolicyHookTests.cs | 16 | Test file - update after main changes |
| DrummerMotifIntegrationTests.cs | 15 | Test file - update after main changes |
| DrummerCandidateSource.cs | 15 | Core file - Story 2 |
| DrumPhraseGenerator.cs | 10 | Core file - Story 2 |
| DrummerDeterminismTests.cs | 10 | Test file - update after main changes |
| DrumBarContextBuilder.cs | 5 | DELETE or merge into BarTrack |
| DrumDensityCalculator.cs | 4 | Core file - Story 2 |
| IDrumPolicyProvider.cs | 4 | Interface - Story 2 |
| DrumSelectionEngine.cs | 4 | Core file - Story 2 |
| IDrumCandidateSource.cs | 3 | Interface - Story 2 |
| DrumTrackGenerator.cs | 2 | Core file - Story 2 |

### Key Builders to Modify:
1. **BarTrack.RebuildFromTimingTrack** - Must also populate section context
2. **DrumBarContextBuilder** - DELETE (merged into BarTrack)
3. **DrummerContextBuilder** - Simplify to use Bar directly

---

## Stories

### Story 1: Add Section Context Properties to Bar

**Size:** Small (30-45 min)

**Goal:** Add BarContext properties to the Bar class.

**Breaking Change:** NO - This is additive only.

**Files to Modify:**
- `Music\Song\Bar\Bar.cs`

**Implementation:**
1. Add `Section?` property
2. Add `BarWithinSection` property (int)
3. Add `BarsUntilSectionEnd` property (int)
4. Add computed `PhrasePosition` property
5. Update AI comments

**Acceptance Criteria:**
- [ ] Bar has all BarContext properties plus timing properties
- [ ] PhrasePosition is computed correctly (0.0 at start, 1.0 at end)
- [ ] Existing code continues to work (additive change)
- [ ] Build succeeds

---

### Story 2: Update BarTrack.RebuildFromTimingTrack to Populate Section Context

**Size:** Small (30-45 min)

**Goal:** BarTrack should populate both timing AND section context for each Bar.

**Breaking Change:** NO - This enhances existing method.

**Files to Modify:**
- `Music\Song\Bar\BarTrack.cs`

**Implementation:**
1. Add `SectionTrack` parameter to `RebuildFromTimingTrack`
2. During bar creation, look up Section for each bar
3. Calculate BarWithinSection and BarsUntilSectionEnd
4. Set these on the Bar object

**Acceptance Criteria:**
- [ ] RebuildFromTimingTrack populates section context on each Bar
- [ ] Existing callers updated to pass SectionTrack
- [ ] Build succeeds

---

### Story 3: Update AgentContext to Reference Bar

**Status:** Completed

**Size:** Medium (45-60 min)

**Goal:** Replace redundant properties in AgentContext with a Bar reference.

**Breaking Change:** YES - AgentContext signature changes. Fixed in Story 4.

**Files to Modify:**
- `Music\Generator\Agents\Common\AgentContext.cs`

**Implementation:**
1. Add `Bar` property to AgentContext
2. Keep BarNumber, SectionType, PhrasePosition, BarsUntilSectionEnd as computed properties that delegate to Bar
3. Update CreateMinimal() factory method
4. Remove `required` from properties that now delegate to Bar

**Computed Properties (backward compatible):**
```csharp
public int BarNumber => Bar.BarNumber;
public MusicConstants.eSectionType SectionType => Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
public double PhrasePosition => Bar.PhrasePosition;
public int BarsUntilSectionEnd => Bar.BarsUntilSectionEnd;
```

**Acceptance Criteria:**
- [ ] AgentContext has `Bar` property
- [ ] Backward-compatible computed properties work
- [ ] Build succeeds (may have warnings about obsolete construction)

---

### Story 4: Update DrummerContext and DrummerContextBuilder

**Status:** Completed

**Size:** Medium (45-60 min)

**Goal:** Update drummer-specific context to use consolidated Bar.

**Breaking Change:** NO - Internal refactor, DrummerContext inherits from updated AgentContext.

**Files to Modify:**
- `Music\Generator\Agents\Drums\Context\DrummerContext.cs`
- `Music\Generator\Agents\Drums\Context\DrummerContextBuilder.cs`
- `Music\Generator\Agents\Drums\Context\DrumBarContextBuilder.cs` (mark obsolete)

**Implementation:**
1. Update `DrummerContextBuildInput` to take `Bar` instead of `BarContext`
2. Simplify `DrummerContextBuilder.Build` to use Bar properties
3. Remove phrase position computation (now in Bar)
4. Mark DrumBarContextBuilder as obsolete

**Acceptance Criteria:**
- [ ] DrummerContextBuilder accepts Bar directly
- [ ] DrumBarContextBuilder marked [Obsolete]
- [ ] Build succeeds

---

### Story 5: Update Interface Signatures (IDrumCandidateSource, IDrumPolicyProvider)

**Status:** Completed

**Size:** Small (30-45 min)

**Goal:** Update interfaces to use Bar instead of BarContext.

**Breaking Change:** YES - Interface signatures change. Implementations updated in same story.

**Files to Modify:**
- `Music\Generator\Agents\Drums\Selection\Candidates\IDrumCandidateSource.cs`
- `Music\Generator\Agents\Drums\Policy\IDrumPolicyProvider.cs`
- `Music\Generator\Agents\Drums\Selection\Candidates\DrummerCandidateSource.cs`
- `Music\Generator\Agents\Drums\Policy\DrummerPolicyProvider.cs`
- `Music\Generator\Agents\Drums\Policy\DefaultDrumPolicyProvider.cs`

**Implementation:**
1. Change `IDrumCandidateSource.GetCandidateGroups(BarContext, string)` to `GetCandidateGroups(Bar, string)`
2. Change `IDrumPolicyProvider.GetPolicy(DrumBarContext, string)` to `GetPolicy(Bar, string)`
3. Update all implementations

**Acceptance Criteria:**
- [ ] Interfaces use Bar parameter
- [ ] All implementations updated
- [ ] Build succeeds

---

### Story 6: Update DrumDensityCalculator and DrumSelectionEngine

**Status:** Completed

**Size:** Small (30-45 min)

**Goal:** Update calculation utilities to use Bar.

**Breaking Change:** YES - Method signatures change.

**Files to Modify:**
- `Music\Generator\Agents\Drums\Selection\DrumDensityCalculator.cs`
- `Music\Generator\Agents\Drums\Selection\DrumSelectionEngine.cs`

**Implementation:**
1. Change `ComputeDensityTarget(BarContext, ...)` to `ComputeDensityTarget(Bar, ...)`
2. Change `SelectUntilTargetReached(BarContext, ...)` to `SelectUntilTargetReached(Bar, ...)`

**Acceptance Criteria:**
- [ ] Both utilities accept Bar instead of BarContext
- [ ] Build succeeds

---

### Story 7: Update DrumPhraseGenerator and DrumTrackGenerator

**Status:** Completed

**Size:** Medium (45-60 min)

**Goal:** Update generators to use consolidated Bar.

**Breaking Change:** NO - Internal implementation change.

**Files to Modify:**
- `Music\Generator\Agents\Drums\Generation\DrumPhraseGenerator.cs`
- `Music\Generator\Agents\Drums\Generation\DrumTrackGenerator.cs`

**Implementation:**
1. Replace DrumBarContextBuilder.Build() calls with direct Bar access from BarTrack
2. Update iteration to use barTrack.Bars directly
3. Remove intermediate BarContext list creation

**Acceptance Criteria:**
- [ ] Generators use Bar directly from BarTrack
- [ ] No BarContext creation in generators
- [ ] Build succeeds

---

### Story 8: Delete BarContext and DrumBarContextBuilder

**Status:** Completed

**Size:** Small (15-30 min)

**Goal:** Remove the now-unused BarContext type and DrumBarContextBuilder.

**Breaking Change:** NO - All references already updated.

**Files to Delete:**
- `Music\Generator\Agents\Common\BarContext.cs`
- `Music\Generator\Agents\Drums\Context\DrumBarContextBuilder.cs`

**Implementation:**
1. Delete both files
2. Remove any `using` statements referencing these
3. Update .csproj if necessary

**Acceptance Criteria:**
- [ ] Both files deleted
- [ ] Build succeeds
- [ ] All tests pass

---

### Story 9: Remove Backward-Compatibility Properties from AgentContext

**Size:** Small (30-45 min)

**Goal:** Clean up AgentContext by removing delegating properties added for backward compatibility.

**Breaking Change:** YES - Final cleanup, all callers should already use Bar properties.

**Files to Modify:**
- `Music\Generator\Agents\Common\AgentContext.cs`
- Any remaining callers using old property names

**Implementation:**
1. Remove `BarNumber` property (use `Bar.BarNumber`)
2. Remove `SectionType` property (use `Bar.Section?.SectionType`)
3. Remove `PhrasePosition` property (use `Bar.PhrasePosition`)
4. Remove `BarsUntilSectionEnd` property (use `Bar.BarsUntilSectionEnd`)
5. Update any remaining callers

**Acceptance Criteria:**
- [ ] AgentContext has no redundant properties
- [ ] Build succeeds
- [ ] All tests pass

---

## Execution Order

```
Story 1 (Add to Bar) ─────────────────────────────────────────────────────┐
                                                                          │
Story 2 (BarTrack populates) ────────────────────────────────────────────┤
                                                                          ▼
Story 3 (AgentContext refs Bar) ──┬── BREAKING ──► Story 4 (Drummer context)
                                  │
                                  └── BREAKING ──► Story 5 (Interfaces) ──► Story 6 (Utilities)
                                                                          │
Story 7 (Generators) ◄────────────────────────────────────────────────────┘
         │
         ▼
Story 8 ─────► Story 9 
```

**Breaking Changes Timeline:**
- Stories 1-2: Additive, no breaks
- Stories 3-6: Breaking changes, must be done together
- Stories 7-8: Fix all consumers
- Stories 9: Final cleanup

---

## Success Criteria

After implementing this epic:

1. **Single source of truth:** `Bar` is the SSOT for all bar-related context
2. **No redundancy:** No duplicate BarNumber/BarsUntilSectionEnd properties
3. **Simplified builders:** One place (BarTrack) populates all bar context
4. **Cleaner AgentContext:** References Bar instead of duplicating properties
5. **BarContext eliminated:** The type no longer exists
6. **All tests pass:** No regressions

---

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Large number of test file changes | Story 8 is sized for 2-3 hours; can be split if needed |
| Breaking changes mid-epic | Stories 3-6 must be done in one session |
| Missed references | Use `grep -r "BarContext"` before Story 9 to verify |
| Section lookup performance | BarTrack already iterates once; minimal overhead |

---

*Epic Created:* Based on user request to consolidate Bar/BarContext redundancy
*Estimated Total Time:* 6-8 hours
*Breaking Changes:* Stories 3-6, Story 10
