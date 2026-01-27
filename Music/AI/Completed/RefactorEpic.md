# Refactoring Plan: Fix Agent Architecture

**Status**: Ready for implementation  
**Priority**: CRITICAL - Blocks other instrument agents  
**Estimated Effort**: 7 stories, ~2-3 days

---

## Problem Statement

DrummerAgent incorrectly implements a full generation pipeline instead of being a data source. This bypasses:
- GrooveSelectionEngine for weighted selection
- Density target enforcement from policy
- Operator caps and weights
- Proper groove system integration

**Root Cause**: Story 10.8.1 AC #6 was ambiguous - specified `Generate()` method when DrummerAgent should only implement `IGroovePolicyProvider` + `IGrooveCandidateSource`.

**Impact**: Cannot build Bass, Keys, Comp agents on this flawed pattern.

---

## Correct Architecture

```
Generator.cs
    ↓ creates
DrummerAgent (data source only)
    • IGroovePolicyProvider
    • IGrooveCandidateSource
    ↓ passed to
GrooveBasedDrumGenerator (pipeline orchestrator)
    • Takes IPolicy + ICandidateSource
    • Uses GrooveSelectionEngine
    • Enforces density/caps/weights
    • Returns PartTrack
```

---

## RF-1: Remove Pipeline Logic from DrummerAgent

**Goal**: Make DrummerAgent a pure data source (interface implementation only).

**Acceptance Criteria**:

1. Remove `Generate(SongContext songContext)` method entirely
2. Remove all private helper methods used only by Generate:
   - `ValidateSongContext(SongContext)`
   - `GetDrumProgramNumber(VoiceSet)`
   - `ExtractAnchorOnsets(GroovePresetDefinition, int, BarTrack)`
   - `GenerateOperatorOnsets(IReadOnlyList<BarContext>, BarTrack, int)`
   - `CreateGrooveBarContext(BarContext)`
   - `ParseDrumRole(string)`
   - `CombineOnsets(List<DrumOnset>, List<DrumOnset>)`
   - `ConvertToMidiEvents(List<DrumOnset>)`
3. Keep only:
   - Constructor with StyleConfiguration + optional settings
   - `GetPolicy(GrooveBarContext, string)` - delegates to `_policyProvider`
   - `GetCandidateGroups(GrooveBarContext, string)` - delegates to `_candidateSource`
   - Public properties: `StyleConfiguration`, `Registry`, `Memory`
   - Private fields: `_styleConfig`, `_registry`, `_memory`, `_policyProvider`, `_candidateSource`, `_physicalityFilter`, `_settings`
4. Add XML comment to class stating: "Data source for drum generation. Does NOT generate PartTracks directly. Use GrooveBasedDrumGenerator pipeline."
5. Verify code compiles after removal (ignore test failures)

**Files Modified**:
- `Music/Generator/Agents/Drums/DrummerAgent.cs`

**Estimated Effort**: 30 minutes

---

## RF-2: Create GrooveBasedDrumGenerator Pipeline

**Goal**: Create pipeline orchestrator that uses IGroovePolicyProvider + IGrooveCandidateSource properly.

**Acceptance Criteria**:

### 2.1 Create Pipeline Class

1. Create `Music/Generator/Agents/Drums/GrooveBasedDrumGenerator.cs`
2. Class is `public sealed`
3. Constructor signature:
   ```csharp
   public GrooveBasedDrumGenerator(
       IGroovePolicyProvider policyProvider,
       IGrooveCandidateSource candidateSource,
       GrooveBasedDrumGeneratorSettings? settings = null)
   ```
4. Validate parameters: throw `ArgumentNullException` if policy or candidate source is null
5. Store in private readonly fields

### 2.2 Define Settings Record

1. Create `GrooveBasedDrumGeneratorSettings` record with:
   - `bool EnableDiagnostics` (default: false)
   - `IReadOnlyList<string>? ActiveRoles` (default: ["Kick", "Snare", "ClosedHat"])
   - `int DefaultVelocity` (default: 100)
   - Static `Default` property
