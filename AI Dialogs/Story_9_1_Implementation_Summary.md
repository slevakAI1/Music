# Story 9.1 Implementation Summary

## Overview
Story 9.1 implements the MotifPlacementPlanner system that deterministically selects WHICH motifs appear WHERE in song structure.

## Files Created

### Data Models
1. **Song/Material/MotifPlacement.cs**
   - Immutable record describing a single motif placement
   - Fields: MotifId, AbsoluteSectionIndex, StartBarWithinSection, DurationBars, VariationIntensity, TransformTags
   - Factory method with validation

2. **Song/Material/MotifPlacementPlan.cs**
   - Container for complete placement plan
   - Ordered list of placements
   - Query methods for section-level access

### Planning Logic
3. **Song/Material/MotifPlacementPlanner.cs**
   - Deterministic placement planning
   - MVP heuristics:
     - Chorus: almost always gets hook motif (highest energy)
     - Intro: optional teaser if low energy
     - Verse: optional riff if mid-high energy  
     - Bridge: new or transformed motif (contrast)
     - Solo: featured motif
     - Outro: optional if not too sparse
   - Respects orchestration constraints (role presence)
   - Supports A/A' reuse via SectionVariationPlan
   - Transform tag generation based on variation intensity

### Tests
4. **Song/Material/Tests/MotifPlacementPlannerTests.cs**
   - 14 comprehensive test methods
   - Coverage:
     - Determinism (same inputs → same output)
     - Empty bank handling
     - Orchestration constraint compliance
     - Section-type specific heuristics (Chorus, Verse, Bridge, Intro)
     - Common song form (Intro-V-C-V-C-Bridge-C-Outro)
     - Seed sensitivity (different seeds → different choices)
     - Variation intensity bounds [0..1]
     - A/A' reuse logic
     - Transform tag generation
     - Placement bounds (within section limits)
   - Test helper implementations of ISongIntentQuery

5. **Song/Material/Tests/Stage9TestRunner.cs**
   - Consolidated test runner for Stage 9
   - Follows existing Stage8TestRunner pattern

## Key Design Decisions

### Determinism
- All placement decisions deterministic by (seed, section structure, energy/tension, motif availability)
- Hash-based selection for tie-breaking
- No randomness; same inputs always produce same plan

### Constraints
- Role presence checked via ISongIntentQuery.RolePresence
- Lead/vocal roles conceptually always present (reserve space)
- Other roles checked against orchestration intent

### A/A' Variation
- Repeated section types reuse same motif
- BaseReferenceSectionIndex from SectionVariationPlan drives reuse
- VariationIntensity copied from section intent
- Transform tags generated based on variation context

### MVP Placement Heuristics
Section-type specific rules:
- **Chorus** (energy > 0.3 OR 80% probability): Primary hook placement
- **Intro** (energy < 0.4 AND 50% probability): Optional teaser
- **Verse** (energy > 0.5 AND 60% probability): Optional riff
- **Bridge** (70% probability): Contrast motif
- **Solo** (80% probability): Featured motif
- **Outro** (energy > 0.3 AND 40% probability): Optional closure

### Material Kind Selection
- Chorus/Bridge → Hook
- Verse (high energy) → Riff
- Verse (low energy) → MelodyPhrase
- Solo → Riff
- Intro → Hook

### Duration and Timing
- Full section duration for Chorus and high-energy sections
- Half section possible for Verse and lower energy (50% probability)
- Start bar typically 0
- PreChorus builds may delay entry by 2 bars (30% probability)

### Transform Tags
Based on VariationIntensity and context:
- High variation (> 0.6) enables "Syncopate" or "OctaveUp" (probabilistic)
- "Lift" variation tag suggests "OctaveUp" transform (40% probability)

## Dependencies
- **Stage M1**: Material system (PartTrack, MaterialBank, PartTrackDomain, PartTrackKind, MaterialKind)
- **Stage 7**: Energy/tension/variation system (ISongIntentQuery, SectionVariationPlan)
- **Existing**: SectionTrack, Section, MusicConstants.eSectionType

## Integration Points
Story 9.1 outputs consumed by:
- **Story 9.2** (MotifRenderer): Converts placements into actual note sequences
- **Story 9.3** (Accompaniment integration): Ducking/spacing based on motif presence
- **Story 9.4** (Diagnostics): Placement visualization and analysis

## Testing
Run tests via:
```csharp
Stage9TestRunner.RunAllStage9Tests();
```

All 14 tests pass:
- Determinism verified
- Constraints respected
- Heuristics produce sensible output
- A/A' logic works correctly
- Edge cases handled (empty bank, bounds, etc.)

## Acceptance Criteria Status
✓ Create MotifPlacementPlanner that outputs MotifPlacementPlan
✓ Deterministic selection using seed + song structure + Stage 7 intent + MaterialBank
✓ MVP placement heuristics (Chorus, Intro, PreChorus, Bridge, Verse)
✓ Collision checks (orchestration role presence)
✓ A/A' reuse support via SectionVariationPlan
✓ Output structure: List<MotifPlacement> with all required fields
✓ Tests: Determinism, orchestration constraints, common forms, seed sensitivity

## Notes
- Register collision detection (avoiding dense motifs in same band) deferred to Story 9.3 (accompaniment integration)
- Phrase map integration uses fallback when phrase data not available (phrase map system not yet implemented)
- Current implementation uses probabilistic placement decisions; future enhancement could make these configurable via style policies
