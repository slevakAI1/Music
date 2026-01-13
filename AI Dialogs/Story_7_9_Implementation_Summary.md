# Story 7.9 Implementation Summary

## Overview

Story 7.9 implements a unified Stage 7 intent query surface (`ISongIntentQuery`) that aggregates energy, tension, variation, and orchestration information into a single stable contract for Stage 8/9 motif placement and melody generation.

**Key principle**: No new planners or dependencies. This is pure query aggregation of existing Stage 7 outputs.

---

## What Was Created

### Core Files

1. **`Generator\Energy\ISongIntentQuery.cs`** - Query interface and context records
   - `ISongIntentQuery` interface with `GetSectionIntent` and `GetBarIntent` methods
   - `SectionIntentContext` - Section-level macro intent aggregation
   - `BarIntentContext` - Bar-level micro intent with phrase positions
   - `RolePresenceHints` - Orchestration hints from energy profile
   - `RegisterConstraints` - Lead space ceiling, bass floor, vocal band
   - `RoleDensityCaps` - Maximum density caps per role

2. **`Generator\Energy\DeterministicSongIntentQuery.cs`** - Implementation
   - Precomputes and caches section contexts at construction (O(1) lookup)
   - Aggregates existing `EnergySectionProfile`, `ITensionQuery`, `IVariationQuery`
   - No new planning logic, only query composition

3. **`Generator\Energy\Tests\SongIntentQueryTests.cs`** - Comprehensive test suite
   - 16 test methods covering all acceptance criteria
   - Verifies determinism, aggregation correctness, edge cases
   - 100% passing

4. **`Generator\Energy\Tests\RunSongIntentQueryTests.cs`** - Test runner
   - Call `RunSongIntentQueryTests.Run()` to execute all tests

---

## Usage Examples

### Creating a Song Intent Query

```csharp
// Assuming you already have these from Generator.cs:
var energyArc = EnergyArc.Create(sectionTrack, grooveName, seed);
var sectionProfiles = BuildSectionProfiles(energyArc, sectionTrack, seed);
var tensionQuery = new DeterministicTensionQuery(energyArc, seed);
var variationQuery = new DeterministicVariationQuery(sectionTrack, energyArc, tensionQuery, grooveName, seed);

// Create unified intent query:
ISongIntentQuery intentQuery = new DeterministicSongIntentQuery(
    sectionProfiles,
    tensionQuery,
    variationQuery);
```

### Querying Section-Level Intent (Motif Placement)

```csharp
// Get section intent for motif placement decision
var sectionIntent = intentQuery.GetSectionIntent(absoluteSectionIndex: 2);

// Check if this is a good section for a primary motif
if (sectionIntent.SectionType == MusicConstants.eSectionType.Chorus &&
    sectionIntent.Energy > 0.6 &&
    sectionIntent.TransitionHint == SectionTransitionHint.Build)
{
    // Place primary motif here
    // Use sectionIntent.RegisterConstraints.LeadSpaceCeiling to stay out of lead space
    // Use sectionIntent.RolePresence to check which roles are active
}

// Check variation relationship for motif repetition
if (sectionIntent.BaseReferenceSectionIndex.HasValue)
{
    // This is an A' or B section - vary the motif from base section
    var baseIntent = intentQuery.GetSectionIntent(sectionIntent.BaseReferenceSectionIndex.Value);
    var variationIntensity = sectionIntent.VariationIntensity;
    
    // Apply bounded motif variation operators based on intensity
}
```

### Querying Bar-Level Intent (Melody Timing)

```csharp
// Get bar intent for syllable placement decision
var barIntent = intentQuery.GetBarIntent(
    absoluteSectionIndex: 2,
    barIndexWithinSection: 3);

// Check phrase position for melodic decisions
if (barIntent.PhrasePosition == PhrasePosition.Peak)
{
    // Place highest note or most stressed syllable here
    // Use barIntent.EffectiveEnergy for velocity/intensity
}

if (barIntent.IsPhraseEnd)
{
    // This is a cadence point - use stable pitch, allow rest
    // Tension might be high (anticipation) or low (release)
    if (barIntent.MicroTension > 0.6)
    {
        // High tension cadence - half cadence, leave unresolved
    }
    else
    {
        // Low tension cadence - authentic cadence, resolve to tonic
    }
}

// Adjust arrangement density under melody
if (barIntent.Section.RolePresence.PadsPresent)
{
    // Reduce pad density in vocal band during active melody
    var vocalBand = barIntent.Section.RegisterConstraints.VocalBand;
    // ... apply ducking logic ...
}
```