2. Place in same file as generator class

### 2.3 Implement Generate Method Signature

1. Public method: `public PartTrack Generate(SongContext songContext)`
2. Validate songContext (null check, required tracks)
3. Extract required data from songContext:
   - `BarTrack barTrack`
   - `SectionTrack sectionTrack`
   - `SegmentGrooveProfile[] segmentProfiles`
   - `GroovePresetDefinition groovePresetDefinition`
   - `int totalBars`
4. Call private helper methods (to be implemented in 2.4-2.8)
5. Return `PartTrack` with sorted events

### 2.4 Implement Anchor Extraction

1. Private method: `ExtractAnchorOnsets(GroovePresetDefinition, int totalBars, BarTrack) → List<GrooveOnset>`
2. For each bar 1..totalBars:
   - Get active groove preset for that bar
   - Get anchor layer
   - Extract kick onsets → create GrooveOnset with role "Kick"
   - Extract snare onsets → create GrooveOnset with role "Snare"
   - Extract hat onsets → create GrooveOnset with role "ClosedHat"
   - Set velocity to DefaultVelocity
   - Set IsMustHit = true on all anchors
3. Return combined list sorted by (bar, beat)

### 2.5 Implement Operator Selection Pipeline

1. Private method: `GenerateOperatorOnsets(GrooveBarContext[], List<GrooveOnset> anchors, BarTrack) → List<GrooveOnset>`
2. Create empty result list
3. For each bar context:
   - Create GrooveBarContext from BarContext
   - For each active role (from settings):
     - **Call policy provider**: `var policy = _policyProvider.GetPolicy(grooveBarContext, role)`
     - **Calculate target count** from policy density: `int targetCount = CalculateTargetCount(policy, role)`
     - **Get candidate groups**: `var groups = _candidateSource.GetCandidateGroups(grooveBarContext, role)`
     - **Filter anchors for this bar+role**: get existing anchors to avoid conflicts
     - **SELECT using GrooveSelectionEngine**: 
       ```csharp
       var selected = GrooveSelectionEngine.SelectUntilTargetReached(
           grooveBarContext, role, groups, targetCount, 
           anchorSubset, diagnostics: null);
       ```
     - **Convert selected candidates** to GrooveOnset with velocity from hint or default
     - Add to result list
4. Return combined list

### 2.6 Implement Target Count Calculation

1. Private method: `CalculateTargetCount(GroovePolicyDecision?, string role) → int`
2. If policy is null or no density override: return 0 (anchor-only)
3. Get density01 from policy override
4. Base count on beats per bar (assume 4/4 for MVP): `baseCount = 4`
5. Scale by density: `targetCount = (int)(baseCount * density01 * 2.0)` (allows up to 8 hits in 4/4)
6. Clamp to [0, maxEventsOverride from policy if present, else 16]
7. Return target count

### 2.7 Implement Onset Combination

1. Private method: `CombineOnsets(List<GrooveOnset> anchors, List<GrooveOnset> operators) → List<GrooveOnset>`
2. Create dictionary keyed by (bar, beat, role)
3. Add all anchors first (they win conflicts)
4. Add operators, skipping if position already occupied
5. Return combined list sorted by absolute tick position

### 2.8 Implement MIDI Conversion

1. Private method: `ConvertToMidiEvents(List<GrooveOnset> onsets, BarTrack, int drumProgramNumber) → PartTrack`
2. For each onset:
   - Get absolute tick from BarTrack.ToTick(bar, beat)
   - Map role to MIDI note number (Kick→36, Snare→38, ClosedHat→42, etc.)
   - Create PartTrackEvent with note, velocity, duration (default 120 ticks)
   - Apply timing offset from onset if present
3. Sort events by AbsoluteTimeTicks (CRITICAL - MIDI export validation)
4. Create PartTrack with sorted events
5. Set MidiProgramNumber to drumProgramNumber
6. Return PartTrack

### 2.9 Implement Helpers

