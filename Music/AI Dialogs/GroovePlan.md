# Groove-Driven Drum Generator Plan

## 1. Design Overview

### 1.1 Architecture

The drum generator will follow a **pipeline architecture** with distinct phases:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DRUM GENERATION PIPELINE                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Phase 1: INITIALIZATION                                                    │
│  ├── Load GroovePresetDefinition + SegmentGrooveProfiles                   │
│  ├── Resolve meter/timing from SongContext                                  │
│  └── Build per-bar context (section, phrase position, segment profile)     │
│                                                                             │
│  Phase 2: ANCHOR GENERATION (per bar)                                       │
│  ├── Copy base onsets from AnchorLayer (Kick, Snare, Hat)                  │
│  ├── Apply protection rules (MustHitOnsets, NeverRemoveOnsets)             │
│  └── Output: base onset list per role                                       │
│                                                                             │
│  Phase 3: VARIATION SELECTION (per bar)                                     │
│  ├── Filter candidates by EnabledVariationTags                             │
│  ├── Apply density targets to select/prune candidates                       │
│  ├── Respect MaxAddsPerBar caps at group and candidate level               │
│  ├── Use ProbabilityBias for deterministic weighted selection              │
│  └── Output: additional onsets to add                                       │
│                                                                             │
│  Phase 4: CONSTRAINT ENFORCEMENT (per bar)                                  │
│  ├── Enforce MaxHitsPerBar / MaxHitsPerBeat                                │
│  ├── Filter by AllowedSubdivisions grid                                     │
│  ├── Apply syncopation/anticipation rules                                   │
│  ├── Protect phrase-end anchors if configured                              │
│  └── Output: validated onset list                                           │
│                                                                             │
│  Phase 5: VELOCITY SHAPING (per onset)                                      │
│  ├── Classify onset strength (Downbeat, Backbeat, Offbeat, Ghost, etc.)    │
│  ├── Look up VelocityRule for role + strength                              │
│  ├── Apply AccentBias adjustments                                           │
│  └── Output: velocity per onset                                             │
│                                                                             │
│  Phase 6: TIMING ADJUSTMENT (per onset)                                     │
│  ├── Apply Feel (Swing/Shuffle offset for offbeats)                        │
│  ├── Apply RoleTimingBiasTicks per role                                     │
│  ├── Clamp by MaxAbsTimingBiasTicks                                         │
│  └── Output: final tick position per onset                                  │
│                                                                             │
│  Phase 7: MIDI EVENT EMISSION                                               │
│  ├── Convert role + onset to MIDI note number                              │
│  ├── Create PartTrackEvent for each note                                    │
│  └── Output: PartTrack with all drum events                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Key Design Decisions

1. **Deterministic generation**: Use seed-based selection for probability; same inputs = same output
2. **Per-bar processing**: Each bar processed independently with its segment context
3. **Role-based handling**: Kick, Snare, Hat processed separately then merged
4. **Layered protection**: Protection hierarchy applied additively (base → refine → specific)

---

## 2. Settings Inventory

### 2.1 Enums (Multiple Choice)

| Enum | Values | Handler Required |
|------|--------|-----------------|
| `GrooveFeel` | Straight, Swing, Shuffle, TripletFeel | 4 timing behaviors |
| `AllowedSubdivision` | Quarter, Eighth, Sixteenth, EighthTriplet, SixteenthTriplet | Grid filter (flags) |
| `OnsetStrength` | Downbeat, Backbeat, Strong, Offbeat, Pickup, Ghost | Velocity lookup key |
| `TimingFeel` | Ahead, OnTop, Behind, LaidBack | Micro-timing direction |

### 2.2 Boolean Settings

| Class | Property | True Behavior | False Behavior |
|-------|----------|---------------|----------------|
| `RoleRhythmVocabulary` | AllowSyncopation | Offbeat onsets permitted | Offbeat onsets filtered |
| `RoleRhythmVocabulary` | AllowAnticipation | Pickup onsets permitted | Pickup onsets filtered |
| `RoleRhythmVocabulary` | SnapStrongBeatsToChordTones | (pitched roles only) | N/A for drums |
| `GroovePhraseHookPolicy` | AllowFillsAtPhraseEnd | Fill candidates active in window | Fills disabled |
| `GroovePhraseHookPolicy` | AllowFillsAtSectionEnd | Section-end fills active | Section fills disabled |
| `GroovePhraseHookPolicy` | ProtectDownbeatOnPhraseEnd | Beat 1 never removed | Beat 1 can be removed |
| `GroovePhraseHookPolicy` | ProtectBackbeatOnPhraseEnd | Backbeats never removed | Backbeats can be removed |
| `GrooveProtectionLayer` | IsAdditiveOnly | Layer only adds protections | Layer can override |
| `GrooveVariationLayer` | IsAdditiveOnly | Layer only adds candidates | Layer can replace |
| `GrooveOverrideMergePolicy` | OverrideReplacesLists | Segment replaces base lists | Union/append |
| `GrooveOverrideMergePolicy` | OverrideCanRemoveProtectedOnsets | Segment can remove protected | Protected items safe |
| `GrooveOverrideMergePolicy` | OverrideCanRelaxConstraints | Segment can raise caps | Caps enforced strictly |
| `GrooveOverrideMergePolicy` | OverrideCanChangeFeel | Segment can change swing | Feel locked to base |
| `SectionRolePresenceDefaults` | RolePresent[role] | Role generates notes | Role silent |

### 2.3 Numeric Settings

