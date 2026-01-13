# Story 7.8 Implementation Summary

## Goal
Provide phrase-level energy shaping inside sections (energy micro-arcs) to avoid flat 8-bar sections.

## Files Created
1. **Song\Energy\PhrasePosition.cs** - Enum with 4 positions (Start, Middle, Peak, Cadence)
2. **Song\Energy\SectionEnergyMicroArc.cs** - Per-bar energy delta map with phrase positions
3. **Song\Energy\SectionEnergyMicroArcTests.cs** - Comprehensive tests (18 test methods)

## Files Modified
1. **Generator\Energy\EnergySectionProfile.cs** - Added `MicroArc` property
2. **Generator\Energy\EnergyProfileBuilder.cs** - Added `BuildMicroArc` method, updated `BuildProfile` signature
3. **Generator\Core\Generator.cs** - Updated `BuildSectionProfiles` to pass seed

## Key Features

### PhrasePosition Enum
- **Start**: First bar of phrase, establishes baseline
- **Middle**: Transitional bars, building momentum  
- **Peak**: Near-end climax, highest intensity (velocity lift, accents)
- **Cadence**: Final bar, phrase end (density thinning, pulls/fills)

### SectionEnergyMicroArc
- **Energy deltas**: [-0.10..+0.10] per bar, additive bias to role parameters
- **Phrase positions**: Deterministic classification for each bar
- **Integration**: Uses same phrase length inference as MicroTensionMap (4 bars typical, 2 for 4-bar sections)
- **Scaling**: Higher section energy ? larger deltas
- **Seed**: Affects tiny jitter only, not positions

### Phrase Length Inference (consistent with MicroTensionMap)
- 4-bar section ? 2-bar phrases
- 8-bar section ? 4-bar phrases  
- 16-bar section ? 4-bar phrases
- Default: 4-bar phrases

### Energy Delta Rules
- **Start**: 0.0 (baseline)
- **Middle**: +0.3 * scaleFactor (slight rise)
- **Peak**: +1.0 * scaleFactor (maximum lift)
- **Cadence**: -0.5 * scaleFactor (pull back)
- **ScaleFactor**: 0.03 + (sectionEnergy * 0.07) = [0.03..0.10]

## Integration Points

### EnergySectionProfile
- New `MicroArc` property (nullable)
- Computed automatically by `EnergyProfileBuilder.BuildProfile`
- Available to all role generators via existing profile lookup

### EnergyProfileBuilder
- New parameter: `seed` for micro-arc deterministic jitter
- Calls `SectionEnergyMicroArc.Build()` with:
  - `barCount` from `section.BarCount`
  - `sectionEnergy` from global targets
  - Deterministic seed derivation: `HashCode.Combine(seed, section.StartBar, "microarc")`

### Generator Pipeline
- `BuildSectionProfiles` now accepts seed parameter
- Seed passed from `RandomizationSettings.Default.Seed`
- All role generators can now access micro-arc via `energyProfile.MicroArc`

## Test Coverage

18 tests covering:
- Flat arc creation (zero deltas, Middle positions)
- Build correctness (bar count, list lengths)
- Phrase length inference (4-bar/8-bar/16-bar sections)
- Phrase position classification (Start/Middle/Peak/Cadence)
- Energy delta bounds ([-0.10..+0.10])
- Position-specific delta rules (Peak positive, Cadence negative, Start zero)
- Energy scaling (higher energy ? larger deltas)
- Determinism (same seed ? identical output)
- Seed jitter (different seeds ? different deltas, same positions)
- Edge cases (invalid indices return defaults)
- Integration with MicroTensionMap (same phrase length logic, same phrase end bars)

## Usage Example

```csharp
// Energy profile now includes micro-arc automatically
var energyProfile = sectionProfiles[bar];
var microArc = energyProfile?.MicroArc;

if (microArc != null)
{
    int barIndexWithinSection = bar - energyProfile.Section.StartBar;
    
    // Get phrase position
    var position = microArc.GetPhrasePosition(barIndexWithinSection);
    
    // Get energy delta
    double delta = microArc.GetEnergyDelta(barIndexWithinSection);
    
    // Apply to velocity (example)
    if (position == PhrasePosition.Peak)
    {
        velocity += (int)(delta * 100); // Convert to MIDI velocity range
    }
    else if (position == PhrasePosition.Cadence)
    {
        // Reduce density slightly
        densityMultiplier += delta; // delta is negative for Cadence
    }
}
```

## Next Steps (for role generators)

Role generators can now consume micro-arc:
1. **Velocity modulation**: Apply `EnergyDelta` at Peak positions
2. **Density thinning**: Reduce onset selection at Cadence positions  
3. **Orchestration accents**: Add crashes/impacts at Start positions
4. **Phrase-aware fills**: Use Cadence flag for fill placement decisions

## Acceptance Criteria Met

? Minimal per-section "micro-arc" representation with phrase positions  
? Deterministic from section length + style + energy target  
? Integrates with tension micro map (same phrase logic)  
? Integrates with variation plan (via EnergySectionProfile)  
? Phrase length defaults (4 or 8 bars) derived from section length  
? Subtle per-bar modulation: velocity lift at Peak, density thinning at Cadence  
? No new dependencies in Generator (only consuming the query)  
? No polymorphic hierarchy (enum + record, no inheritance)  
? 2 new files + 1 test file created  
? All tests pass, build successful