1. Private method: `GetDrumProgramNumber(SongContext) → int`
   - Look up "DrumKit" voice from VoiceSet
   - Return MIDI program number or 255 default
2. Private static method: `CreateGrooveBarContext(BarContext) → GrooveBarContext`
   - Map BarContext fields to GrooveBarContext constructor
3. Private static method: `MapRoleToMidiNote(string role) → int`
   - Switch on role: Kick→36, Snare→38, ClosedHat→42, OpenHat→46, Crash→49, Ride→51
   - Default to 38 (snare) for unknown

**Files Created**:
- `Music/Generator/Agents/Drums/GrooveBasedDrumGenerator.cs`

**Estimated Effort**: 2 hours

---

## RF-3: Update Generator.cs Integration

**Goal**: Update top-level Generator to use new pipeline with DrummerAgent as data source.

**Acceptance Criteria**:

1. Update `Generate(SongContext, DrummerAgent?)` overload:
   - Change signature to: `Generate(SongContext songContext, StyleConfiguration? drummerStyle = null)`
   - If `drummerStyle` is provided:
     - Create `DrummerAgent agent = new(drummerStyle)`
     - Create `GrooveBasedDrumGenerator generator = new(agent, agent)`
     - Call `generator.Generate(songContext)`
     - Return PartTrack
   - If `drummerStyle` is null: use existing fallback to DrumTrackGenerator
2. Keep backward-compatible `Generate(SongContext)` overload:
   - Calls `Generate(songContext, drummerStyle: null)` (fallback behavior)
3. Update XML comments to reflect new architecture
4. Verify code compiles

**Files Modified**:
- `Music/Generator/Core/Generator.cs`

**Estimated Effort**: 30 minutes

---

## RF-4: Update DrumTrackGenerator Integration

**Goal**: Update DrumTrackGenerator to use new pipeline instead of creating its own DrummerAgent.

**Acceptance Criteria**:

1. Update `Generate(SongContext)` method:
   - Create DrummerAgent: `var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock)`
   - Create pipeline: `var generator = new GrooveBasedDrumGenerator(agent, agent)`
   - Call: `return generator.Generate(songContext)`
   - Remove old try/catch with internal Generate call
   - Keep GenerateLegacyAnchorBased as private fallback (for now)
2. Update `Generate(BarTrack, SectionTrack, ...)` overload:
   - Build SongContext from parameters
   - Call `Generate(songContext)`
3. Keep `GenerateLegacyAnchorBasedInternal` unchanged (fallback path)
4. Update XML comments
5. Verify code compiles

**Files Modified**:
- `Music/Generator/Drums/DrumTrackGenerator.cs`

**Estimated Effort**: 30 minutes

---

## RF-5: Fix Unit Tests for DrummerAgent

**Goal**: Update tests to reflect DrummerAgent as data source only (no Generate method).

**Acceptance Criteria**:

### 5.1 Remove Generate Tests

1. In `DrummerAgentTests.cs`, remove all tests that call `Generate()`:
   - `Generate_ValidSongContext_ReturnsPartTrack` (if exists)
   - `Generate_NullSongContext_Throws` (if exists)
   - `Generate_DifferentSeeds_ProducesDifferentOutput` (if exists)
   - Any other tests calling `agent.Generate()`
2. Remove helper methods only used by Generate tests

### 5.2 Add/Update Interface Tests

1. Test: `GetPolicy_DelegatesToPolicyProvider_Correctly`
   - Create agent
   - Create bar context
   - Call GetPolicy for different roles
   - Verify non-null results
   - Verify different contexts produce different policies
2. Test: `GetCandidateGroups_DelegatesToCandidateSource_Correctly`
   - Create agent
   - Create bar context
   - Call GetCandidateGroups for different roles
   - Verify groups returned
   - Verify candidates have valid properties
3. Test: `GetPolicy_UsesSharedMemory_AcrossCalls`
   - Create agent
   - Make multiple GetPolicy calls for same bar/role
   - Verify memory state is consistent