| Class | Property | Type | Usage |
|-------|----------|------|-------|
| `GroovePresetIdentity` | BeatsPerBar | int | Meter numerator |
| `GrooveSubdivisionPolicy` | SwingAmount01 | double | Swing intensity 0-1 |
| `VelocityRule` | Min, Max, Typical | int | Velocity bounds |
| `VelocityRule` | AccentBias | int | Additive velocity adjustment |
| `GrooveTimingPolicy` | MaxAbsTimingBiasTicks | int | Timing clamp |
| `GrooveTimingPolicy` | RoleTimingBiasTicks[role] | int | Per-role tick offset |
| `RoleRhythmVocabulary` | MaxHitsPerBar | int | Hard cap per bar |
| `RoleRhythmVocabulary` | MaxHitsPerBeat | int | Density cap per beat |
| `GrooveRoleConstraintPolicy` | RoleMaxDensityPerBar[role] | int | Global density cap |
| `GroovePhraseHookPolicy` | PhraseEndBarsWindow | int | Fill window size |
| `GroovePhraseHookPolicy` | SectionEndBarsWindow | int | Section fill window |
| `SectionRolePresenceDefaults` | RoleDensityMultiplier[role] | double | Section density scaling |
| `GrooveOnsetCandidate` | OnsetBeat | decimal | Beat position |
| `GrooveOnsetCandidate` | MaxAddsPerBar | int | Candidate cap |
| `GrooveCandidateGroup` | MaxAddsPerBar | int | Group cap |
| `RoleDensityTarget` | Density01 | double | Target density |
| `RoleDensityTarget` | MaxEventsPerBar | int | Segment cap |
| `SegmentGrooveProfile` | StartBar, EndBar | int? | Bar range |
| `SegmentGrooveProfile` | OverrideSwingAmount01 | double? | Segment swing override |

### 2.4 Probability Settings

| Class | Property | Type | Effect |
|-------|----------|------|--------|
| `GrooveOnsetCandidate` | ProbabilityBias | double | Selection weight |
| `GrooveCandidateGroup` | BaseProbabilityBias | double | Group weight multiplier |

### 2.5 List/Collection Settings

| Class | Property | Type | Usage |
|-------|----------|------|-------|
| `GrooveInstanceLayer` | KickOnsets, SnareOnsets, HatOnsets | List<decimal> | Base pattern |
| `RoleProtectionSet` | MustHitOnsets | List<decimal> | Required onsets |
| `RoleProtectionSet` | ProtectedOnsets | List<decimal> | Discouraged removal |
| `RoleProtectionSet` | NeverRemoveOnsets | List<decimal> | Hard protection |
| `RoleProtectionSet` | NeverAddOnsets | List<decimal> | Forbidden additions |
| `GroovePhraseHookPolicy` | EnabledFillTags | List<string> | Active fill tag filter |
| `SegmentGrooveProfile` | EnabledVariationTags | List<string> | Active variation tags |
| `SegmentGrooveProfile` | EnabledProtectionTags | List<string> | Active protection tags |
| `GrooveProtectionPolicy` | HierarchyLayers | List<GrooveProtectionLayer> | Layered protections |
| `GrooveVariationCatalog` | HierarchyLayers | List<GrooveVariationLayer> | Layered candidates |

---

## 3. Setting Precedence (Ordered)

Settings are applied in this order. Earlier settings establish constraints; later settings operate within those constraints.

### Tier 1: Identity & Meter (Immutable Context)
1. `GroovePresetIdentity.BeatsPerBar` - establishes grid
2. `SectionTrack` / `BarTrack` - bar boundaries

### Tier 2: Role Presence (On/Off Gate)
3. `GrooveOrchestrationPolicy.DefaultsBySectionType[section].RolePresent[role]` - skip if false
4. `SegmentGrooveProfile.SectionIndex` - map bar to segment

### Tier 3: Protection Rules (Hard Constraints)
5. `GrooveProtectionPolicy.HierarchyLayers` - merge layered protections
6. `RoleProtectionSet.MustHitOnsets` - always include these
7. `RoleProtectionSet.NeverRemoveOnsets` - cannot be removed by variation
8. `RoleProtectionSet.NeverAddOnsets` - cannot be added by variation

### Tier 4: Subdivision Grid (Filter)
9. `GrooveSubdivisionPolicy.AllowedSubdivisions` - filter invalid beat positions
10. `RoleRhythmVocabulary.AllowSyncopation` - filter offbeat candidates
11. `RoleRhythmVocabulary.AllowAnticipation` - filter pickup candidates

### Tier 5: Anchor Layer (Base Pattern)
12. `GrooveInstanceLayer.KickOnsets/SnareOnsets/HatOnsets` - base onsets

### Tier 6: Phrase/Section Context
13. `GroovePhraseHookPolicy.AllowFillsAtPhraseEnd` + window - enable fills
14. `GroovePhraseHookPolicy.AllowFillsAtSectionEnd` + window - enable section fills
15. `GroovePhraseHookPolicy.ProtectDownbeatOnPhraseEnd` - protect beat 1
16. `GroovePhraseHookPolicy.ProtectBackbeatOnPhraseEnd` - protect 2/4

### Tier 7: Variation Selection
17. `SegmentGrooveProfile.EnabledVariationTags` - filter active groups
18. `GrooveVariationCatalog.HierarchyLayers` - merge candidate pools
19. `GrooveCandidateGroup.GroupTags` - match against enabled tags
20. `GrooveOnsetCandidate.Tags` - additional tag filter
21. `GrooveOnsetCandidate.ProbabilityBias` × `GrooveCandidateGroup.BaseProbabilityBias` - selection weight

