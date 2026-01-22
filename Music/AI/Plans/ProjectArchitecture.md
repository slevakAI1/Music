# Music Generator Project Architecture Reference

**Purpose:** AI code generation context for the Music project. Describes the current codebase structure, data models, generation pipelines, and key conventions.

**Last Updated:** Companion document to active development.

---

## 1) Solution Overview

| Property | Value |
|----------|-------|
| Project | `Music.csproj` |
| Test Project | `Music.Tests.csproj` |
| Framework | `net9.0-windows` |
| App Type | WinForms (`<UseWindowsForms>true</UseWindowsForms>`) |
| Nullable | Enabled (`<Nullable>enable</Nullable>`) |

### NuGet Dependencies

| Package | Purpose |
|---------|---------|
| `Melanchall.DryWetMidi` | MIDI read/write/export and playback |
| `MeltySynth` | Software synthesizer (MIDI playback) |
| `MusicTheory` | Theory utilities (pitch/chord logic) |
| `DiffPlex` | Diffing for diagnostics/compare workflows |

### Excluded Folders (not compiled)

- `Archives/`, `Future Ideas/`, `Files/`
- `Generator/Core/RandomizationOld/`, `Generator/DrumsOld/`, `Generator/Energy/`, `Generator/Guitar/`
- `Song/GrooveOld/`, `Tests/`, `Patches/`

---

## 2) Entry Points

### Program.cs

```
GlobalExceptionHandler.Configure()  → UI exception handling
WordParser.EnsureLoaded()          → Pre-load lyric parser singleton
Rng.Initialize()                    → Seed per-purpose RNG instances
Application.Run(new MainForm())     → Start MDI container
```

### MainForm.cs

- MDI container hosting `WriterForm` on startup
- Creates `MidiIoService` and `MidiPlaybackService`
- Menu actions for MIDI import/playback

### WriterForm.cs

- Primary interactive design surface
- Hosts song grid (`dgSong`) with fixed rows for Voice, Section, Lyrics, Harmony, Groove, TimeSignature, Tempo
- Double-click opens editor dialogs (e.g., `SectionEditorForm`, `HarmonyEditorForm`, `GrooveEditorForm`)
- Triggers generation via `Generator.Generate(songContext)`

---

## 3) Core Data Model

### Timing Constants

```csharp
// Music/Core/MusicConstants.cs
public const short TicksPerQuarterNote = 480;  // Canonical tick resolution
```

**All tick calculations MUST use this constant.**

### SongContext (Central DTO)

Location: `Song/SongContext.cs`

```csharp
public sealed class SongContext
{
    public int CurrentBar { get; set; } = 1;           // 1-based
    public BarTrack BarTrack { get; set; }             // Timing ruler (derived from TimingTrack)
    
    // Groove System (NEW)
    public GroovePresetDefinition GroovePresetDefinition { get; set; }
    public IReadOnlyList<SegmentGrooveProfile> SegmentGrooveProfiles { get; set; }
    
    // Design Tracks
    public HarmonyTrack HarmonyTrack { get; set; }     // Chord progression
    public SectionTrack SectionTrack { get; set; }     // Song structure
    public LyricTrack LyricTrack { get; set; }         // Lyrics with phonetics
    
    // Runtime Output
    public Song Song { get; set; }                     // Tempo + TimeSignature + rendered PartTracks
    
    // Instrument Mapping
    public VoiceSet Voices { get; set; }               // Role → MIDI program
    
    // Material System (Stage 8+9)
    public MaterialBank MaterialBank { get; set; }     // Reusable motifs/fragments
}
```

### Song (Runtime Output)

Location: `Song/Song.cs`

```csharp
public sealed class Song
{
    public TempoTrack TempoTrack { get; set; }
    public Timingtrack TimeSignatureTrack { get; set; }
    public List<PartTrack> PartTracks { get; set; }
    public int TotalBars { get; set; }
}
```

### Section Model

Location: `Song/Section/`

```csharp
// Section.cs
public class Section
{
    public int SectionId { get; set; }
    public MusicConstants.eSectionType SectionType { get; set; }  // Intro, Verse, Chorus, Solo, Bridge, Outro, Custom
    public string? Name { get; set; }
    public int StartBar { get; set; }      // 1-based
    public int BarCount { get; set; } = 4;
}

// SectionTrack.cs - ordered, contiguous sections
public class SectionTrack
{
    public List<Section> Sections { get; set; }
    public int TotalBars => _nextBar - 1;   // Last bar covered
    
    public void Add(MusicConstants.eSectionType sectionType, int barCount, string? name = null);
    public bool GetActiveSection(int bar, out Section? section);  // Find section containing bar
}
```

### PartTrack (Instrument Events)

Location: `Song/PartTrack/`

```csharp
// PartTrack.cs
public sealed class PartTrack
{
    public record struct PartTrackId(string Value);  // GUID-based stable identity
    
    public PartTrackMeta Meta { get; set; }          // Identity/domain/kind metadata
    public string MidiProgramName { get; set; }
    public int MidiProgramNumber { get; set; }       // 0-255
    public List<PartTrackEvent> PartTrackNoteEvents { get; set; }  // Ordered by AbsoluteTimeTicks
}

// PartTrackEvent.cs
public class PartTrackEvent
{
    public long AbsoluteTimeTicks { get; init; }     // Canonical timing (absolute from song start)
    public PartTrackEventType Type { get; init; }
    public int NoteNumber { get; set; }
    public int NoteDurationTicks { get; set; }
    public int NoteOnVelocity { get; set; } = 100;
}
```

#### PartTrackBarCoverageAnalyzer (Story SC1)

Location: `Generator/Groove/PartTrackBarCoverageAnalyzer.cs`

Utility for analyzing which bars have content vs are empty (supports incremental/non-contiguous fill workflows):

```csharp
public enum BarFillState
{
    Empty,       // No events intersect this bar
    HasContent,  // At least one event intersects this bar
    Locked       // Optional: user-set "do not modify" flag (future)
}

public static class PartTrackBarCoverageAnalyzer
{
    // Returns Dictionary<int, BarFillState> mapping bar numbers to fill state
    public static IReadOnlyDictionary<int, BarFillState> Analyze(
        PartTrack partTrack,
        BarTrack barTrack,
        int totalBars);
}
```