### Checking Register Constraints (Arrangement Collision Prevention)

```csharp
var sectionIntent = intentQuery.GetSectionIntent(sectionIndex);
var constraints = sectionIntent.RegisterConstraints;

// For comp/keys voicing decisions:
if (voicingTopNote >= constraints.LeadSpaceCeiling)
{
    // Transpose down to avoid lead space
    voicingTopNote -= 12;
}

// For bass pattern decisions:
if (bassNote < constraints.BassFloor)
{
    // This is too low for comp - stay above bass floor
}

// For pad/keys sustain decisions (when melody exists):
var (vocalMin, vocalMax) = constraints.VocalBand;
if (sustainedNote >= vocalMin && sustainedNote <= vocalMax)
{
    // This note is in vocal band - shorten sustain or omit
}
```

### Using Density Caps (Cross-Role Thinning)

```csharp
var sectionIntent = intentQuery.GetSectionIntent(sectionIndex);
var caps = sectionIntent.DensityCaps;

// For bass approach note decisions:
if (currentBassApproachProbability > caps.Bass)
{
    // Reduce approach note rate to stay under cap
    currentBassApproachProbability = caps.Bass;
}

// For comp rhythm pattern selection:
if (patternDensity > caps.Comp)
{
    // Select sparser pattern or thin onsets
}

// Density caps vary with section energy:
// - Low energy (< 0.3): Low caps (bass=0.60, comp=0.65, drums=0.70)
// - Mid energy: Default caps (bass=0.85, comp=0.90, drums=0.90)
// - High energy (> 0.7): High caps (bass=1.0, comp=1.0, drums=1.0)
```

---

## Context Record Structure

### `SectionIntentContext` (Section-Level)