### Tier 8: Density Enforcement
22. `RoleDensityTarget.Density01` - target fill level
23. `RoleDensityTarget.MaxEventsPerBar` - segment cap
24. `GrooveRoleConstraintPolicy.RoleMaxDensityPerBar[role]` - global cap
25. `RoleRhythmVocabulary.MaxHitsPerBar` - vocabulary cap
26. `RoleRhythmVocabulary.MaxHitsPerBeat` - per-beat cap
27. `GrooveOnsetCandidate.MaxAddsPerBar` - per-candidate cap
28. `GrooveCandidateGroup.MaxAddsPerBar` - per-group cap
29. `SectionRolePresenceDefaults.RoleDensityMultiplier[role]` - scale target

### Tier 9: Velocity Shaping
30. `GrooveAccentPolicy.RoleStrengthVelocity[role][strength]` - lookup rule
31. `VelocityRule.Typical` - base velocity
32. `VelocityRule.Min/Max` - bounds
33. `VelocityRule.AccentBias` - adjustment
34. `GrooveAccentPolicy.RoleGhostVelocity[role]` - ghost note velocity

### Tier 10: Timing/Feel Adjustment
35. `SegmentGrooveProfile.OverrideFeel` ?? `GrooveSubdivisionPolicy.Feel` - swing mode
36. `SegmentGrooveProfile.OverrideSwingAmount01` ?? `GrooveSubdivisionPolicy.SwingAmount01` - swing amount
37. `GrooveTimingPolicy.RoleTimingFeel[role]` - micro-timing direction
38. `GrooveTimingPolicy.RoleTimingBiasTicks[role]` - tick offset
39. `GrooveTimingPolicy.MaxAbsTimingBiasTicks` - clamp

### Tier 11: Merge Policy (Override Rules)
40. `GrooveOverrideMergePolicy.*` - governs how segment overrides apply

---

## 4. User Stories

### Epic: Groove-Driven Drum Generation

> **PRIORITY STRATEGY**: Stories are organized into phases. **Phase 1 (MVP)** delivers audible 
> drum output using anchor patterns from `CreateTestGrooveD1` as early as possible. 
> Subsequent phases add refinement features. All original settings coverage is preserved.

---

## Phase 1: MVP - Audible Anchor Output (Stories 1-4)

*Goal: Generate audible drum notes from anchor patterns. After Phase 1, you can hear drums.*

---

### Story 1: Scaffold DrumGeneratorNew with Minimal Types (COMPLETED)
**As a** developer  
**I want** a minimal generator class that can emit drum notes  
**So that** I have audible feedback immediately

**Acceptance Criteria:**
- [ ] Create `DrumTrackGeneratorNew` static class in `Music.Generator` namespace (update existing stub)
- [ ] Create `DrumOnset` record (role, beat, velocity, tickPosition) - minimal fields for MVP
- [ ] Create `DrumRole` enum (Kick, Snare, Hat) for role identification
- [ ] Create the rest of the roles and definitions for entire midi drum kit
- [ ] Implement main `Generate` method signature matching existing pattern
- [ ] Method returns `PartTrack` with at least placeholder structure

**Tasks:**
1. Update DrumTrackGeneratorNew.cs with minimal implementation
2. Define DrumOnset and DrumRole types
3. Implement Generate method that returns empty PartTrack initially

---

### Story 2: Implement Anchor Pattern Extraction (COMPLETED)
**As a** generator  
**I want** to extract onset positions from a GroovePresetDefinition's AnchorLayer  
**So that** I have the base groove pattern to emit

**Acceptance Criteria:**
- [x] Resolve `GroovePresetDefinition` from `GrooveTrack.GetActiveGroovePreset(bar)`
- [x] Read `GrooveInstanceLayer.KickOnsets` for Kick role
- [x] Read `GrooveInstanceLayer.SnareOnsets` for Snare role
- [x] Read `GrooveInstanceLayer.HatOnsets` for Hat role
- [x] Return `List<DrumOnset>` with beat positions and default velocity (100)
- [x] Handle preset lookup using `SourcePresetName` from `CreateTestGrooveD1` ("PopRockBasic")

**Settings Handled:**
- `GroovePresetDefinition.AnchorLayer`
- `GrooveInstanceLayer.KickOnsets/SnareOnsets/HatOnsets`

---

### Story 3: Implement MIDI Event Emission (MVP) (COMPLETED)
**As a** generator  
**I want** DrumOnsets converted to PartTrackEvents  
**So that** the output produces audible drums

**Acceptance Criteria:**
- [x] Map DrumRole → MIDI note number (Kick=36, Snare=38, Hat=42)
- [x] Convert beat position to absolute tick using `BarTrack.ToTick` (bar+beat)
- [x] Create `PartTrackEvent` NoteOn for each onset with velocity
- [x] Create corresponding NoteOff events (short duration represented via `NoteDurationTicks`, e.g., 60 ticks)
- [x] Sort events by absolute tick
- [x] Return complete `PartTrack` with `MidiProgramNumber` from parameter

**Settings Handled:**
- Role-to-MIDI mapping (hardcoded for MVP)
- Beat-to-tick conversion via BarTrack

**Implementation notes:**
- `Music/Generator/Drums/DrumTrackGeneratorNew.cs` implements anchor extraction, `ExtractAnchorOnsets`, and `ConvertOnsetsToMidiEvents`.
- `DrumOnset` now includes `BarNumber`; `ConvertOnsetsToMidiEvents` uses `BarTrack.ToTick` and `PartTrackEvent` constructor with `NoteDurationTicks`.

---

### Story 4: Integration and End-to-End Verification (COMPLETED)
**As a** developer  
**I want** GeneratorNew to use DrumTrackGeneratorNew  
**So that** I can hear drums when running the application