**Story SC1:** Pure, side-effect-free utility. Bar intersection rule: note belongs to bar if its `[startTick, endTick)` overlaps the bar's `[StartTick, EndTick)`.

### BarTrack (Timing Ruler)

Location: `Song/Bar/BarTrack.cs`

- Derived from `Timingtrack` via `RebuildFromTimingTrack(timingTrack, totalBars)`
- **Read-only to generator** — editors rebuild, generator consumes
- Provides `TryGetBar(barNumber)`, `ToTick(bar, beat)`, `IsBeatInBar(bar, beat)`

### HarmonyTrack

Location: `Song/Harmony/HarmonyTrack.cs`

```csharp
public class HarmonyTrack
{
    public List<HarmonyEvent> Events { get; set; }  // Sorted by StartBar, StartBeat
    public HarmonyEvent? GetActiveHarmonyEvent(int bar, decimal beat);
}

public sealed class HarmonyEvent
{
    public int StartBar { get; init; }      // 1-based
    public int StartBeat { get; init; } = 1;
    public string Key { get; init; }        // e.g., "C major"
    public int Degree { get; init; }        // 1..7
    public string Quality { get; init; }    // "maj", "min7", "dom7"
    public string Bass { get; init; }       // "root", "3rd", "5th", "7th"
}
```

---

## 4) Generator Pipeline

### Current State

**Only DrumTrackGenerator is active.** 

Location: `Generator/Core/Generator.cs`

```csharp
public static PartTrack Generate(SongContext songContext)
{
    // Validates: SectionTrack, TimeSignatureTrack, GroovePresetDefinition
    // Returns: Single drum PartTrack
    
    return DrumTrackGenerator.Generate(
        barTrack, sectionTrack, segmentProfiles, groovePresetDefinition, totalBars, drumProgramNumber);
}
```

### DrumTrackGenerator Pipeline

Location: `Generator/Drums/DrumTrackGenerator.cs`

Pipeline steps (in order):

1. **Build bar contexts** — `BarContextBuilder.Build(sectionTrack, segmentProfiles, totalBars)`
2. **Merge protection hierarchy** — `ProtectionPerBarBuilder.Build(barContexts, protectionPolicy)`
3. **Augment phrase hooks** — `PhraseHookProtectionAugmenter.Augment(...)`
4. **Extract anchor onsets** — From `GroovePresetDefinition.AnchorLayer` (kick/snare/hat)
5. **Filter by subdivision** — `OnsetGridBuilder.Build()` + `grid.IsAllowed(beat)`
6. **Filter by syncopation/anticipation** — `RhythmVocabularyFilter.Filter(...)`
7. **Filter by role presence** — `RolePresenceGate.IsRolePresent(...)`
8. **Apply protections** — `ProtectionApplier.Apply(...)` (must-hit, never-add, never-remove)
9. **Apply feel timing** — `FeelTimingEngine.ApplyFeelTiming(...)` (global feel shifts eligible eighth offbeats)
10. **Apply role micro-timing** — `RoleTimingEngine.ApplyRoleTiming(...)` (per-role feel+bias added on top of E1; final clamp applied)
11. **Convert to MIDI events** — Create `PartTrackEvent` from `GrooveOnset`

---

## 5) Groove System (NEW Architecture)

The groove system uses a declarative preset-based model with hierarchical policies.

### Key Types

Location: `Generator/Groove/Groove.cs`

#### GroovePresetDefinition (Container)

```csharp
public sealed class GroovePresetDefinition
{
    public GroovePresetIdentity Identity { get; set; }       // Name, BeatsPerBar, StyleFamily, Tags
    public GrooveInstanceLayer AnchorLayer { get; set; }     // Base pattern (kick/snare/hat/bass/comp/pads onsets)
    public GrooveProtectionPolicy ProtectionPolicy { get; set; }  // All policies bundled
    public GrooveVariationCatalog VariationCatalog { get; set; }  // Optional candidates for variation
}
```

#### GrooveProtectionPolicy (Bundled Policies)

```csharp
public sealed class GrooveProtectionPolicy
{
    public GroovePresetIdentity Identity { get; set; }
    public GrooveSubdivisionPolicy SubdivisionPolicy { get; set; }     // Grid + feel
    public GrooveRoleConstraintPolicy RoleConstraintPolicy { get; set; } // Vocab constraints
    public GroovePhraseHookPolicy PhraseHookPolicy { get; set; }       // Fill windows/cadence
    public GrooveTimingPolicy TimingPolicy { get; set; }               // Micro-timing (per-role feel & bias)
    public GrooveAccentPolicy AccentPolicy { get; set; }               // Velocity shaping
    public GrooveOrchestrationPolicy OrchestrationPolicy { get; set; } // Role presence defaults
    public List<GrooveProtectionLayer> HierarchyLayers { get; set; }   // Layered protections
}
```

Note: The `GrooveTimingPolicy` is consumed by the Role Timing Engine (Story E2). Key behavior:
- `TimingFeel` maps to fixed base tick offsets: Ahead=-10, OnTop=0, Behind=+10, LaidBack=+20.
- `RoleTimingBiasTicks[role]` is added to the base feel offset per role.
- `GroovePolicyDecision` may provide field-level overrides per bar/role: `RoleTimingFeelOverride` and `RoleTimingBiasTicksOverride`.
- Combined offset is added to any existing E1 feel timing and the final result is clamped to `[-MaxAbsTimingBiasTicks..+MaxAbsTimingBiasTicks]`.

#### SegmentGrooveProfile (Per-Section Overrides)

```csharp
public sealed class SegmentGrooveProfile
{
    public string SegmentId { get; set; }
    public int? SectionIndex { get; set; }
    public int? StartBar { get; set; }
    public int? EndBar { get; set; }
    public string? GroovePresetName { get; set; }           // For mid-song preset switching
    public List<string> EnabledVariationTags { get; set; }
    public List<RoleDensityTarget> DensityTargets { get; set; }
    public GrooveFeel? OverrideFeel { get; set; }
    public double? OverrideSwingAmount01 { get; set; }
}
```