| Property | Type | Source | Purpose |
|----------|------|--------|---------|
| `AbsoluteSectionIndex` | `int` | Position | 0-based section index in song |
| `SectionType` | `eSectionType` | `EnergySectionProfile.Section` | Verse/Chorus/Bridge/etc |
| `Energy` | `double [0..1]` | `EnergySectionProfile.Global` | Target energy level |
| `Tension` | `double [0..1]` | `ITensionQuery.GetMacroTension` | Anticipation/release level |
| `TensionDrivers` | `TensionDriver` | `ITensionQuery.GetMacroTension` | Why tension has this value |
| `TransitionHint` | `SectionTransitionHint` | `ITensionQuery.GetTransitionHint` | Build/Release/Sustain/Drop |
| `VariationIntensity` | `double [0..1]` | `IVariationQuery.GetVariationPlan` | How much to vary from base |
| `BaseReferenceSectionIndex` | `int?` | `IVariationQuery.GetVariationPlan` | Which section to vary from (A/A'/B) |
| `VariationTags` | `IReadOnlySet<string>` | `IVariationQuery.GetVariationPlan` | Tags like "A", "A'", "B", "Lift" |
| `RolePresence` | `RolePresenceHints` | `EnergySectionProfile.Orchestration` | Which roles active + cymbal hints |
| `RegisterConstraints` | `RegisterConstraints` | Standardized constants | Lead ceiling, bass floor, vocal band |
| `DensityCaps` | `RoleDensityCaps` | Derived from energy | Max density per role |

### `BarIntentContext` (Bar-Level)

| Property | Type | Source | Purpose |
|----------|------|--------|---------|
| `Section` | `SectionIntentContext` | Parent section | All section-level intent |
| `BarIndexWithinSection` | `int` | Position | 0-based bar index in section |
| `MicroTension` | `double [0..1]` | `ITensionQuery.GetMicroTension` | Bar-level tension |
| `EnergyDelta` | `double [-0.10..+0.10]` | `SectionEnergyMicroArc` | Additive energy modifier |
| `PhrasePosition` | `PhrasePosition` | `SectionEnergyMicroArc` | Start/Middle/Peak/Cadence |
| `IsPhraseEnd` | `bool` | `ITensionQuery.GetPhraseFlags` | Last bar of phrase |
| `IsSectionEnd` | `bool` | `ITensionQuery.GetPhraseFlags` | Last bar of section |
| `IsSectionStart` | `bool` | `ITensionQuery.GetPhraseFlags` | First bar of section |
| `EffectiveEnergy` | `double [0..1]` | Computed | `Section.Energy + EnergyDelta` (clamped) |

---

## Integration Points

### Current Generator.cs (No Changes Required Yet)

The existing `Generator.cs` already creates all necessary inputs:
- `EnergyArc` (line 56-59)
- `sectionProfiles` (line 62)
- `ITensionQuery` (line 65)
- `IVariationQuery` (line 69-74)

**Story 7.9 is complete without modifying Generator.cs**. The query can be created on-demand when Stage 8/9 motif/melody systems are implemented.

### Future Stage 8 Motif Placement

```csharp
// Pseudocode for future motif placement system:
ISongIntentQuery intentQuery = new DeterministicSongIntentQuery(sectionProfiles, tensionQuery, variationQuery);

foreach (var section in sectionTrack.Sections)
{
    var sectionIntent = intentQuery.GetSectionIntent(sectionIndex);
    
    // Decide if motif belongs in this section
    if (ShouldPlaceMotif(sectionIntent))
    {
        // Decide which bars within section
        for (int bar = 0; bar < section.BarCount; bar++)
        {
            var barIntent = intentQuery.GetBarIntent(sectionIndex, bar);
            
            if (barIntent.PhrasePosition == PhrasePosition.Peak)
            {
                // Place motif peak notes here
            }
        }
    }
}
```

### Future Stage 9 Melody Generation

```csharp
// Pseudocode for future melody system:
ISongIntentQuery intentQuery = new DeterministicSongIntentQuery(sectionProfiles, tensionQuery, variationQuery);

foreach (var syllable in syllableTimingPlan)
{
    var barIntent = intentQuery.GetBarIntent(syllable.SectionIndex, syllable.BarIndex);
    
    // Choose pitch based on intent
    int pitch = GeneratePitch(
        syllable,
        barIntent.Section.Energy,
        barIntent.MicroTension,
        barIntent.PhrasePosition,
        barIntent.Section.RegisterConstraints.VocalBand);
    
    // Adjust velocity based on effective energy
    int velocity = (int)(64 + barIntent.EffectiveEnergy * 40);
    
    // Signal arrangement to duck under this syllable
    if (barIntent.Section.RolePresence.PadsPresent)
    {
        DuckPadsInVocalBand(barIntent);
    }
}
```

---

## Design Decisions

### Why No New Planners?

Story 7.9 is strictly a **query aggregation layer**. All planning logic already exists in:
- `EnergyArc` + `EnergyProfileBuilder` (energy targets + role profiles)
- `DeterministicTensionQuery` (tension + transition hints)
- `DeterministicVariationQuery` (A/A'/B variation plans)

Adding new planners would violate the story's constraint: "If a value can be computed from existing objects, do not introduce a planner."

### Why Precompute Section Contexts?

Section contexts are queried frequently (once per section for motif decisions, once per bar for melody decisions). Precomputing at construction ensures O(1) lookup and eliminates redundant computation.

### Why Include Register Constraints?

Current generators hard-code these constants:
- `KeysTrackGenerator.cs` line 319: `const int LeadSpaceCeiling = 72;`
- Comp register guardrails: implicit bass floor at E3 (MIDI 52)

Story 7.9 standardizes these as queryable contract so Stage 8/9 can respect them without duplicating constants.

### Why Include Density Caps?

Stage 8 cross-role thinning (future) will need maximum density limits. Current generators have implicit caps (e.g., drums never exceed groove template density). Story 7.9 makes these explicit and queryable.

---

## Testing

### Running Tests

```csharp
// From any test harness or main program:
Music.Generator.Tests.RunSongIntentQueryTests.Run();
```

### Test Coverage

| Test | Purpose |
|------|---------|
| `TestSectionIntentAggregation` | Verifies energy profile data aggregated correctly |
| `TestBarIntentAggregation` | Verifies section + bar-level data combined correctly |
| `TestDeterminism` | Same inputs ? same outputs |
| `TestRolePresenceMapping` | Orchestration hints map correctly |
| `TestRegisterConstraints` | Lead ceiling, bass floor, vocal band correct |
| `TestDensityCapsByEnergy` | Caps vary with energy (low/mid/high) |
| `TestVariationIntegration` | Variation plan data propagated correctly |
| `TestTensionIntegration` | Tension + drivers + transition hints correct |
| `TestMicroArcIntegration` | Energy delta + phrase position from micro-arc |
| `TestPhrasePositionFlags` | Phrase end, section start/end flags correct |
| `TestTransitionHintPropagation` | Build/Release/Sustain/Drop hints correct |
| `TestInvalidSectionHandling` | Out-of-range indices throw/return false |
| `TestCacheConsistency` | Cached contexts match fresh computation |
| `TestEffectiveEnergyCalculation` | Section energy + delta, clamped [0..1] |
| `TestVocalBandReservation` | Vocal band sensible for future melody |
| `TestSectionCount` | Section count matches profile count |

**All tests passing ?**

---

## Acceptance Criteria Verification

### ? Provide single Stage 7 query surface with immutable context object

- `ISongIntentQuery` interface provides `GetSectionIntent` and `GetBarIntent`
- Returns immutable `SectionIntentContext` and `BarIntentContext` records

### ? Must have GetSectionIntent(sectionIndex) and GetBarIntent(sectionIndex, barIndex)

- Both methods implemented in `DeterministicSongIntentQuery`
- Tests verify correct behavior and out-of-range handling

### ? Context includes: energy target, tension target, tension drivers, transition hint, variation plan summary, role presence, register bands, density caps

| Required | Property in Context | Source |
|----------|-------------------|--------|
| Energy target | `Energy` | `EnergySectionProfile.Global` |
| Tension target | `Tension` | `ITensionQuery.GetMacroTension` |
| Tension drivers | `TensionDrivers` | `SectionTensionProfile.Driver` |
| Transition hint | `TransitionHint` | `ITensionQuery.GetTransitionHint` |
| Variation plan summary | `VariationIntensity`, `BaseReferenceSectionIndex`, `VariationTags` | `IVariationQuery.GetVariationPlan` |
| Role presence/orchestration | `RolePresence` | `EnergyOrchestrationProfile` |
| Reserved register bands | `RegisterConstraints` | Standardized constants |
| Density caps | `DensityCaps` | Derived from energy level |

### ? Keep Stage 7 as owner of macro intent; Stage 8 may extend with phrase maps

- Story 7.9 aggregates existing Stage 7 outputs without adding new planning
- `BarIntentContext` includes `PhrasePosition` and flags from existing `SectionEnergyMicroArc` and `MicroTensionMap`
- Stage 8 can extend with formal `PhraseMap` without breaking this contract

### ? Motifs can query energy/tension targets, phrase positions, register intent

- `SectionIntentContext` provides `Energy`, `Tension`, `RegisterConstraints`
- `BarIntentContext` provides `PhrasePosition`, `IsPhraseEnd`, `IsSectionEnd`
- `RegisterConstraints.LeadSpaceCeiling` reserves space for future lead/motif

### ? Melody/lyrics can request arrangement ducking and register avoidance

- `RolePresence` indicates which roles are active for ducking decisions
- `RegisterConstraints.VocalBand` reserves typical vocal range [C4-E5]
- `BarIntentContext.Section` provides access to all section-level intent for ducking logic

---

## Future Work (Out of Scope for Story 7.9)

### Stage 8 Phrase Map Integration

When Stage 8 implements formal `PhraseMap`:
- `BarIntentContext` can add `PhraseMap?` property
- `DeterministicSongIntentQuery` constructor can accept optional `IPhraseMapQuery`
- Phrase position would come from formal phrase map instead of inference

### Stage 8 Cross-Role Thinning

When Stage 8 implements density budgets:
- Use `BarIntentContext.Section.DensityCaps` to enforce maximum densities
- Query `BarIntentContext` to check which roles are active for simultaneous busy-ness decisions

### Stage 9 Motif Placement System

When Stage 9 implements motif placement:
- Use `SectionIntentContext` to decide which sections get motifs
- Use `BarIntentContext.PhrasePosition` to place motif peaks at phrase peaks
- Use `TransitionHint` to vary motif behavior at section boundaries

### Stage 10 Melody Generation

When Stage 10 implements melody:
- Use `RegisterConstraints.VocalBand` to stay in singable range
- Use `BarIntentContext.MicroTension` to shape melodic tension
- Use `RolePresence` to signal arrangement ducking when melody present

---

## Summary

Story 7.9 successfully implements a unified Stage 7 intent query that:
- ? Aggregates existing energy/tension/variation queries without new dependencies
- ? Provides stable contract for Stage 8/9 integration
- ? Maintains strict determinism (same inputs ? same outputs)
- ? Includes comprehensive test coverage (16 tests, all passing)
- ? Requires no changes to existing `Generator.cs` (future-proofed)
- ? Supports motif placement, melody timing, register constraints, and arrangement ducking

**Files created:**
1. `Generator\Energy\ISongIntentQuery.cs` (379 lines)
2. `Generator\Energy\DeterministicSongIntentQuery.cs` (181 lines)
3. `Generator\Energy\Tests\SongIntentQueryTests.cs` (646 lines)
4. `Generator\Energy\Tests\RunSongIntentQueryTests.cs` (24 lines)

**Total: 4 files, 1230 lines, 0 new dependencies, 100% test pass rate.**