**Acceptance Criteria:**
- [x] Wire `DrumTrackGeneratorNew.Generate` into `GeneratorNew.Generate`
- [x] Pass required parameters: harmonyTrack, grooveTrack, barTrack, sectionTrack, totalBars
- [x] Verify using `CreateTestGrooveD1` which sets `SourcePresetName = "PopRockBasic"`  (user manual test with existing UI)
- [x] Confirm audible drum output (kick on 1/3, snare on 2/4, hats on eighths)   (user manual test with existing UI)
- [x] Validate PartTrack contains expected note count  (user manual test with existing UI)

**Implementation notes:**
- `Music/Generator/Core/GeneratorNew.cs` lines 38-44 call `DrumTrackGeneratorNew.Generate` with all required parameters.
- Drum program number resolved via `GetProgramNumberForRole(songContext.Voices, "DrumKit", defaultProgram: 255)`.
- Returns drum PartTrack to caller (`HandleCommandWriteTestSongNew.cs` adds it to song and grid).

**Manual testing required:** Run the application, click "Write Test Song" button, and verify:
1. Drums are audible in the generated MIDI
2. Kick pattern appears on beats 1 and 3 (per PopRockBasic preset)
3. Snare pattern appears on beats 2 and 4 (per PopRockBasic preset)
4. Hi-hat pattern appears on eighth notes (per PopRockBasic preset)

**MVP Complete**: After Story 4, drums are audible using anchor patterns.

---

## Phase 2: Core Context & Structure (Stories 5-7)

*Goal: Add per-bar context, section awareness, and role presence checks.*

---

### Story 5: Implement Per-Bar Context Building (COMPLETED)
**As a** generator  
**I want** to build context for each bar  
**So that** subsequent phases know section and phrase position

**Acceptance Criteria:**
- [x] Create `DrumBarContext` record (bar number, section, segment profile, phrase position)
- [x] Read `SectionTrack.Sections` to map bar → section
- [x] Calculate phrase position (bar within section, bars until section end)
- [x] Handle `GroovePresetIdentity.BeatsPerBar` for grid calculation
- [x] Return `List<DrumBarContext>` for all bars

**Settings Handled:**
- `GroovePresetIdentity.BeatsPerBar`
- `SectionTrack.Sections`
- `SegmentGrooveProfile.StartBar/EndBar/SectionIndex`

**Implementation notes:**
- `DrumBarContext` record added with BarNumber, Section, SegmentProfile, BarWithinSection, BarsUntilSectionEnd fields.
- `BuildBarContexts` method encapsulates context building logic per copilot-instructions.md guidance.
- Maps bars to sections using `SectionTrack.GetActiveSection`.
- Resolves segment profiles by matching bar against StartBar/EndBar ranges.
- Calculates phrase position: `BarWithinSection` (0-based) and `BarsUntilSectionEnd`.
- Generator signature updated to accept `IReadOnlyList<SegmentGrooveProfile>` from SongContext.
- Framework only—no audible change without Stories 6-7 applying this context.

---

### Story 6: Implement Role Presence Check (COMPLETED)
**As a** generator  
**I want** to skip roles that are disabled for a section type  
**So that** orchestration policy controls which instruments play

**Acceptance Criteria:**
- [x] Look up `GrooveOrchestrationPolicy.DefaultsBySectionType` by section name
- [x] Check `RolePresent[role]` for Kick, Snare, Hat, DrumKit
- [x] If role not present, emit no notes for that role in that bar
- [x] Handle missing section type gracefully (default to present)

**Settings Handled:**
- `GrooveOrchestrationPolicy.DefaultsBySectionType`
- `SectionRolePresenceDefaults.RolePresent`

**Implementation notes:**
- `ApplyRolePresenceFilter` method added to encapsulate filtering logic.
- Looks up section type from `DrumBarContext.Section.SectionType`.
- Finds matching `SectionRolePresenceDefaults` by case-insensitive section type match.
- Checks `RolePresent` dictionary for individual role (Kick, Snare, Hat, etc.).
- Falls back to `DrumKit` master switch if individual role not specified.
- Defaults to present (true) if no orchestration policy or section defaults found.
- Generator signature updated to accept `GroovePresetDefinition` for access to `ProtectionPolicy.OrchestrationPolicy`.
- **Audible difference**: If orchestration policy disables a role for a section (e.g., Hat in verse), that role will be silent.

---

### Story 7: Add Segment Groove Profile Support (COMPLETED)
**As a** generator  
**I want** to resolve segment profiles for bars  
**So that** segment-specific overrides can be applied later

**Acceptance Criteria:**
- [x] Read `SegmentGrooveProfiles` from song context
- [x] Map bar → segment profile by `StartBar`/`EndBar` range
- [x] Store segment profile in `DrumBarContext`
- [x] Handle bars with no explicit segment (use defaults)

**Settings Handled:**
- `SegmentGrooveProfile.StartBar/EndBar`
- `SegmentGrooveProfile.SectionIndex`

**Implementation notes:**
- **Implemented as part of Story 5** - `BuildBarContexts` already resolves segment profiles.
- Matches bars against `StartBar`/`EndBar` ranges and stores result in `DrumBarContext.SegmentProfile`.
- Returns null for bars without explicit segment (callers handle defaults).
- Framework only—segment data is captured but not yet applied to generation.
- **Future stories will consume this data**: Story 20 (Feel/Swing overrides), Story 22 (Merge policy), variations/density (when implemented).
- **Manual testing**: Segment profiles are built by `GrooveSetupFactory.BuildSegmentProfilesForTestSong` and passed through `SongContext.SegmentGrooveProfiles`.