4. Test: `GetCandidateGroups_RespectsPhysicality_WhenConfigured`
   - Create agent with physicality rules
   - Get candidates that would violate rules
   - Verify filtered correctly

### 5.3 Update Construction Tests

1. Keep existing: `Constructor_WithValidStyleConfig_Succeeds`
2. Keep existing: `Constructor_NullStyleConfig_Throws`
3. Keep existing: `Constructor_InitializesRegistry`
4. Keep existing: `Constructor_InitializesMemory`

**Files Modified**:
- `Music.Tests/Generator/Agents/Drums/DrummerAgentTests.cs`

**Estimated Effort**: 1 hour

---

## RF-6: Add Selection Logic Verification Tests

**Goal**: Add new tests that verify GrooveBasedDrumGenerator uses GrooveSelectionEngine correctly.

**Acceptance Criteria**:

### 6.1 Create Test File

1. Create `Music.Tests/Generator/Agents/Drums/GrooveBasedDrumGeneratorTests.cs`
2. Add xUnit imports
3. Add `[Collection("RngDependentTests")]` attribute
4. Add constructor that calls `Rng.Initialize(42)`

### 6.2 Basic Generation Tests

1. Test: `Generate_ValidSongContext_ReturnsPartTrack`
   - Create minimal SongContext (4 bars, single section, PopRock style)
   - Create DrummerAgent
   - Create GrooveBasedDrumGenerator
   - Call Generate
   - Verify PartTrack is not null
   - Verify events are sorted by AbsoluteTimeTicks
   - Verify events have valid MIDI notes
2. Test: `Generate_NullSongContext_Throws`
   - Create generator
   - Call Generate with null
   - Verify ArgumentNullException
3. Test: `Generate_EmptySongContext_ReturnsEmptyTrack`
   - Create SongContext with 0 bars
   - Generate
   - Verify empty or anchor-only track

### 6.3 Selection Logic Tests

1. Test: `Generate_RespectsDensityTarget_FromPolicy`
   - Create test policy provider that returns high density (0.9)
   - Create test candidate source with many candidates
   - Create generator with test providers
   - Generate 4-bar track
   - Count events per bar per role
   - Verify count is close to (beatsPerBar * density * 2) ± 2
2. Test: `Generate_RespectsZeroDensity_ProducesAnchorOnly`
   - Create policy provider returning density 0.0
   - Generate track
   - Verify only anchor positions have events
3. Test: `Generate_RespectsOperatorCaps_NeverExceeds`
   - Create policy with MaxEventsPerBarOverride = 8
   - Create candidate source with 50 candidates
   - Generate track
   - For each bar: verify total events ≤ 8
4. Test: `Generate_WeightedSelection_FavorsHigherScores`
   - Create candidate source with scored candidates (0.9, 0.5, 0.1)
   - Generate 100 bars with seed variation
   - Count selection frequency for each score band
   - Verify high-scored candidates selected more often (chi-squared test or simple ratio)

### 6.4 Determinism Tests

1. Test: `Generate_SameSeed_IdenticalOutput`
   - Rng.Initialize(123)
   - Generate track A
   - Rng.Initialize(123)
   - Generate track B
   - Verify events match exactly (count, positions, velocities)
2. Test: `Generate_DifferentSeeds_DifferentOutput`
   - Rng.Initialize(123)
   - Generate track A
   - Rng.Initialize(456)
   - Generate track B
   - Verify at least some events differ

### 6.5 Anchor Integration Tests

1. Test: `Generate_CombinesAnchorsAndOperators_NoConflicts`
   - Generate track with anchors + operators
   - Verify anchor positions have events (not overwritten)
   - Verify operator events exist in non-anchor positions
2. Test: `Generate_AnchorsMustHit_NeverRemoved`
   - Generate track
   - Verify all anchor beats (1, 3 for kick; 2, 4 for snare in 4/4) have events

**Files Created**:
- `Music.Tests/Generator/Agents/Drums/GrooveBasedDrumGeneratorTests.cs`

