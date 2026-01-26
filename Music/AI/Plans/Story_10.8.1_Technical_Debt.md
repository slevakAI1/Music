# Story 10.8.1 Technical Debt

## Status: Implemented with Known Issues

Story 10.8.1 was implemented and meets all acceptance criteria functionally. However, PreAnalysis identified architectural issues that turned out to be more than stylistic concerns.

## Critical Issue: Bypasses Groove Selection System

### Problem

`DrummerAgent.Generate()` currently:
1. Gets candidate groups from `IGrooveCandidateSource` ✅
2. **Converts ALL candidates directly to onsets** ❌
3. Does NOT use `GrooveSelectionEngine.SelectUntilTargetReached()` ❌

See `DrummerAgent.cs` lines 279-315:
```csharp
private List<DrumOnset> GenerateOperatorOnsets(...)
{
    // ...
    var candidateGroups = GetCandidateGroups(grooveBarContext, role);
    
    // BUG: This converts ALL candidates, not selected ones
    foreach (var group in candidateGroups)
    {
        foreach (var candidate in group.Candidates)
        {
            onsets.Add(new DrumOnset(...)); // Adds EVERYTHING
        }
    }
}
```

### Impact

This bypasses:
- **Density target enforcement** - No limiting of onset count per bar
- **Weighted selection** - All candidates selected equally regardless of score/weight
- **Operator caps** - MaxEventsPerBar and per-group caps ignored
- **Policy-driven selection** - Density01Override from GetPolicy() never used

### Why It Seems To Work

- PhysicalityFilter reduces candidates before they reach Generate()
- Anchor onsets provide baseline structure
- Tests don't verify density targets or selection weights

### Architectural Root Cause

Per PreAnalysis clarifying question #4:

> **Why does AC #6 specify `Generate(SongContext) → PartTrack`?** This suggests a top-level entry point, but the dual interface (IGroovePolicyProvider + IGrooveCandidateSource) suggests the agent is a *component* within the existing groove system, not a replacement.

**Answer**: AC #6 was ambiguous. The two interfaces suggest DrummerAgent should be a **data source** for a pipeline, not the pipeline itself.

## Correct Architecture (Not Implemented)

### Option A: DrummerAgent as Component

```
┌─────────────┐
│Generator.cs │
└──────┬──────┘
       │ creates
       ↓
┌──────────────────┐        ┌─────────────────┐
│  DrummerAgent    │        │ GroovePipeline  │
│  (implements)    │───────→│  (orchestrator) │
│  • IPolicy       │  pass  │                 │
│  • ICandidateSource│       │  Uses:          │
└──────────────────┘        │  • GetPolicy()  │
                            │  • GetCandidates()│
                            │  • Selection    │
                            │  • Caps         │
                            │  • Rendering    │
                            └─────────────────┘
```

DrummerAgent would:
- Implement IGroovePolicyProvider ✅
- Implement IGrooveCandidateSource ✅
- **NOT** have Generate() method ❌

GroovePipeline would:
- Accept IGroovePolicyProvider + IGrooveCandidateSource
- Call GetPolicy() for density targets
- Call GetCandidateGroups() for candidates
- Use GrooveSelectionEngine for selection
- Apply caps enforcement
- Convert to PartTrack

### Option B: DrummerAgent Internal Pipeline (Current, Needs Fix)

Keep Generate() method but fix it to use GrooveSelectionEngine:

```csharp
private List<DrumOnset> GenerateOperatorOnsets(...)
{
    var onsets = new List<DrumOnset>();
    
    foreach (var barContext in barContexts)
    {
        var grooveBarContext = CreateGrooveBarContext(barContext);
        
        foreach (var role in drumRoles)
        {
            // 1. Get policy for density target
            var policy = GetPolicy(grooveBarContext, role);
            int targetCount = CalculateTargetCount(policy, grooveBarContext);
            
            // 2. Get candidate groups
            var candidateGroups = GetCandidateGroups(grooveBarContext, role);
            
            // 3. SELECT using GrooveSelectionEngine (MISSING!)
            var selectedCandidates = GrooveSelectionEngine.SelectUntilTargetReached(
                grooveBarContext,
                role,
                candidateGroups,
                targetCount,
                existingAnchors,
                diagnostics: null);
            
            // 4. Convert SELECTED candidates to onsets
            foreach (var candidate in selectedCandidates)
            {
                onsets.Add(new DrumOnset(...));
            }
        }
    }
    
    return onsets;
}
```

## Recommended Fix

**Option B** (fix current implementation) is simpler:

1. Add `GrooveSelectionEngine.SelectUntilTargetReached()` call
2. Calculate target count from policy density
3. Pass existing anchors to avoid conflicts
4. Convert only SELECTED candidates

This keeps the existing API but fixes the selection logic.

## Testing Gap

Current tests don't verify:
- Density targets are enforced
- Operator weights affect selection frequency
- Caps limit total events per bar
- Policy overrides affect selection

Story 10.8.2 should add these tests before fixing the implementation.

## Related Files

- `Music/Generator/Agents/Drums/DrummerAgent.cs` (lines 279-315)
- `Music/Generator/Groove/GrooveSelectionEngine.cs` (correct pattern)
- `Music/AI/Plans/PreAnalysis_10.8.1.md` (identified this issue)

## Follow-Up Story Needed

**Story 10.8.4** - Fix DrummerAgent Selection Logic
- Update GenerateOperatorOnsets() to use GrooveSelectionEngine
- Add tests for density enforcement
- Add tests for weighted selection
- Add tests for cap enforcement
- Verify determinism after fix

## Temporary Workaround

Current implementation is usable for:
- Basic drum generation
- Operator candidate generation
- Physicality filtering

But produces overly dense output because it doesn't respect:
- Density targets from policy
- Selection weights
- Per-bar caps

---

**Created**: Based on Story 10.8.1 implementation review
**Severity**: High - Core selection logic not implemented
**Priority**: Should fix before Story 10.8.3 (golden tests)