---

## Phase 3: Protection System (Stories 8-9)

*Goal: Implement protection rules that constrain what can be modified.*

---

### Story 8: Implement Protection Hierarchy Merger (COMPLETED)
**As a** generator  
**I want** to merge protection layers respecting IsAdditiveOnly  
**So that** refined protections layer on base protections

**Acceptance Criteria:**
- [x] Iterate `GrooveProtectionPolicy.HierarchyLayers` in order
- [x] For each layer, check `AppliesWhenTagsAll` against enabled tags
- [x] If `IsAdditiveOnly=true`, union onsets; else replace
- [x] Merge `MustHitOnsets`, `ProtectedOnsets`, `NeverRemoveOnsets`, `NeverAddOnsets`
- [x] Return merged `RoleProtectionSet` per role

**Settings Handled:**
- `GrooveProtectionPolicy.HierarchyLayers`
- `GrooveProtectionLayer.LayerId/AppliesWhenTagsAll/IsAdditiveOnly`
- `RoleProtectionSet.*`

**Implementation notes:**
- `Music/Generator/Drums/ProtectionPolicyMerger.cs` implements hierarchical layer merging.
- `MergeProtectionLayers` method processes layers [0..n] in order (base → refined).
- `LayerApplies` checks if all `AppliesWhenTagsAll` tags are present in `enabledTags`.
- `MergeAdditive` unions onset lists when `IsAdditiveOnly=true`.
- `MergeReplace` replaces entire protection set when `IsAdditiveOnly=false`.
- `UnionOnsets` prevents duplicate onsets in merged lists.
- `CloneProtectionSet` creates deep copies to avoid shared references.
- Integrated into `DrumTrackGeneratorNew.Generate` at line 82 via `MergeProtectionLayersPerBar`.
- Per-bar merging uses `EnabledProtectionTags` from `SegmentGrooveProfile`.

---

### Story 9: Apply Must-Hit and Protection Rules (COMPLETED)
**As a** generator  
**I want** MustHitOnsets always included and NeverRemove protected  
**So that** essential groove anchors are preserved

**Acceptance Criteria:**
- [x] Ensure all `MustHitOnsets` are in the onset list
- [x] Mark `NeverRemoveOnsets` as protected (cannot be pruned)
- [x] Filter variation candidates against `NeverAddOnsets`
- [x] `ProtectedOnsets` are discouraged but not forbidden to remove

**Settings Handled:**
- `RoleProtectionSet.MustHitOnsets`
- `RoleProtectionSet.NeverRemoveOnsets`
- `RoleProtectionSet.NeverAddOnsets`
- `RoleProtectionSet.ProtectedOnsets`

**Implementation notes:**
- `Music/Generator/Drums/DrumTrackGeneratorNew.cs` lines 222-304 implement `EnforceProtections` method.
- `DrumOnset` record extended with `IsMustHit`, `IsNeverRemove`, `IsProtected` flags (lines 33-35).
- Protection enforcement pipeline:
  1. Groups onsets by bar for efficient processing.
  2. Iterates each bar's merged protections by role.
  3. **NeverAddOnsets**: Removes matching onsets from pool (line 249).
  4. **NeverRemoveOnsets**: Sets `IsNeverRemove=true` flag on matching onsets (line 256).
  5. **ProtectedOnsets**: Sets `IsProtected=true` flag on matching onsets (line 259).
  6. **MustHitOnsets**: Adds missing onsets with default velocity=100; sets all three flags appropriately (lines 263-279).
- Deduplication by bar+role+beat ensures no duplicate onsets (lines 294-301).
- Integrated into `DrumTrackGeneratorNew.Generate` at line 91 after role presence filtering.
- **Manual testing**: Verify protection flags affect future variation/pruning logic (Stories 10-17).

---

## Phase 4: Subdivision & Rhythm Vocabulary (Stories 10-12)

*Goal: Filter onsets by grid and rhythm rules.*

---

### Story 10: Implement Subdivision Grid Filter (COMPLETED)
**As a** generator  
**I want** to filter onsets by allowed subdivision grid  
**So that** only valid beat positions are used

**Acceptance Criteria:**
- [x] Parse `AllowedSubdivision` flags to determine valid beat fractions
- [x] Quarter: 1, 2, 3, 4
- [x] Eighth: 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5
- [x] Sixteenth: 1, 1.25, 1.5, 1.75, 2, 2.25, ...
- [x] EighthTriplet: 1, 1.33, 1.67, 2, 2.33, ...
- [x] SixteenthTriplet: finer grid
- [x] Filter candidate onsets to valid positions only

**Settings Handled:**
- `GrooveSubdivisionPolicy.AllowedSubdivisions` (all 5 flag values)

**Implementation notes:**
- `Music/Generator/Drums/DrumTrackGeneratorNew.cs` implements `ApplySubdivisionFilter` method.
- Filter is applied after `ExtractAnchorOnsets` in the `Generate` pipeline (before role presence and protection enforcement).
- Builds valid beat positions using divisionsPerBeat: Quarter=1, Eighth=2, Sixteenth=4, EighthTriplet=3, SixteenthTriplet=6.
- Uses epsilon comparison (0.002 beats) to handle recurring fractions (1/3, 1/6).
- Returns empty list if `AllowedSubdivision.None` to enforce strict policy intent.
- Null subdivision policy bypasses filter (returns original onsets).
- **Audible difference**: If subdivision policy restricts grid (e.g., Quarter only), offbeat onsets will be filtered out.