#### OnsetStrength Classification

```csharp
public enum OnsetStrength
{
    Downbeat,   // Beat 1
    Backbeat,   // Beats 2/4 in 4/4
    Strong,     // Other strong beats
    Offbeat,    // Eighth offbeats, syncopations
    Pickup,     // Anticipations
    Ghost       // Low intensity decorative
}
```

#### GrooveOnset (Output Type)

Location: `Generator/Groove/GrooveOnset.cs`

```csharp
public sealed record GrooveOnset
{
    public required string Role { get; init; }        // "Kick", "Snare", "Bass", etc.
    public required int BarNumber { get; init; }      // 1-based
    public required decimal Beat { get; init; }       // 1-based, can be fractional (e.g., 1.5)
    public OnsetStrength? Strength { get; init; }
    public int? Velocity { get; init; }               // 1-127
    public int? TimingOffsetTicks { get; init; }
    public GrooveOnsetProvenance? Provenance { get; init; }
    public bool IsMustHit { get; init; }
    public bool IsNeverRemove { get; init; }
    public bool IsProtected { get; init; }
}
```

#### GrooveBarPlan (Per-Bar Plan)

Location: `Generator/Groove/GrooveBarPlan.cs`

```csharp
public sealed record GrooveBarPlan
{
    public required IReadOnlyList<GrooveOnset> BaseOnsets { get; init; }              // Anchors
    public required IReadOnlyList<GrooveOnset> SelectedVariationOnsets { get; init; } // Selected variations
    public required IReadOnlyList<GrooveOnset> FinalOnsets { get; init; }             // After constraints
    public GrooveBarDiagnostics? Diagnostics { get; init; }                           // Story G1: Structured diagnostics
    public required int BarNumber { get; init; }
}
```

**Story G1 Update:** `Diagnostics` changed from `string?` to `GrooveBarDiagnostics?` for structured decision tracing.

#### GrooveBarDiagnostics (Decision Trace - Story G1)

Location: `Generator/Groove/GrooveBarDiagnostics.cs`

Structured diagnostics for opt-in decision tracing. When disabled, remains null (zero-cost).

```csharp
public sealed record GrooveBarDiagnostics
{
    public required int BarNumber { get; init; }
    public required string Role { get; init; }
    public required IReadOnlyList<string> EnabledTags { get; init; }
    public required int CandidateGroupCount { get; init; }
    public required int TotalCandidateCount { get; init; }
    public required IReadOnlyList<FilterDecision> FiltersApplied { get; init; }
    public required DensityTargetDiagnostics DensityTarget { get; init; }
    public required IReadOnlyList<SelectionDecision> SelectedCandidates { get; init; }
    public required IReadOnlyList<PruneDecision> PruneEvents { get; init; }
    public required OnsetListSummary FinalOnsetSummary { get; init; }
}

// Supporting records
public sealed record FilterDecision(string CandidateId, string Reason);
public sealed record SelectionDecision(string CandidateId, double Weight, string RngStreamUsed);
public sealed record PruneDecision(string OnsetId, string Reason, bool WasProtected);
public sealed record DensityTargetDiagnostics { /* density computation inputs */ };
public sealed record OnsetListSummary { /* onset counts at pipeline stages */ };
```

**Story G1:** Diagnostics collection is opt-in via `GrooveDiagnosticsCollector` helper class. Records:
- Enabled tags after phrase/segment/policy resolution
- Candidate pool statistics
- Filter decisions with reasons
- Selection decisions with weights and RNG streams
- Prune events with protection status
- Final onset counts

#### GrooveOverrideMergePolicy (Story F1)

Location: `Generator/Groove/Groove.cs`

Controls how segment overrides interact with base policies:

```csharp
public sealed class GrooveOverrideMergePolicy
{
    public bool OverrideReplacesLists { get; set; }           // Replace vs union/append for lists
    public bool OverrideCanRemoveProtectedOnsets { get; set; } // Allow removing protected items
    public bool OverrideCanRelaxConstraints { get; set; }     // Allow increasing caps
    public bool OverrideCanChangeFeel { get; set; }           // Allow changing swing/feel
}
```

**Story F1:** Enforced by `OverrideMergePolicyEnforcer.CanRemoveOnset()` to ensure predictable override behavior.

### Groove Infrastructure Files

| File | Purpose |
|------|---------|
| `OnsetGrid.cs` / `OnsetGridBuilder.cs` | Valid beat positions from subdivision policy |
| `OnsetStrengthClassifier.cs` | Classify onset strength |
| `VelocityShaper.cs` | Compute velocity per onset (role x strength) |
| `FeelTimingEngine.cs` | Apply feel timing (Straight/Swing/Shuffle/Triplet) to eligible eighth offbeats |
| `RoleTimingEngine.cs` | Apply per-role micro-timing (TimingFeel -> ticks + RoleTimingBiasTicks) and clamp to `MaxAbsTimingBiasTicks` |
| `ProtectionPerBarBuilder.cs` | Merge protection layers per bar |
| `ProtectionApplier.cs` | Apply must-hit/never-add/never-remove rules |
| `RolePresenceGate.cs` | Check if role is present in section |
| `RhythmVocabularyFilter.cs` | Filter by syncopation/anticipation rules |
| `PhraseHookProtectionAugmenter.cs` | Protect anchors near phrase/section ends |
| `BarContext.cs` / `BarContextBuilder.cs` | Per-bar context (section, phrase position) |
| `GrooveBarContext.cs` | Type alias for BarContext in groove system |
| `GrooveBarPlan.cs` | Per-bar plan (base onsets, variation, final, diagnostics) |
| `GrooveBarDiagnostics.cs` | Structured diagnostics records (Story G1) |
| `GrooveDiagnosticsCollector.cs` | Helper for collecting diagnostic data during pipeline (Story G1) |
| `GrooveOnsetProvenance.cs` | Provenance record for `GrooveOnset` (Story G2) |
| `GrooveOnsetFactory.cs` | Helper to construct `GrooveOnset` with provenance (Story G2) |
| `GrooveSelectionEngine.cs` | Select candidates until target reached; supports diagnostics collection |
| `GrooveCapsEnforcer.cs` | Enforce hard caps with diagnostics; respects merge policy (Stories C3, F1, G1) |
| `OverrideMergePolicyEnforcer.cs` | Enforce override merge policy rules (Story F1) |
| `PartTrackBarCoverageAnalyzer.cs` | Analyze per-bar fill state (Story SC1) |
| `GrooveTestSetup.cs` | Build PopRockBasic preset for testing |

