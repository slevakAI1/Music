# Velocity Architecture Research: Groove System vs Drummer Agent

**Date:** January 2025  
**Status:** Analysis Complete  
**Recommendation:** Partial refactoring warranted

---

## Executive Summary

**User's concern is architecturally valid but requires nuanced handling.**

The user correctly identifies that velocity is NOT a rhythmic element and questioning its presence in the "Groove" system is reasonable. However, complete removal would break non-drum roles until their agents are implemented (Stages 11-14).

**Recommendation:** Keep minimal velocity fallback in Groove, remove sophisticated drum-specific velocity configuration (`GrooveAccentPolicy` drum entries), and document the transitional architecture.

---

## Analysis

### What "Groove" Should Mean

The term "groove" in music production refers to:
1. **Temporal placement** — when notes occur (onset grid)
2. **Timing feel** — swing, shuffle, push/pull micro-timing
3. **Rhythmic density** — how many events per beat/bar
4. **Rhythmic patterns** — backbeat emphasis, syncopation

**Velocity (dynamics) is NOT groove.** Dynamics are about *how loud*, not *when*. The user's intuition is correct.

### Current Architecture: Two Velocity Systems

The codebase has **two parallel velocity systems**:

#### 1. Groove `VelocityShaper` (Generic Fallback)

**Location:** `Generator/Groove/VelocityShaper.cs`

```csharp
// Preserve existing velocity if already set
if (onset.Velocity.HasValue)
{
    result.Add(onset);
    continue;
}
// Compute velocity using role + strength lookup
int velocity = ComputeVelocity(onset.Role, strength, accentPolicy, policyDecision);
```

**Key behavior:**
- Only fills in velocity when NOT already set
- Uses `GrooveAccentPolicy` with role+strength → velocity mapping
- Supports policy overrides (multiplier, additive bias)

**Configured via:** `GrooveProtectionPolicy.AccentPolicy` with per-role dictionaries

#### 2. Drummer `DrummerVelocityShaper` (Agent-Specific)

**Location:** `Generator/Agents/Drums/Performance/DrummerVelocityShaper.cs`

```csharp
// Classify the candidate's dynamic intent
var intent = ClassifyDynamicIntent(candidate);
// Get target velocity for this intent from STYLE CONFIGURATION
int targetVelocity = settings.GetTargetVelocity(intent);
```

**Key behavior:**
- Sets `VelocityHint` on `DrumCandidate` (not final velocity)
- Uses normalized `DynamicIntent` enum (genre-agnostic)
- Style configuration maps intent → numeric velocity
- Runs BEFORE Groove VelocityShaper

**Configured via:** `DrummerVelocityHintSettings` in `StyleConfiguration`

### The VelocityHint → Velocity Flow Problem

Currently, there's a **gap** in how `VelocityHint` becomes `Velocity`:

1. `DrummerVelocityShaper.ApplyHints()` sets `DrumCandidate.VelocityHint`
2. `DrumCandidateMapper.Map()` puts VelocityHint in tags: `VelocityHint:95`
3. **This tag is NOT consumed** by Groove `VelocityShaper`!
4. Groove `VelocityShaper` uses `GrooveAccentPolicy` lookup instead

**This means the DrummerVelocityShaper's work is partially ignored.** The sophisticated intent-based velocity hinting only affects final output if the tag is extracted and applied, which doesn't happen.

---

## User's Specific Concerns Addressed

### Concern 1: "Velocity is not rhythmic"
**Verdict: CORRECT**

Velocity/dynamics are not part of groove/rhythm. They should be owned by:
- Instrument agents (drummer, guitarist, etc.)
- Performance rendering layer (Stage 17)

### Concern 2: "Pop Rock snare velocity wouldn't always be in a narrow range"
**Verdict: PARTIALLY CORRECT**

Looking at `GrooveTestSetup.BuildDefaultAccentPolicy()`:
```csharp
[GrooveRoles.Snare] = new()
{
    [OnsetStrength.Backbeat] = new VelocityRule { Min = 95, Max = 127, Typical = 112, AccentBias = 5 },
    [OnsetStrength.Ghost] = new VelocityRule { Min = 20, Max = 50, Typical = 35, AccentBias = 0 },
}
```

This is:
- ❌ **Too narrow** — real drummers vary significantly within and across bars
- ❌ **In the wrong place** — drummer dynamics belong in the drummer agent
- ✅ **Useful as fallback** — if no agent provides velocity, *some* value is needed

### Concern 3: "Ducking/adjustment is still not groove"
**Verdict: CORRECT**

Ducking (reducing velocity when motifs are active) is a **coordination** concern, not a groove concern. It belongs in:
- Cross-role coordination (Stage 15 "Band Brain")
- Individual agent awareness of motif presence (Story 9.3)

---

## Recommended Actions

### Option A: Full Removal (Not Recommended Now)
Remove all velocity code from Groove system.

**Problem:** Bass, Comp, Keys, Pads don't have agents yet. Without Groove velocity fallback, these roles would have no velocity assignment at all.

### Option B: Minimal Fallback (Recommended)

#### Step 1: Simplify `GrooveAccentPolicy` to bare minimum
Remove per-role velocity configuration. Keep only a single global default:

```csharp
public sealed class GrooveAccentPolicy
{
    // REMOVE: RoleStrengthVelocity per-role mappings for drums
    // KEEP: Simple global fallback only
    public VelocityRule GlobalDefault { get; set; } = new() 
    { 
        Min = 60, Max = 100, Typical = 80, AccentBias = 0 
    };
}
```

#### Step 2: Fix the VelocityHint → Velocity flow
Modify `VelocityShaper` to respect VelocityHint from candidates:

```csharp
// Check for VelocityHint tag from candidate mapper
if (TryGetVelocityHintFromTags(onset.Tags, out int hintVelocity))
{
    result.Add(onset with { Velocity = hintVelocity });
    continue;
}
```

This ensures DrummerVelocityShaper's work is respected.

#### Step 3: Document the transitional architecture
The velocity architecture is:
1. **Drummer agent** → sophisticated intent-based velocity via `DrummerVelocityHintSettings`
2. **Future agents** → will implement their own velocity systems (Stories 11-14)
3. **Groove VelocityShaper** → temporary fallback for roles without agents

#### Step 4: Remove drum entries from `GrooveAccentPolicy` test setup
In `GrooveTestSetup.BuildDefaultAccentPolicy()`, remove Kick, Snare, ClosedHat, OpenHat entries since those should come from DrummerVelocityShaper.

### Option C: Deferred Cleanup (Alternative)

Keep current architecture but:
1. Document that Groove velocity is a TEMPORARY fallback
2. Add TODO markers for removal when all instrument agents exist
3. Ensure DrummerVelocityShaper outputs flow through properly

---

## Evidence for Recommendations

### Supporting the User's Position:

1. **NorthStar Stage 17** — "Performance Rendering (Full Humanization)" is separate from Groove
2. **DrummerVelocityHintSettings** already exists with per-style configuration
3. **DynamicIntent enum** provides genre-agnostic intent classification
4. **Agent architecture** — each instrument owns its own decisions (Story 6.1 comment: "Groove VelocityShaper remains the single place that produces final GrooveOnset.Velocity" — this was the transitional design)

### Counterpoints (why not fully remove):

1. **Bass, Comp, Keys, Pads** have no agents yet
2. **Groove system is tested** with velocity shaping (200+ tests)
3. **Breaking change** — removing velocity would require updating all groove consumers

---

## Architectural Recommendation

The **correct long-term architecture** is:

```
                    ┌─────────────────────────────────────┐
                    │     Instrument Agents               │
                    │  (Drummer, Guitarist, Bassist, etc.)│
                    │                                     │
                    │  Each agent owns:                   │
                    │  - Velocity decisions               │
                    │  - Articulation decisions           │
                    │  - Timing nuance (micro-timing)     │
                    └─────────────────────────────────────┘
                                    │
                                    ▼ hints/intents
                    ┌─────────────────────────────────────┐
                    │       Groove System                 │
                    │                                     │
                    │  Owns ONLY:                         │
                    │  - Onset grid (when notes occur)    │
                    │  - Swing/shuffle feel               │
                    │  - Density/variation decisions      │
                    │  - Role constraints                 │
                    │                                     │
                    │  Does NOT own:                      │
                    │  - Velocity                         │
                    │  - Articulation                     │
                    │  - Agent-specific decisions         │
                    └─────────────────────────────────────┘
                                    │
                                    ▼
                    ┌─────────────────────────────────────┐
                    │   Performance Rendering (Stage 17)  │
                    │                                     │
                    │  Applies:                           │
                    │  - Final velocity values            │
                    │  - Micro-timing adjustments         │
                    │  - Articulation mapping             │
                    │  - Humanization                     │
                    └─────────────────────────────────────┘
```

---

## Conclusion

**The user is correct that velocity doesn't belong in Groove conceptually.** However, practical constraints (no agents for bass/comp/keys yet) require keeping a minimal fallback.

**Immediate action:** 
1. Fix the VelocityHint → Velocity flow so DrummerVelocityShaper's work is respected
2. Remove drum-specific entries from `GrooveAccentPolicy` (let drummer agent handle)
3. Keep minimal global fallback for roles without agents

**Future action (Stages 11-14):** 
As each instrument agent is implemented, that agent takes over velocity decisions for its roles, and the Groove fallback becomes less relevant.

**Stage 17 action:**
Consolidate all performance rendering (velocity, timing, articulation) into a unified Performance layer, removing velocity entirely from Groove.

---

## Files Affected if Refactoring

### To Modify:
- `Generator/Groove/VelocityShaper.cs` — Add VelocityHint tag extraction
- `Generator/Groove/GrooveAccentPolicy.cs` — Consider simplifying to global default only
- `Generator/Groove/GrooveTestSetup.cs` — Remove drum-specific velocity entries

### Tests to Update:
- `Music.Tests/Generator/Groove/VelocityShaperTests.cs` — Update for new behavior

### Documentation:
- `ProjectArchitecture.md` — Document transitional velocity architecture
- `CurrentEpic_HumanDrummer.md` — Clarify Story 6.1 velocity flow

---

## Decision Required

The user should decide:

1. **Do nothing now** — Accept transitional architecture, document it
2. **Minimal fix** — Fix VelocityHint flow, remove drum entries from GrooveAccentPolicy
3. **Full refactor** — Remove velocity from Groove entirely, handle fallback elsewhere

**Recommendation: Option 2 (Minimal fix)** — Fixes the immediate problem without destabilizing the codebase.
