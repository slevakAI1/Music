# Story 7.2 Implementation Summary

## Implementation Date
2025-01-XX

## Acceptance Criteria Status
All 7 acceptance criteria from Story 7.2 have been met:

### 1. ? Create/update `SectionEnergyProfile` as a small strongly-typed model computed per `SongSection`
- **Implemented in**: `Song/Energy/EnergySectionProfile.cs`
- **Key features**:
  - Main profile class with Global, Roles, and Orchestration blocks
  - Stores reference to Section and SectionIndex for context
  - Strongly-typed structure with required properties

### 2. ? Inputs must include: `EnergyArc` target, `SectionType` + `SectionIndex`, style/groove identity
- **Implemented in**: `Song/Energy/EnergyProfileBuilder.cs`
- **Method signature**: `BuildProfile(EnergyArc, Section, sectionIndex, previousProfile?)`
- **Key features**:
  - Consumes EnergyArc via `GetTargetForSection()`
  - Uses Section.SectionType and sectionIndex parameter
  - Accesses groove name via `energyArc.GrooveName`

### 3. ? Global block with `Energy`, `TensionTarget`, `ContrastBias`
- **Implemented in**: `Song/Energy/EnergySectionProfile.cs` - `EnergyGlobalTargets` class
- **Properties**:
  - `Energy` [0..1]: target energy level
  - `TensionTarget` [0..1]: tension/anticipation level
  - `ContrastBias` [0..1]: difference from previous section
- **Computation**: Derived in `EnergyProfileBuilder.BuildGlobalTargets()`

### 4. ? Per-role `RoleProfile` for Bass/Comp/Keys/Pads/Drums with required controls
- **Implemented in**: 
  - `Song/Energy/EnergyRoleProfile.cs` - individual role profile
  - `Song/Energy/EnergySectionProfile.cs` - `EnergyRoleProfiles` container
- **Controls per role**:
  - `DensityMultiplier` [typically 0.5-2.0]: note count/activity scaling
  - `VelocityBias` [typically -20 to +20]: dynamics adjustment
  - `RegisterLiftSemitones` [typically -24 to +24]: register shift
  - `BusyProbability` [0..1]: variation/embellishment rate
- **Per-role derivation**: Separate `BuildXxxProfile()` methods for role-specific mapping

### 5. ? Orchestration block with role presence/featured and cymbal language hints
- **Implemented in**: `Song/Energy/EnergyOrchestrationProfile.cs`
- **Role presence flags**: BassPresent, CompPresent, KeysPresent, PadsPresent, DrumsPresent
- **Cymbal controls**:
  - `CymbalLanguage` enum (Minimal/Standard/Intense)
  - `CrashOnSectionStart` (bool)
  - `PreferRideOverHat` (bool)
- **Section-aware logic**: First verse sparse, chorus full, etc.

### 6. ? High energy needs multiple levers (not just velocity)
- **Implementation**: Each role's `BuildXxxProfile()` method maps energy to all 4 factors:
  1. **Dynamics** (velocity bias): -20 to +20 range per role
  2. **Density** (density multiplier): role-specific min/max (e.g., Keys: 0.5-1.6)
  3. **Register** (register lift): role-specific (Keys: -12 to +24, Bass: stays low)
  4. **Rhythmic activity** (busy probability): 0.2-0.9 range per role
- **Orchestration layer**: Role presence changes with energy (pads absent in low-energy verse)
- **Tests validate**: Multi-factor variation (TestEnergyFactors)

### 7. ? Deterministic derivation
- **Guaranteed by**: Linear mapping functions without randomness
- **Functions**: MapEnergyToDensity, MapEnergyToVelocityBias, MapEnergyToRegisterLift, MapEnergyToBusy
- **Validated by**: TestDeterminism (same inputs ? same outputs)

## Files Created

1. **Song/Energy/EnergyRoleProfile.cs**
   - Per-role control parameters
   - Factory methods (Neutral, LowEnergy, HighEnergy)
   - 4 core parameters: Density, Velocity, Register, Busy

2. **Song/Energy/EnergyOrchestrationProfile.cs**
   - Role presence flags
   - Cymbal language enum and controls
   - Factory methods (Full, Sparse, HighEnergy)

3. **Song/Energy/EnergySectionProfile.cs**
   - Main profile class (Global + Roles + Orchestration)
   - EnergyGlobalTargets class
   - EnergyRoleProfiles container class

4. **Song/Energy/EnergyProfileBuilder.cs**
   - Profile derivation logic
   - Per-role mapping functions (BuildBassProfile, etc.)
   - Energy-to-control linear mapping helpers
   - Tension and contrast computation

5. **Song/Energy/EnergySectionProfileTests.cs**
   - 9 comprehensive test methods
   - Validates all acceptance criteria
   - Tests determinism, ranges, multi-factor mapping

## Design Decisions

### Role-Specific Parameter Ranges
Each role has tuned min/max ranges for energy mapping:

| Role | Density | Velocity | Register | Busy |
|------|---------|----------|----------|------|
| Bass | 0.8-1.3 | -15 to +15 | 0 (stays low) | 0.2-0.7 |
| Comp | 0.6-1.5 | -20 to +20 | 0 to +12 | 0.3-0.8 |
| Keys | 0.5-1.6 | -20 to +20 | -12 to +24 | 0.2-0.7 |
| Pads | 0.7-1.4 | -15 to +15 | 0 to +12 | 0.1-0.5 |
| Drums | 0.7-1.6 | -15 to +20 | 0 (N/A) | 0.2-0.9 |

**Rationale**:
- Bass: Limited density/register variation to maintain foundation
- Comp: Moderate register lift to avoid lead space
- Keys: Widest range for dramatic contrast
- Pads: Sustained role, lower busy probability
- Drums: High density/busy sensitivity for dynamic performance

### Tension Computation
**Approach**: Tension tracks energy but at reduced magnitude
- Base tension = energy × 0.5
- Section-type adjustments:
  - Verse: baseline
  - Chorus: 0.8× (resolves tension)
  - Bridge: 1.3× (builds tension)
  - Intro/Outro: reduced (0.7×/0.5×)

**Rationale**: Tension is related to but distinct from energy (Story 7.5 will expand this)

### Contrast Bias Computation
**Formula**: Absolute difference between current and previous section energy
```csharp
contrastBias = Math.Abs(currentEnergy - previousEnergy)
```
**Range**: Automatically [0..1] by definition
**Usage**: Will drive A/A'/B variation decisions (Story 7.6)

### Orchestration Logic (Section-Type-Aware)

**Intro**:
- Pads present only if energy > 0.3
- Keys present only if energy > 0.4
- Gradual build-up approach

**Verse 1** (sectionIndex == 0):
- Pads present only if energy > 0.4
- Keys present only if energy > 0.5
- Typically sparser than Verse 2+

**Chorus**:
- All roles present by default
- Crash on section start if energy > 0.6
- Full arrangement typical

**Outro**:
- Pads present only if energy > 0.3
- Wind-down approach

### Cymbal Language
**Energy thresholds**:
- < 0.4: Minimal (closed hats, rare crashes)
- 0.4-0.7: Standard (balanced)
- > 0.7: Intense (frequent crashes, ride preference)

**Ride preference**: energy > 0.7

## Integration Points for Story 7.3

The implementation provides these integration contracts for Story 7.3 (wiring to role renderers):

### For All Roles
1. **Access profile**: `EnergySectionProfile profile = /* from generator context */`
2. **Check presence**: `if (!profile.Orchestration.BassPresent) return;`
3. **Apply controls**:
   - `densityMultiplier = profile.Roles.Bass.DensityMultiplier`
   - `velocityBias = profile.Roles.Bass.VelocityBias`
   - `registerLift = profile.Roles.Bass.RegisterLiftSemitones`
   - `busyProbability = profile.Roles.Bass.BusyProbability`

### Role-Specific Guardrails (Story 7.3)
Must be implemented in role generators:
- **Bass**: Clamp register to stay in low range (e.g., E1-E3)
- **Comp**: Keep top note below lead-space ceiling (e.g., < C5)
- **Keys/Pads**: Avoid lead-space ceiling for sustained notes
- **Pads**: Avoid future vocal band (placeholder for Story 9)
- **Drums**: Cap density to prevent overwhelming other roles

### Drums Integration (Story 6 knobs)
Map profile to existing drum parameters:
- `DensityMultiplier` ? ghost note frequency, fill rate
- `VelocityBias` ? baseline velocity adjustment
- `BusyProbability` ? fill probability, variation rate
- `CymbalLanguage` ? crash placement rules
- `CrashOnSectionStart` ? section-start crash flag
- `PreferRideOverHat` ? ride vs hat selection

## Testing

All unit tests pass and validate:
- ? Profile derivation from energy arc
- ? Per-role profile mapping (all roles have profiles)
- ? Orchestration logic (presence, cymbal language)
- ? Tension computation (in range, section-type-aware)
- ? Contrast bias (relative to previous section)
- ? Multi-factor energy (not just velocity)
- ? Range constraints (all values within valid ranges)
- ? Section-type specifics (verse sparse, chorus full)
- ? Determinism (same inputs ? same outputs)

## Notes

- All mapping functions are linear (energy × range)
- No randomness in profile derivation (deterministic)
- Role-specific ranges tuned for musical effectiveness
- Orchestration rules are heuristic-based (can be refined)
- Tension model is simplified (Story 7.5 will expand)
- Contrast bias computation is geometric (absolute difference)
- Tests use PopGroove and RockGroove for variety
- Profile builder is stateless (pure functions)

## Next Steps (Story 7.3)

Wire profiles into role generators:
1. Update Generator.cs to create EnergyArc
2. Build profiles for each section before generation
3. Pass profiles to role generators
4. Apply controls in each generator:
   - Density: scale note count/activity
   - Velocity: apply bias with MIDI clamping
   - Register: shift MIDI notes (with guardrails)
   - Busy: control variation/embellishment probability
5. Respect orchestration presence flags
6. Implement role-specific guardrails