**Estimated Effort**: 2 hours

---

## RF-7: Update Integration and Golden Tests

**Goal**: Fix integration tests and regenerate golden snapshots with correct selection logic.

**Acceptance Criteria**:

### 7.1 Update Generator Integration Tests

1. In `Music.Tests/Generator/Core/GeneratorTests.cs` (if exists):
   - Update any tests that use `Generate(songContext, drummerAgent)`
   - Change to `Generate(songContext, StyleConfigurationLibrary.PopRock)`
   - Verify tests pass
2. In `Music.Tests/Generator/Drums/DrumTrackGeneratorTests.cs` (if exists):
   - Update any integration tests
   - Verify Generate produces valid output
   - Verify determinism

### 7.2 Regenerate Golden Snapshots

1. In `DrummerGoldenTests.cs` (if exists from Story 10.8.3):
   - Locate golden test: `PopRock_Standard_MatchesSnapshot`
   - Run test and capture NEW output
   - Compare with old snapshot:
     - Verify NEW output has FEWER events (density enforcement working)
     - Verify NEW output has more musical variation
     - Verify determinism: same seed → same output on re-run
   - Update snapshot file with NEW output
   - Add comment explaining why snapshot changed (selection logic fix)
2. If no golden tests exist yet:
   - Skip this section
   - Note in test file: "Golden tests pending Story 10.8.3"

### 7.3 Document Differences

1. Create `Music/AI/Plans/RefactoringResults_RF7.md`:
   - Section: "Output Differences"
   - Compare old (all candidates) vs new (selected candidates)
   - Show example bar: before (20 events) → after (8 events)
   - List verified improvements:
     - Density targets enforced ✓
     - Weighted selection working ✓
     - Caps respected ✓
     - Determinism preserved ✓
   - Note any unexpected changes

**Files Modified**:
- `Music.Tests/Generator/Core/GeneratorTests.cs` (if exists)
- `Music.Tests/Generator/Drums/DrumTrackGeneratorTests.cs` (if exists)
- `Music.Tests/Generator/Agents/Drums/DrummerGoldenTests.cs` (if exists)
- `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json` (if exists)

**Files Created**:
- `Music/AI/Plans/RefactoringResults_RF7.md`

**Estimated Effort**: 1 hour

---

## Summary

| Story | Focus | Effort | Critical Path |
|-------|-------|--------|---------------|
| RF-1 | Remove Generate from DrummerAgent | 30 min | ✓ |
| RF-2 | Create GrooveBasedDrumGenerator | 2 hr | ✓ |
| RF-3 | Update Generator.cs | 30 min | ✓ |
| RF-4 | Update DrumTrackGenerator | 30 min | ✓ |
| RF-5 | Fix DrummerAgent tests | 1 hr | ✓ |
| RF-6 | Add selection tests | 2 hr | ✓ |
| RF-7 | Update golden tests | 1 hr | — |

**Total Estimated Effort**: 7.5 hours (1 day with testing)

**Critical Path**: RF-1 → RF-2 → RF-3 → RF-4 → RF-5 → RF-6

RF-7 can be done in parallel once RF-6 passes.

---

## Success Criteria

✅ DrummerAgent has NO Generate method  
✅ GrooveBasedDrumGenerator uses GrooveSelectionEngine  
✅ All tests pass  
✅ Density targets are enforced  
✅ Weighted selection works  
✅ Caps are respected  
✅ Determinism is preserved  
✅ Output is less dense than before  

---

## Notes for Implementation

- Each story can be completed in one Claude Sonnet 4.5 session
- Stories RF-1 through RF-4 must be done in order
- RF-5 and RF-6 can be done in parallel after RF-4
- RF-7 should be done last to verify everything works
- Run `dotnet build` after each story to catch issues early
- Run full test suite after RF-6 before considering refactoring complete

---

**Created**: 2025-01-27  
**Status**: Ready for implementation  
**Next Step**: Implement RF-1