---

## 6) Material System (Stage 8+9)

Reusable musical fragments (motifs, riffs, hooks) with placement and rendering.

### Core Types

Location: `Song/Material/`

#### PartTrackMeta (Track Metadata)

```csharp
public sealed record PartTrackMeta
{
    public PartTrack.PartTrackId TrackId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string IntendedRole { get; init; }           // "Lead", "Bass", "Guitar", "Keys"
    public PartTrackDomain Domain { get; init; }        // SongAbsolute or MaterialLocal
    public PartTrackKind Kind { get; init; }            // RoleTrack, MaterialFragment, MaterialVariant
    public MaterialKind MaterialKind { get; init; }     // Riff, Hook, MelodyPhrase, etc.
    public MaterialProvenance? Provenance { get; init; }
    public IReadOnlySet<string> Tags { get; init; }
}
```

#### Domain and Kind Enums

```csharp
// PartTrackDomain - Time semantics
public enum PartTrackDomain
{
    SongAbsolute = 0,  // Ticks from song start (global timeline)
    MaterialLocal = 1  // Ticks from fragment start (local, always >= 0)
}

// PartTrackKind - Intent/category
public enum PartTrackKind
{
    RoleTrack = 0,          // Song playback track
    MaterialFragment = 1,   // Reusable template (riff/hook/phrase)
    MaterialVariant = 2     // Transformed output from fragment
}

// MaterialKind - Classification
public enum MaterialKind
{
    Unknown = 0, Riff = 1, Hook = 2, MelodyPhrase = 3,
    DrumFill = 4, BassFill = 5, CompPattern = 6, KeysPattern = 7
}
```

#### MotifSpec (Motif Definition)

```csharp
public record MotifSpec(
    PartTrack.PartTrackId MotifId,
    string Name,
    string IntendedRole,
    MaterialKind Kind,
    IReadOnlyList<int> RhythmShape,      // Tick positions from motif start
    ContourIntent Contour,                // Up, Down, Arch, Flat, ZigZag
    RegisterIntent Register,              // CenterMidiNote, RangeSemitones
    TonePolicy TonePolicy,                // ChordToneBias, AllowPassingTones
    IReadOnlySet<string> Tags);
```

#### MaterialBank (Container)

```csharp
public sealed class MaterialBank
{
    public void Add(PartTrack track);
    public bool TryGet(PartTrack.PartTrackId id, out PartTrack? track);
    public IReadOnlyList<PartTrack> GetByKind(PartTrackKind kind);
    public IReadOnlyList<PartTrack> GetByMaterialKind(MaterialKind materialKind);
    public IReadOnlyList<PartTrack> GetByRole(string intendedRole);
    public IReadOnlyList<PartTrack> GetMotifsByRole(string intendedRole);
    public PartTrack? GetMotifByName(string name);
}
```

### Motif Pipeline

1. **MotifLibrary** — Hardcoded test motifs (ClassicRockHookA, SteadyVerseRiffA, etc.)
2. **MotifPlacementPlanner** — Determines WHICH motifs WHERE (section type, role, variation intensity)
3. **MotifPlacement** — Record: MotifId, AbsoluteSectionIndex, StartBarWithinSection, DurationBars, VariationIntensity
4. **MotifPlacementPlan** — Collection of MotifPlacement records
5. **MotifPresenceMap** — Query if motif active at (section, bar, role)
6. **MotifRenderer** — Renders motif spec + placement to PartTrack (currently commented out / WIP)

Generator/Material/                     # Processing & generation
  ├── MotifPlacementPlanner.cs          # Story 9.1 (planning)
  ├── MotifPresenceMap.cs               # Story 9.3 (coordination)
  └── MotifRenderer.cs                  # Story 9.2 (rendering)
---

## 7) RNG System

Location: `Generator/Core/Randomization/Rng.cs`

```csharp
public enum RandomPurpose
{
    DrumGenerator,
    GrooveVariationGroupPick, GrooveCandidatePick, GrooveTieBreak,
    GroovePrunePick, GrooveDensityPick, GrooveVelocityJitter,
    GrooveGhostVelocityPick, GrooveTimingJitter, GrooveSwingJitter,
    GrooveFillPick, GrooveAccentPick, GrooveGhostNotePick,
    GrooveOrnamentPick, GrooveCymbalPick, GrooveDynamicsPick
}

// Usage:
Rng.Initialize(seed);  // Call once at app start
int n = Rng.NextInt(RandomPurpose.DrumGenerator, 0, 100);
double d = Rng.NextDouble(RandomPurpose.GrooveVelocityJitter);
```

**Same seed → identical sequences per Purpose.** Order of RandomPurpose enum values must remain stable.

---

## 8) MIDI Pipeline

### Export Pipeline

Location: `Converters/`

```
ConvertPartTracksToMidiSongDocument_For_Play_And_Export.Convert(songTracks, tempoTrack, timeSignatureTrack)
  → Step 1: Convert PartTracks to absolute-time MIDI events
  → Step 2: Merge by instrument, inject tempo/time signature
  → Step 3: Materialize to MidiSongDocument
```

**Validation:** All PartTrack events must be sorted by `AbsoluteTimeTicks` before entering pipeline.

### MIDI Services

Location: `Midi/`