**Test coverage:**
- `Music.Tests/Generator/Drums/SubdivisionGridFilterTests.cs` - 15 tests covering:
  - Null/empty input handling
  - Each individual subdivision flag (Quarter, Eighth, Sixteenth, EighthTriplet, SixteenthTriplet)
  - Combined flags (Quarter + Eighth, all flags)
  - None flag returns empty
  - Null policy bypasses filter
  - Multiple bars filtering
  - Different time signatures (3/4)
  - Epsilon comparison for triplet recurring fractions
  - Preservation of onset properties (velocity, tick position, protection flags)

---

### Story 11: Implement Syncopation and Anticipation Filter
**As a** generator  
**I want** to filter candidates by syncopation/anticipation rules  
**So that** rhythm vocabulary is respected

**Acceptance Criteria:**
- [ ] If `AllowSyncopation=false`, filter onsets with `Strength=Offbeat`
- [ ] If `AllowAnticipation=false`, filter onsets with `Strength=Pickup`
- [ ] Apply per-role from `RoleRhythmVocabulary`
- [ ] Handle both true/false cases correctly

**Settings Handled:**
- `RoleRhythmVocabulary.AllowSyncopation` (true/false)
- `RoleRhythmVocabulary.AllowAnticipation` (true/false)

---

### Story 12: Implement Phrase Hook Policy
**As a** generator  
**I want** fills enabled only in designated windows  
**So that** phrase structure is respected

**Acceptance Criteria:**
- [ ] Calculate if bar is within `PhraseEndBarsWindow` of section end
- [ ] If `AllowFillsAtPhraseEnd=false`, disable fill tags in that window
- [ ] If `AllowFillsAtSectionEnd=false`, disable at section boundary
- [ ] If `ProtectDownbeatOnPhraseEnd=true`, add beat 1 to NeverRemove
- [ ] If `ProtectBackbeatOnPhraseEnd=true`, add beats 2,4 to NeverRemove
- [ ] Filter candidates by `EnabledFillTags`

**Settings Handled:**
- `GroovePhraseHookPolicy.AllowFillsAtPhraseEnd` (true/false)
- `GroovePhraseHookPolicy.PhraseEndBarsWindow`
- `GroovePhraseHookPolicy.AllowFillsAtSectionEnd` (true/false)
- `GroovePhraseHookPolicy.SectionEndBarsWindow`
- `GroovePhraseHookPolicy.ProtectDownbeatOnPhraseEnd` (true/false)
- `GroovePhraseHookPolicy.ProtectBackbeatOnPhraseEnd` (true/false)
- `GroovePhraseHookPolicy.EnabledFillTags`

---

## Phase 5: Variation System (Stories 13-17)

*Goal: Add variation candidates, filtering, and selection.*

---

### Story 13: Implement Variation Catalog Merger
**As a** generator  
**I want** to merge variation layers respecting IsAdditiveOnly  
**So that** refined variations layer on base candidates

**Acceptance Criteria:**
- [ ] Iterate `GrooveVariationCatalog.HierarchyLayers` in order
- [ ] For each layer, check `AppliesWhenTagsAll` against enabled tags
- [ ] If `IsAdditiveOnly=true`, union groups; else replace
- [ ] Return merged list of `GrooveCandidateGroup`

**Settings Handled:**
- `GrooveVariationCatalog.HierarchyLayers`
- `GrooveVariationLayer.LayerId/AppliesWhenTagsAll/IsAdditiveOnly`
- `GrooveVariationLayer.CandidateGroups`

---

### Story 14: Implement Variation Tag Filter
**As a** generator  
**I want** candidates filtered by segment's enabled tags  
**So that** only appropriate variations are considered

**Acceptance Criteria:**
- [ ] Read `SegmentGrooveProfile.EnabledVariationTags`
- [ ] Filter `GrooveCandidateGroup` where any `GroupTags` match enabled
- [ ] Filter `GrooveOnsetCandidate` where any `Tags` match (if specified)
- [ ] Handle empty tag lists as "match all"

**Settings Handled:**
- `SegmentGrooveProfile.EnabledVariationTags`
- `GrooveCandidateGroup.GroupTags`
- `GrooveOnsetCandidate.Tags`

---

### Story 15: Implement Probability-Weighted Selection
**As a** generator  
**I want** candidates selected by weighted probability  
**So that** variation is deterministic but configurable

**Acceptance Criteria:**
- [ ] Calculate weight = `GrooveOnsetCandidate.ProbabilityBias` × `GrooveCandidateGroup.BaseProbabilityBias`
- [ ] Use seeded RNG for deterministic selection
- [ ] Select candidates proportional to weight
- [ ] Order by weight for tie-breaking

**Settings Handled:**
- `GrooveOnsetCandidate.ProbabilityBias` (probability)
- `GrooveCandidateGroup.BaseProbabilityBias` (probability)

---

### Story 16: Implement Density Target Selection
**As a** generator  
**I want** variation count driven by density targets  
**So that** sections have appropriate activity levels

**Acceptance Criteria:**
- [ ] Read `RoleDensityTarget.Density01` for role
- [ ] Calculate target onset count = Density01 × MaxEventsPerBar
- [ ] Apply `SectionRolePresenceDefaults.RoleDensityMultiplier` scaling
- [ ] Select candidates until target reached or pool exhausted
- [ ] Never exceed `MaxEventsPerBar`

**Settings Handled:**
- `RoleDensityTarget.Density01`
- `RoleDensityTarget.MaxEventsPerBar`
- `SectionRolePresenceDefaults.RoleDensityMultiplier`

---

### Story 17: Implement MaxHits Caps Enforcement
**As a** generator  
**I want** density caps strictly enforced  
**So that** bars don't become overcrowded