| Service | Purpose |
|---------|---------|
| `MidiIoService` | Import/export MIDI files (wraps DryWetMidi) |
| `MidiPlaybackService` | Play/Pause/Stop/Resume playback |
| `MidiSongDocument` | Wrapper around DryWetMidi `MidiFile` with metadata |
| `Player.cs` | Software synth integration (MeltySynth) |

---

## 9) Harmony Infrastructure

Location: `Song/Harmony/`

| File | Purpose |
|------|---------|
| `HarmonyTrack.cs` | Timeline of harmony events |
| `HarmonyEvent.cs` | Single chord: Key, Degree, Quality, Bass |
| `HarmonyPitchContext.cs` | Chord-scale/pitch material per slot |
| `HarmonyPitchContextBuilder.cs` | Build pitch contexts from harmony track |
| `ChordRealization.cs` | Concrete voicing with MIDI notes |
| `VoiceLeadingSelector.cs` | Cost-based successive voicing selection |
| `CompVoicingSelector.cs` | Guide-tone biased comp voicings |
| `StrumTimingEngine.cs` | Micro-offset spread within chords |
| `ChordVoicingHelper.cs` | Voicing utilities |
| `HarmonyValidator.cs` | Validation gate before generation |
| `HarmonyEventNormalizer.cs` | Normalization pass |
| `PitchClassUtils.cs` | Key parsing utilities |

---

## 10) Voice and Role System

Location: `Song/Voices/`

```csharp
// VoiceSet.cs
public sealed class VoiceSet
{
    public static IReadOnlyList<string> ValidGrooveRoles { get; } = 
        new List<string> { "Select...", "Pads", "Comp", "Bass", "DrumKit" };
    
    public List<Voice> Voices { get; set; }
    public void AddVoice(string voiceName, string grooveRole);
}

// Voice.cs
public class Voice
{
    public required string VoiceName { get; set; }  // MIDI program name
    public string GrooveRole { get; set; }          // Role tag
}
```

### GrooveRoles Constants

```csharp
public static class GrooveRoles
{
    public const string Kick = "Kick";
    public const string Snare = "Snare";
    public const string ClosedHat = "ClosedHat";
    public const string OpenHat = "OpenHat";
    public const string DrumKit = "DrumKit";
    public const string Bass = "Bass";
    public const string Comp = "Comp";
    public const string Pads = "Pads";
    public const string Keys = "Keys";
    public const string Lead = "Lead";
}
```

---

## 11) Test Infrastructure

### Main Project Tests (compiled with app)

| Location | Content |
|----------|---------|
| `Song/Section/SectionTests.cs` | Section track test data |
| `Song/Harmony/HarmonyTests.cs` | Harmony track test data |
| `Song/Tempo/TempoTests.cs` | Tempo track test data |
| `Song/Timing/TimingTests.cs` | Time signature test data |
| `Song/Material/Tests/*.cs` | Material system tests |
| `Generator/TestSetups/TestDesigns.cs` | Canonical test song context |
| `Generator/Groove/GrooveTestSetup.cs` | PopRockBasic groove preset |
| `Writer/WriterForm/WriterFormTests.cs` | UI tests |

### Music.Tests Project (separate)

Location: `Music.Tests/`

Uses **xUnit** test framework. Contains unit and integration tests for groove system.

**Test packages:** `xunit`, `FluentAssertions`, `NSubstitute`, `AutoFixture.AutoNSubstitute`

**Test conventions:**
- Constructor-based RNG initialization: `Rng.Initialize(42);`
- Method naming: `<Component>_<Condition>_<ExpectedResult>`
- Use `#region` blocks to organize test categories

### Groove System Tests (Story H1)

| File | Purpose |
|------|---------|
| `GrooveOutputContractsTests.cs` | Story A1: Stable groove output types |
| `GrooveRngStreamPolicyTests.cs` | Story A2: Deterministic RNG streams |
| `GroovePolicyHookTests.cs` | Story A3: Policy provider hooks |
| `VariationLayerMergeTests.cs` | Story B1: Layer merge (additive/replace) |
| `CandidateFilterTests.cs` | Story B2: Tag-based filtering |
| `WeightedCandidateSelectionTests.cs` | Story B3: Weighted selection + tie-breaks |
| `GrooveCandidateSourceTests.cs` | Story B4: Candidate source adapter |
| `DensityTargetComputationTests.cs` | Story C1: Density calculation |
| `CapEnforcementTests.cs` | Story C3: Hard caps + pruning |
| `OnsetStrengthClassifierTests.cs` | Story D1: Onset strength (66 tests) |
| `VelocityShaperTests.cs` | Story D2: Velocity shaping (41 tests) |
| `FeelTimingEngineTests.cs` | Story E1: Feel timing |
| `RoleTimingEngineTests.cs` | Story E2: Role micro-timing |
| `OverrideMergePolicyMatrixTests.cs` | Story F1/H1: Override policy matrix (18 tests) |
| `GrooveBarDiagnosticsTests.cs` | Story G1: Structured diagnostics |
| `GrooveOnsetProvenanceTests.cs` | Story G2: Onset provenance |
| `PartTrackBarCoverageAnalyzerTests.cs` | Story SC1: Bar coverage analysis |
| `GroovePhaseIntegrationTests.cs` | Story H1: Narrow integration tests (8 tests) |
| `GrooveCrossComponentTests.cs` | Story H1: Cross-component verification (8 tests) |

### Test Fixtures

Location: `Music.Tests/TestFixtures/`

| File | Purpose |
|------|---------|
| `GrooveSnapshotHelper.cs` | Story H1/H2: Snapshot serialization for golden tests |


```csharp
// Usage for golden tests (H2)
var snapshot = GrooveSnapshotHelper.CreateSnapshot(plan, "Kick");
string json = GrooveSnapshotHelper.SerializeSnapshot(snapshot);
var restored = GrooveSnapshotHelper.DeserializeSnapshot(json);
bool equal = GrooveSnapshotHelper.SnapshotsEqual(expected, actual);
```

---

## 12) Key Conventions

### Bars and Beats

- **Bars are 1-based** throughout (UI, tracks, generator)
- **Beats are 1-based** within bar (e.g., beat 1.5 = first eighth offbeat)
- **BarTrack is read-only to generator** — rebuilt by editors

### Timing

- All tick calculations use `MusicConstants.TicksPerQuarterNote` (480)
- PartTrackEvent uses `AbsoluteTimeTicks` (from song start)
- Material uses `MaterialLocal` domain (from fragment start)

### Determinism

- **Same seed → identical output**
- Variation tied to `(seed, grooveName, sectionType, barIndex)`
- Randomness is tie-break only; inputs drive selection

### Validation

- Harmony validated before generation (`HarmonyValidator`)
- MIDI export validates sorted event ordering
- Protection system ensures must-hit onsets

### File Organization

| Folder | Purpose |
|--------|---------|
| `Generator/Core/` | Entrypoint, RNG, overlap helpers |
| `Generator/Drums/` | Drum track generator |
| `Generator/Groove/` | Groove system (policies, selection, protection) |
| `Generator/Agents/` | Shared agent infrastructure: contracts, memory, and operator primitives for instrument agents |

| `Song/` | Data model (Section, Harmony, Tempo, Timing, PartTrack, Bar, Voices, Lyrics) |
| `Song/Material/` | Material/motif system |
| `Converters/` | PartTrack ↔ MIDI conversion |
| `Midi/` | MIDI I/O and playback services |
| `Writer/` | WinForms UI (WriterForm, EditorForms, SongGrid) |
| `Core/` | Globals, Constants, Helpers |
| `Errors/` | Exception handling, MessageBox helpers |

---

## 13) Current Work in Progress

### Groove System (Active Development)

The groove system is being rebuilt story by story. Current state:
- **Stories A1-A3 (COMPLETED):** Stable output types (GrooveOnset, GrooveBarContext, GrooveBarPlan), deterministic RNG streams, drummer policy hooks
- **Stories B1-B4 (COMPLETED):** Variation layer merge, candidate filtering, weighted selection, operator candidate source hook
- **Stories C1-C3 (COMPLETED):** Density target computation, selection until target, hard caps enforcement
- **Stories D1-D2 (COMPLETED):** Onset strength classification (all meters + grid-aware), velocity shaping (role × strength)
- **Story E1 (COMPLETED):** Feel timing (Straight/Swing/Shuffle/Triplet) with `FeelTimingEngine`
- **Story E2 (COMPLETED):** Role timing feel + bias + clamp with `RoleTimingEngine`
- **Story SC1 (COMPLETED):** Part track bar coverage analysis with `PartTrackBarCoverageAnalyzer`
- **Story F1 (COMPLETED):** Override merge policy enforcement with `GrooveOverrideMergePolicy`
- **Story G1 (COMPLETED):** Groove decision trace with structured `GrooveBarDiagnostics` (opt-in, zero-cost when disabled)
- **Story G2 (COMPLETED):** Onset provenance with `GrooveOnsetProvenance`
- **Story H1 (COMPLETED):** Full groove phase unit tests with override policy matrix, integration tests, and snapshot helpers

**Remaining work:**
- Story H2: End-to-end groove regression snapshot (golden test)

### Human Drummer Agent (Active Development)

The drummer agent implements human-like drumming using the groove system hooks.

**Completed Stories:**
- **Story 1.1-1.4 (COMPLETED):** Common agent infrastructure (IMusicalOperator, AgentContext, IAgentMemory, OperatorFamily, AgentMemory, OperatorSelectionEngine, StyleConfiguration)
- **Story 2.1 (COMPLETED):** DrummerContext extending AgentContext with drum-specific fields (HatMode, HatSubdivision, IsFillWindow, BackbeatBeats)
- **Story 2.2 (COMPLETED):** DrumCandidate record with role, position, strength, hints, articulation, fill role, and score
- **Story 2.3 (COMPLETED):** DrummerPolicyProvider implementing IGroovePolicyProvider
- **Story 2.4 (COMPLETED):** DrummerCandidateSource implementing IGrooveCandidateSource with operator registry, mapping, grouping, and physicality filter stubs

**Key Types:**

| Type | Purpose |
|------|---------|
| `IDrumOperator` | Drum-specific operator interface extending IMusicalOperator<DrumCandidate> |
| `DrumOperatorRegistry` | Registry for operator discovery and filtering by family/style |
| `DrumCandidateMapper` | Maps DrumCandidate → GrooveOnsetCandidate with tag-based hints |
| `DrummerCandidateSource` | IGrooveCandidateSource implementation gathering operator candidates |
| `PhysicalityFilter` | Stub filter for playability validation (full implementation Story 4.3) |
| `PhysicalityRules` | Configuration for hit caps, limb rules, strictness levels |

**Architecture Pattern:**

```
DrummerCandidateSource.GetCandidateGroups(barContext, role)
  │
  ├─ Build DrummerContext from GrooveBarContext
  ├─ Get enabled operators (style + policy filtering)
  ├─ For each operator:
  │    ├─ CanApply() pre-filter check
  │    └─ GenerateCandidates() → List<DrumCandidate>
  ├─ Map DrumCandidate → GrooveOnsetCandidate (preserves hints as tags)
  ├─ Group by OperatorFamily → GrooveCandidateGroup[]
  └─ PhysicalityFilter.Filter() → pruned groups
```

**Files:**

| File | Purpose |
|------|---------|
| `Generator/Agents/Common/IMusicalOperator.cs` | Generic operator interface |
| `Generator/Agents/Common/AgentContext.cs` | Base context for all agents |
| `Generator/Agents/Common/OperatorFamily.cs` | Operator classification enum |
| `Generator/Agents/Common/AgentMemory.cs` | Anti-repetition memory |
| `Generator/Agents/Common/StyleConfiguration.cs` | Style weights and caps |
| `Generator/Agents/Drums/IDrumOperator.cs` | Drum-specific operator interface |
| `Generator/Agents/Drums/DrumOperatorRegistry.cs` | Operator registry and discovery |
| `Generator/Agents/Drums/DrumCandidate.cs` | Drum event candidate record |
| `Generator/Agents/Drums/DrumCandidateMapper.cs` | DrumCandidate → GrooveOnsetCandidate mapper |
| `Generator/Agents/Drums/DrummerCandidateSource.cs` | IGrooveCandidateSource implementation |
| `Generator/Agents/Drums/DrummerContext.cs` | Drum-specific context |
| `Generator/Agents/Drums/DrummerPolicyProvider.cs` | IGroovePolicyProvider implementation |
| `Generator/Agents/Drums/Physicality/PhysicalityFilter.cs` | Playability filter stub |
| `Generator/Agents/Drums/Physicality/PhysicalityRules.cs` | Physicality configuration |