**Acceptance Criteria:**
- [ ] Enforce `RoleRhythmVocabulary.MaxHitsPerBar` - prune excess
- [ ] Enforce `RoleRhythmVocabulary.MaxHitsPerBeat` - no beat overcrowding
- [ ] Enforce `GrooveRoleConstraintPolicy.RoleMaxDensityPerBar[role]`
- [ ] Enforce `GrooveOnsetCandidate.MaxAddsPerBar` per candidate
- [ ] Enforce `GrooveCandidateGroup.MaxAddsPerBar` per group
- [ ] Prefer protected onsets when pruning

**Settings Handled:**
- `RoleRhythmVocabulary.MaxHitsPerBar`
- `RoleRhythmVocabulary.MaxHitsPerBeat`
- `GrooveRoleConstraintPolicy.RoleMaxDensityPerBar`
- `GrooveOnsetCandidate.MaxAddsPerBar`
- `GrooveCandidateGroup.MaxAddsPerBar`

---

## Phase 6: Velocity Shaping (Stories 18-19)

*Goal: Shape dynamics based on onset strength and role.*

---

### Story 18: Implement Onset Strength Classification
**As a** generator  
**I want** each onset classified by strength  
**So that** velocity shaping is applied correctly

**Acceptance Criteria:**
- [ ] Classify beat 1 as `Downbeat`
- [ ] Classify beats 2, 4 as `Backbeat` (4/4)
- [ ] Classify beat 3 as `Strong`
- [ ] Classify .5 positions as `Offbeat`
- [ ] Classify .75 positions as `Pickup` if before strong beat
- [ ] Read `GrooveOnsetCandidate.Strength` for variation candidates
- [ ] Handle `Ghost` strength explicitly

**Settings Handled:**
- `OnsetStrength` enum (all 6 values)
- `GrooveOnsetCandidate.Strength`

---

### Story 19: Implement Velocity Shaping
**As a** generator  
**I want** velocity determined by role and strength  
**So that** dynamics reflect the groove feel

**Acceptance Criteria:**
- [ ] Look up `GrooveAccentPolicy.RoleStrengthVelocity[role][strength]`
- [ ] Use `VelocityRule.Typical` as base
- [ ] Add `VelocityRule.AccentBias`
- [ ] Clamp to `VelocityRule.Min/Max`
- [ ] For Ghost, use `RoleGhostVelocity[role]` if defined
- [ ] Handle missing lookups with sensible defaults

**Settings Handled:**
- `GrooveAccentPolicy.RoleStrengthVelocity`
- `VelocityRule.Min/Max/Typical/AccentBias`
- `GrooveAccentPolicy.RoleGhostVelocity`

---

## Phase 7: Timing & Feel (Stories 20-21)

*Goal: Apply swing, shuffle, and micro-timing adjustments.*

---

### Story 20: Implement Feel/Swing Timing
**As a** generator  
**I want** swing/shuffle feel applied to timing  
**So that** the groove has the right pocket

**Acceptance Criteria:**
- [ ] Read `SegmentGrooveProfile.OverrideFeel` ?? `GrooveSubdivisionPolicy.Feel`
- [ ] If `Straight`: no timing adjustment
- [ ] If `Swing`: shift offbeats later by SwingAmount01 factor
- [ ] If `Shuffle`: triplet-feel offbeat timing
- [ ] If `TripletFeel`: all subdivisions on triplet grid
- [ ] Apply `OverrideSwingAmount01` if specified

**Settings Handled:**
- `GrooveFeel` enum (all 4 values)
- `GrooveSubdivisionPolicy.Feel`
- `GrooveSubdivisionPolicy.SwingAmount01`
- `SegmentGrooveProfile.OverrideFeel`
- `SegmentGrooveProfile.OverrideSwingAmount01`

---

### Story 21: Implement Role Timing Bias
**As a** generator  
**I want** per-role micro-timing applied  
**So that** instruments have distinct pocket feel

**Acceptance Criteria:**
- [ ] Read `GrooveTimingPolicy.RoleTimingFeel[role]`
- [ ] If `Ahead`: negative tick offset
- [ ] If `OnTop`: zero offset
- [ ] If `Behind`: positive tick offset
- [ ] If `LaidBack`: larger positive offset
- [ ] Add `RoleTimingBiasTicks[role]` 
- [ ] Clamp by `MaxAbsTimingBiasTicks`

**Settings Handled:**
- `TimingFeel` enum (all 4 values)
- `GrooveTimingPolicy.RoleTimingFeel`
- `GrooveTimingPolicy.RoleTimingBiasTicks`
- `GrooveTimingPolicy.MaxAbsTimingBiasTicks`

---

## Phase 8: Override & Merge Policy (Story 22)

*Goal: Implement segment override merge rules.*

---

### Story 22: Implement Merge Policy Enforcement
**As a** generator  
**I want** segment overrides governed by merge policy  
**So that** override behavior is predictable

**Acceptance Criteria:**
- [ ] If `OverrideReplacesLists=true`, segment lists replace base
- [ ] If `OverrideCanRemoveProtectedOnsets=true`, allow removal
- [ ] If `OverrideCanRelaxConstraints=true`, use segment caps
- [ ] If `OverrideCanChangeFeel=true`, use segment feel
- [ ] Handle all 4 booleans correctly for both values

**Settings Handled:**
- `GrooveOverrideMergePolicy.OverrideReplacesLists` (true/false)
- `GrooveOverrideMergePolicy.OverrideCanRemoveProtectedOnsets` (true/false)
- `GrooveOverrideMergePolicy.OverrideCanRelaxConstraints` (true/false)
- `GrooveOverrideMergePolicy.OverrideCanChangeFeel` (true/false)

---

## Phase 9: Testing & Validation (Story 23)

*Goal: Comprehensive test coverage for all phases.*

---

### Story 23: Unit Tests for Core Phases
**As a** developer  
**I want** unit tests for each phase  
**So that** behavior is verified and regressions caught

**Acceptance Criteria:**
- [ ] Test anchor generation copies onsets correctly
- [ ] Test protection merger respects IsAdditiveOnly
- [ ] Test subdivision filter for each flag combination
- [ ] Test syncopation/anticipation filters (true/false)
- [ ] Test density selection respects caps
- [ ] Test velocity shaping lookups
- [ ] Test timing feel adjustments (all 4 enum values)
- [ ] Test swing amounts at 0, 0.5, 1.0

---

## 5. Story Dependencies

```
PHASE 1: MVP - AUDIBLE OUTPUT (Stories 1-4)
────────────────────────────────────────────
Story 1 (Scaffold & Types)
    └── Story 2 (Anchor Extraction)
        └── Story 3 (MIDI Emission MVP)
            └── Story 4 (Integration & Verify)
                │
                ▼ AUDIBLE DRUMS WORKING

PHASE 2: CORE CONTEXT (Stories 5-7)
────────────────────────────────────────────
Story 5 (Per-Bar Context)
    ├── Story 6 (Role Presence)
    └── Story 7 (Segment Profile Support)

PHASE 3: PROTECTION (Stories 8-9)
────────────────────────────────────────────
Story 8 (Protection Merge)
    └── Story 9 (Apply Protection Rules)

PHASE 4: SUBDIVISION & RHYTHM (Stories 10-12)
────────────────────────────────────────────
Story 10 (Subdivision Grid Filter)
    └── Story 11 (Syncopation/Anticipation Filter)
        └── Story 12 (Phrase Hook Policy)

PHASE 5: VARIATION (Stories 13-17)
────────────────────────────────────────────
Story 13 (Variation Catalog Merge)
    └── Story 14 (Tag Filter)
        └── Story 15 (Probability Selection)
            └── Story 16 (Density Target)
                └── Story 17 (MaxHits Caps)

PHASE 6: VELOCITY (Stories 18-19)
────────────────────────────────────────────
Story 18 (Strength Classification)
    └── Story 19 (Velocity Shaping)

PHASE 7: TIMING (Stories 20-21)
────────────────────────────────────────────
Story 20 (Feel/Swing)
    └── Story 21 (Role Timing Bias)

PHASE 8: MERGE POLICY (Story 22)
────────────────────────────────────────────
Story 22 (Merge Policy Enforcement)

PHASE 9: TESTING (Story 23)
────────────────────────────────────────────
Story 23 (Unit Tests)
```

**Key Insight**: Phase 1 completes at Story 4 with audible drums. All subsequent phases 
add refinement features incrementally. Each phase after 1 can be tested against audible output.

---

## 6. Estimated Effort

### Phase 1: MVP - Audible Output
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 1 | Scaffold & Types | Small | 2 |
| 2 | Anchor Extraction | Small | 2 |
| 3 | MIDI Emission MVP | Medium | 3 |
| 4 | Integration & Verify | Small | 2 |
| **Phase 1 Total** | | | **9** |

*After Phase 1: Drums are audible using anchor patterns*

### Phase 2: Core Context
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 5 | Per-Bar Context | Medium | 3 |
| 6 | Role Presence | Small | 1 |
| 7 | Segment Profile Support | Small | 2 |
| **Phase 2 Total** | | | **6** |

### Phase 3: Protection
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 8 | Protection Merge | Medium | 3 |
| 9 | Apply Protection Rules | Medium | 3 |
| **Phase 3 Total** | | | **6** |

### Phase 4: Subdivision & Rhythm
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 10 | Subdivision Grid Filter | Medium | 3 |
| 11 | Syncopation/Anticipation Filter | Small | 2 |
| 12 | Phrase Hook Policy | Medium | 5 |
| **Phase 4 Total** | | | **10** |

### Phase 5: Variation
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 13 | Variation Catalog Merge | Medium | 3 |
| 14 | Tag Filter | Small | 2 |
| 15 | Probability Selection | Medium | 3 |
| 16 | Density Target | Medium | 3 |
| 17 | MaxHits Caps | Medium | 3 |
| **Phase 5 Total** | | | **14** |

### Phase 6: Velocity
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 18 | Strength Classification | Small | 2 |
| 19 | Velocity Shaping | Medium | 3 |
| **Phase 6 Total** | | | **5** |

### Phase 7: Timing
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 20 | Feel/Swing | Medium | 5 |
| 21 | Role Timing Bias | Small | 2 |
| **Phase 7 Total** | | | **7** |

### Phase 8: Merge Policy
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 22 | Merge Policy Enforcement | Medium | 3 |
| **Phase 8 Total** | | | **3** |

### Phase 9: Testing
| Story | Description | Complexity | Points |
|-------|-------------|------------|--------|
| 23 | Unit Tests | Large | 8 |
| **Phase 9 Total** | | | **8** |

| **Grand Total** | | | **68** |

---

## 7. Definition of Done

- [ ] All settings from Groove.cs are implemented
- [ ] Each enum value has distinct behavior
- [ ] Each boolean has true/false handling
- [ ] Probability settings affect selection weights only
- [ ] Numeric settings constrain or configure behavior
- [ ] Build passes with no errors
- [ ] Unit tests pass
- [ ] End-to-end test generates valid MIDI
- [ ] Changing any setting produces different output (except probabilities which affect likelihood)