**Remaining Drummer Agent Work:**
- Story 2.5: DrummerMemory (fill shape tracking)
- Stories 3.1-3.6: Drum operators (28 total)
- Stories 4.1-4.4: Full physicality constraints (limb model, sticking rules)
- Stories 5.1-5.3: Pop Rock style configuration

### Material/Motif System

- Data definitions complete (MotifSpec, MotifPlacement, MaterialBank)
- Placement planner implemented
- Renderer partially implemented (commented out)
- Not yet integrated into generation

---

## 14) Quick Reference

### Creating a Test Song Context

```csharp
var songContext = new SongContext();
TestDesigns.SetTestDesignD1(songContext);  // Populates all tracks for 48 bars
```

### Creating a Groove Preset

```csharp
var preset = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
    songContext.SectionTrack,
    out var segmentProfiles,
    beatsPerBar: 4);
songContext.GroovePresetDefinition = preset;
songContext.SegmentGrooveProfiles = segmentProfiles;
```

### Running Generation

```csharp
var drumTrack = Generator.Generate(songContext);
songContext.Song.PartTracks.Add(drumTrack);
```

### Exporting to MIDI

```csharp
var midiDoc = ConvertPartTracksToMidiSongDocument_For_Play_And_Export.Convert(
    songContext.Song.PartTracks,
    songContext.Song.TempoTrack,
    songContext.Song.TimeSignatureTrack);
new MidiIoService().ExportToFile("output.mid", midiDoc);
```






---

## 15) Agent Infrastructure (Stories 1.1-1.4)

Location: `Generator/Agents/Common/`

Purpose: Shared contracts and base types for all instrument agents (Drums, Guitar, Keys, Bass, Vocals).

Files added in Story 1.1:

```
Generator/Agents/Common/
  ├── IMusicalOperator.cs          (generic operator interface)
  ├── AgentContext.cs             (shared, immutable context record)
  ├── IAgentMemory.cs             (memory interface for anti-repetition)
  ├── OperatorFamily.cs           (stable operator classification enum)
  └── FillShape.cs                (fill metadata structure for memory)
```

Files added in Story 1.2:

```
Generator/Agents/Common/
  ├── AgentMemory.cs              (concrete IAgentMemory implementation)
  └── DecayCurve.cs               (enum for repetition penalty decay)
```

Files added in Story 1.3:

```
Generator/Agents/Common/
  └── OperatorSelectionEngine.cs  (selection engine with scoring and density/cap limits)
```

Files added in Story 1.4:

```
Generator/Agents/Common/
  ├── StyleConfiguration.cs        (FeelRules, GridRules, StyleConfiguration records)
  └── StyleConfigurationLibrary.cs (registry with PopRock, Jazz, Metal presets)
```

Notes:
- `AgentContext` is a record to ensure immutability and determinism.
- `IMusicalOperator<TCandidate>` is generic; instrument agents implement specialized versions.
- `OperatorFamily` enum values are stable and must not be reordered.
- `IAgentMemory` provides decision recording and lookups used by selection engine.
- `AgentMemory` uses circular buffer for last-N-bars tracking with configurable window size.
- `GetRepetitionPenalty()` supports Linear and Exponential decay curves.
- All collections use sorted order for deterministic iteration.
- `OperatorSelectionEngine<TCandidate>` selects candidates using weighted scoring.
- Score formula: `finalScore = baseScore * styleWeight * (1.0 - repetitionPenalty)`.
- Deterministic tie-breaking: score desc → operatorId asc → candidateId asc.
- Respects density targets (stop when reached) and hard caps (never exceed).
- `StyleConfiguration` separates genre behavior from operator logic (immutable record).
- `StyleConfigurationLibrary.GetStyle(styleId)` provides case-insensitive lookup.
- Predefined styles: PopRock (straight/16th), Jazz (swing/triplets), Metal (dense/16th).
- Missing operator weights default to 0.5; missing role caps default to int.MaxValue.

---

## 16) Drummer Agent Infrastructure (Stories 2.1-2.2)

Location: `Generator/Agents/Drums/`

Purpose: Drummer-specific context, candidate types, and enums for the Pop Rock Human Drummer agent.

### Files added in Story 2.1:

```
Generator/Agents/Drums/
  ├── DrummerContext.cs            (extends AgentContext with drum-specific fields)
  └── DrummerContextBuilder.cs     (builds DrummerContext from GrooveBarContext + policies)
```

Key types:
- `HatMode` enum: Closed, Open, Ride (which cymbal is timekeeping)
- `HatSubdivision` enum: None, Eighth, Sixteenth (hi-hat density)
- `DrummerContext` record: extends AgentContext with ActiveRoles, LastKickBeat, LastSnareBeat, CurrentHatMode, HatSubdivision, IsFillWindow, IsAtSectionBoundary, BackbeatBeats, BeatsPerBar

### Files added in Story 2.2:

```
Generator/Agents/Drums/
  ├── DrumCandidate.cs             (drum event candidate generated by operators)
  ├── DrumArticulation.cs          (articulation hints: Rimshot, SideStick, OpenHat, Crash, etc.)
  └── FillRole.cs                  (fill pattern role: None, Setup, FillStart, FillBody, FillEnd)
```

Key types:

```csharp
// DrumCandidate - operator-generated event candidate
public sealed record DrumCandidate
{
    public required string CandidateId { get; init; }      // Format: "{OperatorId}_{Role}_{BarNumber}_{Beat}"
    public required string OperatorId { get; init; }
    public required string Role { get; init; }             // Kick, Snare, ClosedHat, etc.
    public required int BarNumber { get; init; }           // 1-based
    public required decimal Beat { get; init; }            // 1-based, fractional
    public required OnsetStrength Strength { get; init; }
    public int? VelocityHint { get; init; }                // 0-127, nullable
    public int? TimingHint { get; init; }                  // Tick offset, nullable
    public DrumArticulation? ArticulationHint { get; init; }
    public required FillRole FillRole { get; init; }
    public required double Score { get; init; }            // 0.0-1.0
}

// FillRole - candidate's role within fill pattern
public enum FillRole { None, Setup, FillStart, FillBody, FillEnd }

// DrumArticulation - playing technique hints
public enum DrumArticulation { None, Rimshot, SideStick, OpenHat, Crash, Ride, RideBell, CrashChoke, Flam }
```

Notes:
- `DrumCandidate.CandidateId` is deterministic: `"{OperatorId}_{Role}_{BarNumber}_{Beat}"` with optional articulation suffix.
- `VelocityHint` and `TimingHint` are nullable; downstream shapers compute defaults when null.
- `Score` must be in [0.0, 1.0]; operators are responsible for returning valid scores.
- `DrumCandidate.TryValidate()` validates all field constraints.
- `DrumCandidate.GenerateCandidateId()` helper creates stable IDs.
- `DrumCandidate.CreateMinimal()` helper for testing.

---

## 17) Drummer Policy Provider (Story 2.3)

Location: `Generator/Agents/Drums/DrummerPolicyProvider.cs`

Purpose: Implements `IGroovePolicyProvider` to drive groove system behavior from drummer context, style configuration, and memory state.

### Key Types

```csharp
// DrummerPolicyProvider - main policy provider
public sealed class DrummerPolicyProvider : IGroovePolicyProvider
{
    public DrummerPolicyProvider(
        StyleConfiguration styleConfig,
        IAgentMemory? memory = null,
        DrummerPolicySettings? settings = null);
    
    public GroovePolicyDecision? GetPolicy(GrooveBarContext barContext, string role);
}

// DrummerPolicySettings - configurable behavior
public sealed record DrummerPolicySettings
{
    public double EnergyDensityScale { get; init; } = 0.2;      // Energy's effect on density (±20%)
    public int FillWindowBars { get; init; } = 2;               // Bars from section end = fill window
    public bool AllowConsecutiveFills { get; init; } = false;
    public int MinBarsBetweenFills { get; init; } = 4;
}

// FillOperatorIds - known fill operator identifiers
public static class FillOperatorIds
{
    public const string TurnaroundFillShort = "TurnaroundFillShort";
    public const string TurnaroundFillFull = "TurnaroundFillFull";
    public const string BuildFill = "BuildFill";
    public const string DropFill = "DropFill";
    public const string SetupHit = "SetupHit";
    public const string StopTime = "StopTime";
    public const string CrashOnOne = "CrashOnOne";
    public static readonly IReadOnlySet<string> All = { ... };
}
```

### Behavior

**Density Override Computation:**
- Base density from section type: Intro=0.4, Verse=0.5, Chorus=0.8, Solo=0.7, Bridge=0.4, Outro=0.5
- Energy modifier applied: Chorus +0.2, Solo +0.1, Bridge/Intro/Outro -0.1
- Result clamped to [0.0, 1.0]

**Max Events Override:**
- Uses `StyleConfiguration.GetRoleCap(role)` from style (e.g., PopRock Kick=8, Snare=6, ClosedHat=16)
- Returns null if cap is int.MaxValue (no limit)

**Operator Allow List:**
- Respects `StyleConfiguration.AllowedOperatorIds` (empty = all allowed)
- Gates fill operators based on fill window + memory state
- Fill operators allowed when: `BarsUntilSectionEnd <= FillWindowBars` AND no recent fill in memory

**Timing Feel Override:**
- PopRock snare gets `TimingFeel.Behind` (laid-back feel)
- Other roles return null (use default)

**Velocity Bias Override:**
- Section-based: Chorus +10, Solo +5, Bridge -10, Intro/Outro -5, Verse 0
- Returns null if bias is 0

**Variation Tags Override:**
- Fill window: adds "Fill", "TurnAround"
- Section start (BarWithinSection=0): adds "SectionStart", "Crash"
- Near section end (BarsUntilSectionEnd<=2): adds "Buildup", "SectionEnd"

### Precedence Rules

Policy override precedence: **immediate context > memory > style config**

1. Immediate context wins (e.g., IsFillWindow status)
2. Memory takes second priority (anti-repetition)
3. Style config provides defaults when no overrides

### Determinism

- Same inputs → same `GroovePolicyDecision` (pure function)
- No RNG calls (determinism from inputs only)
- Read-only memory access (no mutations)

### Memory Integration

- Fill gating checks `IAgentMemory.GetLastFillShape()`
- Consecutive fills blocked if `barsSinceLastFill < MinBarsBetweenFills`
- Memory is optional (null = no memory influence)

### Error Handling

- `ArgumentNullException` for null barContext or role
- Unknown roles return `GroovePolicyDecision.NoOverrides` (safe fallback)
- No exceptions for missing style config fields (uses defaults)

### Test Coverage

Tests in `Music.Tests/Generator/Agents/Drums/DrummerPolicyProviderTests.cs`:
- Construction validation
- GetPolicy basic validation (null checks, unknown roles)
- Determinism (same inputs → same outputs)
- Density overrides (section-based, clamped to [0.0, 1.0])
- Max events caps (respects style RoleCaps)
- Fill window gating (variation tags, operator allow list)
- Memory integration (recent fills block new fills)
- Timing feel overrides (PopRock snare = Behind)
- Velocity bias overrides (section-based)
- All drum roles coverage

Notes:
- DrummerPolicyProvider is **stateless** and **thread-safe**
- Policy decisions are computed per-bar, per-role
- Integration with DrummerCandidateSource happens in Story 2.4
- Operator weights remain in StyleConfiguration; policy only gates eligibility














