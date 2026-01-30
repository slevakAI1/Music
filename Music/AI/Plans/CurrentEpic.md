# Drum Generator V2 Epic: Phrase-Based Pattern Recognition

**Purpose:** Transform the drum generator from random bar-by-bar generation to phrase-based composition with recognizable patterns, purposeful repetition, and meaningful variation.

**Problem Statement:** The current drum generator produces output that is mathematically varied but perceptually similar across different seeds. Each bar is generated independently, resulting in tracks that sound "randomly busy" rather than musically coherent. Human drum tracks establish patterns that listeners recognize and anticipate, with variations that feel purposeful rather than arbitrary.

**Solution Approach:** 
1. Generate short drum phrases (2-4 bars) as reusable building blocks
2. Store phrases in MaterialBank as first-class material objects
3. Place phrases throughout the song with section-appropriate selection
4. Evolve phrases for variety while maintaining recognizable identity
5. Reserve space for fills (future epic)

**Key Insight:** Changing the seed should produce a *different pattern* that is then *repeated purposefully*, not *different randomness everywhere*.

---

## Architecture Overview

### Current State
```
SongContext → GrooveBasedDrumGenerator → (per-bar random operators) → PartTrack
```

### Target State
```
Phase 1: Phrase Generation
  User → "Generate Phrase" button → DrumPhraseGenerator → PartTrack (phrase) → SongGrid

Phase 2: Phrase Storage  
  User → Select phrases in grid → "Save to Bank" button → MaterialBank (stores as DrumPhrase)

Phase 3: Track Generation
  SongContext + MaterialBank → DrumGenerator → (phrase placement + evolution) → PartTrack
```

### Key Types

| Type | Location | Purpose |
|------|----------|---------|
| `DrumPhrase` | `Song/Material/` | Phrase data model (extends MotifSpec or new record) |
| `DrumPhraseGenerator` | `Generator/Agents/Drums/` | Renamed from GrooveBasedDrumGenerator; generates single phrases |
| `DrumGenerator` | `Generator/Agents/Drums/` | New; places and evolves phrases into complete track |
| `DrumPhraseEvolver` | `Generator/Agents/Drums/` | Applies bounded evolution to phrases |
| `DrumPhrasePlacementPlan` | `Generator/Agents/Drums/` | Where phrases go in song structure |

---

## Epic Stories

### Phase 1: Foundation Refactoring

---

#### Story 1.1: Rename GrooveBasedDrumGenerator to DrumPhraseGenerator (Completed)

**Size:** Small (1-2 hours)

**Goal:** Rename `GrooveBasedDrumGenerator` to `DrumPhraseGenerator` to clarify its role as a phrase generator, not a full track generator.

**Files to Modify:**
- `Generator/Agents/Drums/GrooveBasedDrumGenerator.cs` → rename to `DrumPhraseGenerator.cs`
- `Generator/Core/Generator.cs` — update references
- `Writer/WriterForm/HandleCommandAgentTest.cs` — update references
- All test files referencing `GrooveBasedDrumGenerator`

**Implementation Steps:**
1. Rename `GrooveBasedDrumGenerator.cs` to `DrumPhraseGenerator.cs`
2. Rename the class from `GrooveBasedDrumGenerator` to `DrumPhraseGenerator`
3. Update all `using` statements and references in:
   - `Generator.cs` (calls `new GrooveBasedDrumGenerator(...)`)
   - `HandleCommandAgentTest.cs` (if any direct references)
   - Test files in `Music.Tests/Generator/Agents/Drums/`
4. Update AI comments to reflect new purpose:
   ```csharp
   // AI: purpose=Generates a drum phrase (1-N bars) using operator-based variation over anchors.
   // AI: invariants=Output is a PartTrack representing a single phrase; reusable for MaterialBank storage.
   ```
5. Run build and fix any remaining references
6. Run all tests to verify no regressions

**Acceptance Criteria:**
- [ ] File renamed to `DrumPhraseGenerator.cs`
- [ ] Class renamed to `DrumPhraseGenerator`
- [ ] All references updated (no build errors)
- [ ] All existing tests pass
- [ ] AI comments updated to reflect phrase generation purpose

**Notes:**
- This is a pure rename refactor with no behavior changes
- The class will later be enhanced to focus on phrase generation specifically

---

#### Story 1.2: Add "Generate Phrase" Command to WriterForm

**Size:** Medium (2-3 hours)

**Goal:** Add a new command "Generate Phrase" to the WriterForm command dropdown that generates a drum phrase and displays it in the song grid.

**Files to Create:**
- `Writer/WriterForm/HandleCommandGeneratePhrase.cs`
- `Writer/WriterForm/PhraseSettingsDialog.cs` + `.Designer.cs`

**Files to Modify:**
- `Writer/WriterForm/WriterForm.cs` — add case for "Generate Phrase" command
- `Writer/WriterForm/WriterForm.Designer.cs` — add "Generate Phrase" to cbCommand items

**Implementation Steps:**

1. **Create `PhraseSettingsDialog`** (WinForms modal dialog):
   ```
   Layout:
   ┌─────────────────────────────────────────┐
   │ Generate Drum Phrase                    │
   ├─────────────────────────────────────────┤
   │  Number of Bars: [___4___] (NumericUpDown, min=1, max=8, default=4)
   │  Seed:           [_12345_] (NumericUpDown, min=1, max=999999)
   │  Genre:          [PopRock ▼] (ComboBox)
   │                                         │
   │              [OK]  [Cancel]             │
   └─────────────────────────────────────────┘
   ```
   - Properties: `Bars` (int), `Seed` (int), `Genre` (string)
   - Default seed from `Random.Shared.Next(1, 100000)`
   - Genre list from `GrooveAnchorFactory.GetAvailableGenres()`

2. **Create `HandleCommandGeneratePhrase.cs`**:
   ```csharp
   public static class HandleCommandGeneratePhrase
   {
       public static void Execute(SongContext songContext, DataGridView dgSong)
       {
           // 1. Validate songContext has BarTrack
           // 2. Show PhraseSettingsDialog
           // 3. If OK:
           //    a. Initialize Rng with seed
           //    b. Create temporary SongContext for phrase generation:
           //       - Create SectionTrack with single section of N bars
           //       - Use existing BarTrack (or create minimal one)
           //       - Set GroovePresetDefinition from genre
           //    c. Call DrumPhraseGenerator.Generate(tempContext, maxBars)
           //    d. Set result.MidiProgramName = $"Phrase (Seed: {seed}, {bars} bars)"
           //    e. Add result to songContext.Song.PartTracks
           //    f. Add to grid via SongGridManager.AddNewPartTrack
           //    g. Show success message
       }
   }
   ```

3. **Update WriterForm.cs** (in `btnExecute_Click`):
   ```csharp
   case "Generate Phrase":
       HandleCommandGeneratePhrase.Execute(_songContext, dgSong);
       break;
   ```

4. **Update WriterForm.Designer.cs**:
   - Add "Generate Phrase" to `cbCommand.Items` collection

**Acceptance Criteria:**
- [ ] "Generate Phrase" appears in command dropdown
- [ ] Clicking Execute shows `PhraseSettingsDialog`
- [ ] Dialog validates input (bars 1-8, seed positive)
- [ ] Phrase is generated with specified parameters
- [ ] Phrase appears in song grid with descriptive name
- [ ] Same seed + bars + genre produces identical phrase
- [ ] Different seed produces different phrase

**Testing Workflow:**
1. Set test scenario D1 (to get BarTrack)
2. Select "Generate Phrase" from dropdown
3. Click Execute
4. Enter 4 bars, seed 12345, PopRock
5. Click OK
6. Verify phrase appears in grid
7. Click Play to hear the 4-bar phrase

---

#### Story 1.3: Create DrumPhrase Material Type

**Size:** Small (1-2 hours)

**Goal:** Create a data model for drum phrases that can be stored in `MaterialBank`.

**Files to Create:**
- `Song/Material/DrumPhrase.cs`

**Files to Modify:**
- `Song/Material/MaterialKind.cs` — add `DrumPhrase` enum value
- `Song/Material/MaterialBank.cs` — add phrase-specific query methods

**Implementation Steps:**

1. **Add `DrumPhrase` to `MaterialKind` enum**:
   ```csharp
   public enum MaterialKind
   {
       // ... existing values ...
       DrumPhrase,  // Drum pattern phrase (2-8 bars)
   }
   ```

2. **Create `DrumPhrase.cs`**:
   ```csharp
   // AI: purpose=Represents a reusable drum phrase (1-8 bars) for pattern-based drum generation.
   // AI: invariants=BarCount >= 1; Events sorted by AbsoluteTimeTicks; Genre non-empty.
   // AI: deps=Stored in MaterialBank; consumed by DrumGenerator for phrase-based placement.
   
   namespace Music.Generator;
   
   public sealed record DrumPhrase
   {
       /// <summary>Unique identifier for this phrase.</summary>
       public required string PhraseId { get; init; }
       
       /// <summary>Display name for UI.</summary>
       public required string Name { get; init; }
       
       /// <summary>Number of bars in this phrase.</summary>
       public required int BarCount { get; init; }
       
       /// <summary>Genre/style this phrase was generated for.</summary>
       public required string Genre { get; init; }
       
       /// <summary>Seed used to generate this phrase (for reproducibility).</summary>
       public required int Seed { get; init; }
       
       /// <summary>The actual drum events (relative to phrase start, bar 1 = tick 0).</summary>
       public required IReadOnlyList<PartTrackEvent> Events { get; init; }
       
       /// <summary>Optional tags for categorization (e.g., "verse", "chorus", "sparse", "busy").</summary>
       public IReadOnlyList<string> Tags { get; init; } = [];
       
       /// <summary>Energy level hint (0.0 = sparse, 1.0 = dense). Used for matching to sections.</summary>
       public double EnergyHint { get; init; } = 0.5;
       
       /// <summary>Creates a DrumPhrase from a PartTrack.</summary>
       public static DrumPhrase FromPartTrack(
           PartTrack partTrack, 
           string phraseId, 
           string name, 
           int barCount, 
           string genre, 
           int seed,
           IReadOnlyList<string>? tags = null,
           double energyHint = 0.5)
       {
           // Convert events to phrase-relative timing (bar 1 = tick 0)
           // This will be implemented based on BarTrack tick calculations
           return new DrumPhrase
           {
               PhraseId = phraseId,
               Name = name,
               BarCount = barCount,
               Genre = genre,
               Seed = seed,
               Events = partTrack.PartTrackNoteEvents.ToList(),
               Tags = tags ?? [],
               EnergyHint = energyHint
           };
       }
       
       /// <summary>Converts this phrase to a PartTrack at the specified start bar.</summary>
       public PartTrack ToPartTrack(BarTrack barTrack, int startBar)
       {
           // Offset events to start at specified bar
           // Implementation will use barTrack.GetTickForBar()
           throw new NotImplementedException("Story 1.4");
       }
   }
   ```

3. **Add to MaterialBank.cs**:
   ```csharp
   // Add field
   private readonly List<DrumPhrase> _drumPhrases = new();
   
   // Add methods
   public void AddDrumPhrase(DrumPhrase phrase)
   {
       ArgumentNullException.ThrowIfNull(phrase);
       _drumPhrases.Add(phrase);
   }
   
   public IReadOnlyList<DrumPhrase> GetDrumPhrases() => _drumPhrases.AsReadOnly();
   
   public IReadOnlyList<DrumPhrase> GetDrumPhrasesByGenre(string genre)
   {
       return _drumPhrases
           .Where(p => string.Equals(p.Genre, genre, StringComparison.OrdinalIgnoreCase))
           .ToList();
   }
   
   public DrumPhrase? GetDrumPhraseById(string phraseId)
   {
       return _drumPhrases.FirstOrDefault(p => p.PhraseId == phraseId);
   }
   
   public void ClearDrumPhrases() => _drumPhrases.Clear();
   ```

**Acceptance Criteria:**
- [ ] `DrumPhrase` record created with all required properties
- [ ] `MaterialKind.DrumPhrase` enum value added
- [ ] `MaterialBank` has add/get/query methods for drum phrases
- [ ] `FromPartTrack` factory method works correctly
- [ ] Unit tests for DrumPhrase creation and MaterialBank storage

---

#### Story 1.4: Implement DrumPhrase.ToPartTrack for Phrase Placement

**Size:** Small (1-2 hours)

**Goal:** Implement the `ToPartTrack` method that converts a `DrumPhrase` to a `PartTrack` at a specified start bar.

**Files to Modify:**
- `Song/Material/DrumPhrase.cs` — implement `ToPartTrack`

**Implementation Steps:**

1. **Implement `ToPartTrack`**:
   ```csharp
   /// <summary>
   /// Converts this phrase to a PartTrack starting at the specified bar.
   /// </summary>
   /// <param name="barTrack">BarTrack for tick calculations.</param>
   /// <param name="startBar">1-based bar number where phrase should start.</param>
   /// <returns>PartTrack with events offset to start at startBar.</returns>
   public PartTrack ToPartTrack(BarTrack barTrack, int startBar)
   {
       ArgumentNullException.ThrowIfNull(barTrack);
       if (startBar < 1)
           throw new ArgumentOutOfRangeException(nameof(startBar), "Must be >= 1");
       
       // Calculate tick offset: where bar 1 of phrase maps to startBar in song
       long phraseBar1Tick = barTrack.GetTickForBar(1);  // Tick for bar 1 in original phrase context
       long targetStartTick = barTrack.GetTickForBar(startBar);
       long tickOffset = targetStartTick - phraseBar1Tick;
       
       // Create new events with offset timing
       var offsetEvents = Events
           .Select(e => new PartTrackEvent
           {
               AbsoluteTimeTicks = e.AbsoluteTimeTicks + tickOffset,
               Type = e.Type,
               NoteNumber = e.NoteNumber,
               NoteDurationTicks = e.NoteDurationTicks,
               NoteOnVelocity = e.NoteOnVelocity
           })
           .OrderBy(e => e.AbsoluteTimeTicks)
           .ToList();
       
       return new PartTrack
       {
           MidiProgramName = $"{Name} @bar{startBar}",
           MidiProgramNumber = 255, // Drum set
           PartTrackNoteEvents = offsetEvents
       };
   }
   ```

2. **Add helper if needed in BarTrack**:
   ```csharp
   // In BarTrack.cs, ensure GetTickForBar exists:
   public long GetTickForBar(int barNumber)
   {
       // Implementation based on existing bar/tick calculations
   }
   ```

**Acceptance Criteria:**
- [ ] `ToPartTrack` correctly offsets events to start at specified bar
- [ ] Events remain sorted by `AbsoluteTimeTicks`
- [ ] Bar 1 in phrase maps to `startBar` in output
- [ ] Unit tests verify correct tick offsets for various start bars
- [ ] Unit tests verify multi-bar phrase placement

**Test Cases:**
1. 4-bar phrase placed at bar 1 → events at ticks 0-N
2. 4-bar phrase placed at bar 5 → events offset by 4 bars worth of ticks
3. Phrase with events at bar 2.5 placed at bar 9 → events at bar 10.5

---

### Phase 2: Phrase Storage Workflow

---

#### Story 2.1: Add "Save Selected to Material Bank" Functionality

**Size:** Medium (2-3 hours)

**Goal:** Allow user to select phrase tracks in the song grid and save them to the MaterialBank.

**Files to Create:**
- `Writer/WriterForm/HandleCommandSaveToBank.cs`
- `Writer/WriterForm/SaveToBankDialog.cs` + `.Designer.cs`

**Files to Modify:**
- `Writer/WriterForm/WriterForm.cs` — add button click handler
- `Writer/WriterForm/WriterForm.Designer.cs` — add "Save to Bank" button

**Implementation Steps:**

1. **Add button to WriterForm.Designer.cs**:
   - Add `btnSaveToBank` button near track management buttons
   - Text: "Save to Bank"
   - Wire to `btnSaveToBank_Click` handler

2. **Create `SaveToBankDialog`**:
   ```
   Layout:
   ┌─────────────────────────────────────────┐
   │ Save Phrase to Material Bank            │
   ├─────────────────────────────────────────┤
   │  Phrase Name: [__________________]      │
   │  Genre:       [PopRock ▼]               │
   │  Tags:        [________________] (comma-separated)
   │  Energy Hint: [====o====] (TrackBar 0-100, default 50)
   │                                         │
   │  Source Track: "Phrase (Seed: 12345, 4 bars)"
   │  Bar Count: 4                           │
   │                                         │
   │              [Save]  [Cancel]           │
   └─────────────────────────────────────────┘
   ```
   - Properties: `PhraseName`, `Genre`, `Tags` (List<string>), `EnergyHint` (0.0-1.0)
   - Pre-populate name from track name if it looks like a phrase
   - Pre-populate genre from track name if detectable

3. **Create `HandleCommandSaveToBank.cs`**:
   ```csharp
   public static class HandleCommandSaveToBank
   {
       public static void Execute(
           SongContext songContext, 
           DataGridView dgSong,
           MaterialBank materialBank)
       {
           // 1. Get selected rows from grid (excluding fixed rows)
           var selectedTracks = GetSelectedPartTracks(dgSong, songContext);
           
           if (selectedTracks.Count == 0)
           {
               ShowError("No tracks selected. Select one or more phrase tracks first.");
               return;
           }
           
           // 2. For each selected track, show SaveToBankDialog
           foreach (var (track, barCount) in selectedTracks)
           {
               using var dialog = new SaveToBankDialog(track, barCount);
               if (dialog.ShowDialog() == DialogResult.OK)
               {
                   // Create DrumPhrase from track
                   var phraseId = Guid.NewGuid().ToString("N")[..8];
                   var phrase = DrumPhrase.FromPartTrack(
                       track,
                       phraseId,
                       dialog.PhraseName,
                       barCount,
                       dialog.Genre,
                       ExtractSeedFromName(track.MidiProgramName),
                       dialog.Tags,
                       dialog.EnergyHint);
                   
                   materialBank.AddDrumPhrase(phrase);
                   ShowSuccess($"Saved phrase '{dialog.PhraseName}' to Material Bank");
               }
           }
       }
       
       private static List<(PartTrack Track, int BarCount)> GetSelectedPartTracks(
           DataGridView dgSong, 
           SongContext songContext)
       {
           // Implementation: iterate selected rows, get PartTrack from Tag
           // Calculate bar count from track events and BarTrack
       }
       
       private static int ExtractSeedFromName(string name)
       {
           // Parse "Phrase (Seed: 12345, 4 bars)" → 12345
           // Return 0 if not parseable
       }
   }
   ```

4. **Update WriterForm.cs**:
   ```csharp
   private void btnSaveToBank_Click(object sender, EventArgs e)
   {
       _songContext ??= new SongContext();
       _songContext.MaterialBank ??= new MaterialBank();
       HandleCommandSaveToBank.Execute(_songContext, dgSong, _songContext.MaterialBank);
   }
   ```

**Acceptance Criteria:**
- [ ] "Save to Bank" button visible in WriterForm
- [ ] Clicking with no selection shows error message
- [ ] Clicking with selection opens SaveToBankDialog
- [ ] Dialog pre-populates fields from track name
- [ ] Saved phrase appears in MaterialBank
- [ ] Phrase can be retrieved by ID, genre, or GetDrumPhrases()
- [ ] Multiple phrases can be saved in sequence

**Testing Workflow:**
1. Generate a phrase (Story 1.2)
2. Select the phrase row in grid
3. Click "Save to Bank"
4. Enter name, genre, tags
5. Click Save
6. Verify phrase in MaterialBank (via debug or diagnostic)

---

#### Story 2.2: Add Material Bank Diagnostic View

**Size:** Small (1-2 hours)

**Goal:** Add a way to view the current contents of the MaterialBank for debugging and verification.

**Files to Create:**
- `Writer/WriterForm/MaterialBankViewerDialog.cs` + `.Designer.cs`

**Files to Modify:**
- `Writer/WriterForm/WriterForm.cs` — add menu item or button
- `Writer/WriterForm/WriterForm.Designer.cs` — add UI element

**Implementation Steps:**

1. **Create `MaterialBankViewerDialog`**:
   ```
   Layout:
   ┌────────────────────────────────────────────────────────────┐
   │ Material Bank Contents                                     │
   ├────────────────────────────────────────────────────────────┤
   │ ┌────────────────────────────────────────────────────────┐ │
   │ │ [DataGridView - read only]                             │ │
   │ │ ID       | Name         | Bars | Genre  | Energy | Tags│ │
   │ │ a1b2c3d4 | Verse Beat   | 4    | PopRock| 0.5    | verse│
   │ │ e5f6g7h8 | Chorus Drive | 4    | PopRock| 0.8    | chorus│
   │ └────────────────────────────────────────────────────────┘ │
   │                                                            │
   │ Total Phrases: 2                                           │
   │                                                            │
   │                              [Close]  [Clear All]          │
   └────────────────────────────────────────────────────────────┘
   ```

2. **Add button/menu to WriterForm**:
   - Add "View Bank" button or menu item
   - Wire to handler that opens `MaterialBankViewerDialog`

**Acceptance Criteria:**
- [ ] Dialog shows all phrases in MaterialBank
- [ ] Displays ID, Name, BarCount, Genre, EnergyHint, Tags
- [ ] "Clear All" removes all phrases from bank
- [ ] Empty bank shows appropriate message
- [ ] Updates reflect current bank state when opened

---

### Phase 3: New Drum Generator with Phrase Placement

---

#### Story 3.1: Create DrumGenerator Skeleton with Phrase Placement

**Size:** Medium (3-4 hours)

**Goal:** Create a new `DrumGenerator` class that generates drum tracks by placing phrases from the MaterialBank.

**Files to Create:**
- `Generator/Agents/Drums/DrumGenerator.cs`
- `Generator/Agents/Drums/DrumPhrasePlacementPlan.cs`

**Files to Modify:**
- `Generator/Core/Generator.cs` — add new entry point for phrase-based generation

**Implementation Steps:**

1. **Create `DrumPhrasePlacementPlan.cs`**:
   ```csharp
   // AI: purpose=Defines where phrases are placed in the song structure.
   // AI: invariants=Placements are non-overlapping; StartBar >= 1; all bars covered or explicitly empty (fills).
   
   namespace Music.Generator.Agents.Drums;
   
   public sealed record DrumPhrasePlacement
   {
       /// <summary>ID of phrase from MaterialBank.</summary>
       public required string PhraseId { get; init; }
       
       /// <summary>1-based bar where phrase starts.</summary>
       public required int StartBar { get; init; }
       
       /// <summary>Number of bars this placement covers.</summary>
       public required int BarCount { get; init; }
       
       /// <summary>Evolution level applied to this instance (0 = original, 1+ = evolved).</summary>
       public int EvolutionLevel { get; init; } = 0;
       
       /// <summary>Optional evolution parameters.</summary>
       public DrumPhraseEvolutionParams? Evolution { get; init; }
       
       /// <summary>End bar (exclusive).</summary>
       public int EndBar => StartBar + BarCount;
   }
   
   public sealed record DrumPhraseEvolutionParams
   {
       /// <summary>Add ghost notes (0.0 = none, 1.0 = maximum).</summary>
       public double GhostIntensity { get; init; } = 0.0;
       
       /// <summary>Add hat variations (0.0 = none, 1.0 = maximum).</summary>
       public double HatVariation { get; init; } = 0.0;
       
       /// <summary>Simplify pattern (0.0 = no change, 1.0 = maximum simplification).</summary>
       public double Simplification { get; init; } = 0.0;
       
       /// <summary>Random variation intensity (0.0 = exact copy, 1.0 = maximum variation).</summary>
       public double RandomVariation { get; init; } = 0.0;
   }
   
   public sealed class DrumPhrasePlacementPlan
   {
       public List<DrumPhrasePlacement> Placements { get; } = new();
       
       /// <summary>Bars explicitly reserved for fills (not covered by phrases).</summary>
       public HashSet<int> FillBars { get; } = new();
       
       /// <summary>Checks if a bar is covered by a phrase.</summary>
       public bool IsBarCovered(int bar) => 
           Placements.Any(p => bar >= p.StartBar && bar < p.EndBar);
       
       /// <summary>Checks if a bar is a fill bar.</summary>
       public bool IsFillBar(int bar) => FillBars.Contains(bar);
       
       /// <summary>Gets the placement covering a specific bar, or null.</summary>
       public DrumPhrasePlacement? GetPlacementForBar(int bar) =>
           Placements.FirstOrDefault(p => bar >= p.StartBar && bar < p.EndBar);
   }
   ```

2. **Create `DrumGenerator.cs`**:
   ```csharp
   // AI: purpose=Generates complete drum track by placing and evolving phrases from MaterialBank.
   // AI: invariants=Requires at least one phrase in MaterialBank; output covers all non-fill bars.
   // AI: deps=MaterialBank for phrase storage; DrumPhrasePlacementPlanner for placement decisions.
   // AI: change=Story 3.1: Initial skeleton with simple phrase repetition.
   
   namespace Music.Generator.Agents.Drums;
   
   public sealed class DrumGenerator
   {
       private readonly MaterialBank _materialBank;
       
       public DrumGenerator(MaterialBank materialBank)
       {
           _materialBank = materialBank ?? throw new ArgumentNullException(nameof(materialBank));
       }
       
       /// <summary>
       /// Generates a drum track by placing phrases from MaterialBank.
       /// </summary>
       /// <param name="songContext">Song context with section and timing data.</param>
       /// <param name="genre">Genre to filter phrases.</param>
       /// <param name="maxBars">Maximum bars to generate (0 = all).</param>
       /// <returns>Generated drum PartTrack.</returns>
       public PartTrack Generate(SongContext songContext, string genre, int maxBars = 0)
       {
           ValidateSongContext(songContext);
           
           var phrases = _materialBank.GetDrumPhrasesByGenre(genre);
           if (phrases.Count == 0)
               throw new InvalidOperationException($"No phrases in MaterialBank for genre '{genre}'");
           
           int totalBars = songContext.SectionTrack.TotalBars;
           if (maxBars > 0 && maxBars < totalBars)
               totalBars = maxBars;
           
           // Create placement plan (Story 3.2 will enhance this)
           var plan = CreateSimplePlacementPlan(phrases, songContext.SectionTrack, totalBars);
           
           // Generate track from placements
           return GenerateFromPlan(plan, songContext.BarTrack);
       }
       
       private DrumPhrasePlacementPlan CreateSimplePlacementPlan(
           IReadOnlyList<DrumPhrase> phrases,
           SectionTrack sectionTrack,
           int totalBars)
       {
           var plan = new DrumPhrasePlacementPlan();
           
           // Simple strategy: use first phrase, repeat throughout
           // Story 3.2 will implement section-aware selection
           var phrase = phrases[0];
           
           int currentBar = 1;
           while (currentBar <= totalBars)
           {
               int barsRemaining = totalBars - currentBar + 1;
               int placementBars = Math.Min(phrase.BarCount, barsRemaining);
               
               plan.Placements.Add(new DrumPhrasePlacement
               {
                   PhraseId = phrase.PhraseId,
                   StartBar = currentBar,
                   BarCount = placementBars
               });
               
               currentBar += placementBars;
           }
           
           return plan;
       }
       
       private PartTrack GenerateFromPlan(DrumPhrasePlacementPlan plan, BarTrack barTrack)
       {
           var allEvents = new List<PartTrackEvent>();
           
           foreach (var placement in plan.Placements)
           {
               var phrase = _materialBank.GetDrumPhraseById(placement.PhraseId);
               if (phrase == null)
                   continue;
               
               // Convert phrase to events at placement location
               var phraseTrack = phrase.ToPartTrack(barTrack, placement.StartBar);
               
               // Filter events to only include bars within this placement
               // (handles partial placements at song end)
               var placementEndTick = GetTickForBar(barTrack, placement.EndBar);
               var filteredEvents = phraseTrack.PartTrackNoteEvents
                   .Where(e => e.AbsoluteTimeTicks < placementEndTick)
                   .ToList();
               
               allEvents.AddRange(filteredEvents);
           }
           
           // Sort and create final track
           allEvents = allEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();
           
           return new PartTrack
           {
               MidiProgramName = "Drums (Phrase-Based)",
               MidiProgramNumber = 255,
               PartTrackNoteEvents = allEvents
           };
       }
       
       private static long GetTickForBar(BarTrack barTrack, int barNumber)
       {
           // Use BarTrack's bar-to-tick conversion
           return barTrack.GetTickForBar(barNumber);
       }
       
       private static void ValidateSongContext(SongContext songContext)
       {
           ArgumentNullException.ThrowIfNull(songContext);
           if (songContext.SectionTrack == null || songContext.SectionTrack.Sections.Count == 0)
               throw new ArgumentException("SectionTrack must have sections");
           if (songContext.BarTrack == null)
               throw new ArgumentException("BarTrack must be provided");
       }
   }
   ```

3. **Add entry point in Generator.cs**:
   ```csharp
   /// <summary>
   /// Generates a drum track using phrase-based placement from MaterialBank.
   /// </summary>
   public static PartTrack GenerateFromPhrases(
       SongContext songContext, 
       string genre,
       int maxBars = 0)
   {
       ValidateSongContext(songContext);
       ValidateSectionTrack(songContext.SectionTrack);
       
       var materialBank = songContext.MaterialBank 
           ?? throw new ArgumentException("MaterialBank must be provided in SongContext");
       
       var generator = new DrumGenerator(materialBank);
       return generator.Generate(songContext, genre, maxBars);
   }
   ```

**Acceptance Criteria:**
- [ ] `DrumGenerator` class created with `Generate` method
- [ ] `DrumPhrasePlacementPlan` and related types created
- [ ] Simple placement repeats single phrase throughout song
- [ ] Events correctly offset to placement start bars
- [ ] Partial phrases at song end handled correctly
- [ ] Entry point added to `Generator.cs`

---

#### Story 3.2: Add "Generate Drums (Phrases)" Command to WriterForm

**Size:** Small (1-2 hours)

**Goal:** Add a command to generate drums using the new phrase-based generator.

**Files to Create:**
- `Writer/WriterForm/HandleCommandGenerateDrumsFromPhrases.cs`

**Files to Modify:**
- `Writer/WriterForm/WriterForm.cs` — add case for command
- `Writer/WriterForm/WriterForm.Designer.cs` — add to cbCommand items

**Implementation Steps:**

1. **Add "Generate Drums (Phrases)" to cbCommand items**

2. **Create `HandleCommandGenerateDrumsFromPhrases.cs`**:
   ```csharp
   public static class HandleCommandGenerateDrumsFromPhrases
   {
       public static void Execute(SongContext songContext, DataGridView dgSong)
       {
           // 1. Validate songContext and MaterialBank
           if (songContext?.MaterialBank == null)
           {
               ShowError("MaterialBank not initialized. Save some phrases first.");
               return;
           }
           
           var phrases = songContext.MaterialBank.GetDrumPhrases();
           if (phrases.Count == 0)
           {
               ShowError("No phrases in MaterialBank. Generate and save phrases first.");
               return;
           }
           
           // 2. Show dialog for genre selection and parameters
           // (Use existing TestSettingsDialog or create simple dialog)
           
           // 3. Generate using DrumGenerator
           var result = Generator.Generator.GenerateFromPhrases(
               songContext, 
               genre: "PopRock",  // TODO: from dialog
               maxBars: 0);       // Full song
           
           // 4. Add to grid
           result.MidiProgramName = "Drums (Phrase-Based)";
           songContext.Song.PartTracks.Add(result);
           SongGridManager.AddNewPartTrack(result, dgSong);
           
           ShowSuccess("Generated drum track from phrases");
       }
   }
   ```

3. **Update WriterForm.cs btnExecute_Click**:
   ```csharp
   case "Generate Drums (Phrases)":
       HandleCommandGenerateDrumsFromPhrases.Execute(_songContext, dgSong);
       break;
   ```

**Acceptance Criteria:**
- [ ] "Generate Drums (Phrases)" in command dropdown
- [ ] Validates MaterialBank has phrases before generating
- [ ] Generates drum track using phrase placement
- [ ] Track appears in grid and can be played
- [ ] Works with one or more phrases in bank

**Testing Workflow:**
1. Set test scenario D1
2. Generate 2-3 phrases (Story 1.2)
3. Save phrases to bank (Story 2.1)
4. Select "Generate Drums (Phrases)"
5. Click Execute
6. Play generated track — should hear phrase repeated

---

#### Story 3.3: Section-Aware Phrase Selection

**Size:** Medium (2-3 hours)

**Goal:** Enhance phrase placement to select different phrases for different section types.

**Files to Create:**
- `Generator/Agents/Drums/DrumPhrasePlacementPlanner.cs`

**Files to Modify:**
- `Generator/Agents/Drums/DrumGenerator.cs` — use planner instead of simple placement

**Implementation Steps:**

1. **Create `DrumPhrasePlacementPlanner.cs`**:
   ```csharp
   // AI: purpose=Plans phrase placement across song sections with section-appropriate selection.
   // AI: invariants=Each section gets a phrase assignment; same section type reuses same phrase.
   // AI: deps=MaterialBank for phrase queries; SectionTrack for song structure.
   
   namespace Music.Generator.Agents.Drums;
   
   public sealed class DrumPhrasePlacementPlanner
   {
       private readonly MaterialBank _materialBank;
       private readonly int _seed;
       
       public DrumPhrasePlacementPlanner(MaterialBank materialBank, int seed)
       {
           _materialBank = materialBank;
           _seed = seed;
       }
       
       /// <summary>
       /// Creates a phrase placement plan for the song.
       /// </summary>
       public DrumPhrasePlacementPlan CreatePlan(
           SectionTrack sectionTrack,
           string genre,
           int maxBars = 0)
       {
           var plan = new DrumPhrasePlacementPlan();
           var phrases = _materialBank.GetDrumPhrasesByGenre(genre);
           
           if (phrases.Count == 0)
               throw new InvalidOperationException($"No phrases for genre '{genre}'");
           
           int totalBars = maxBars > 0 
               ? Math.Min(maxBars, sectionTrack.TotalBars)
               : sectionTrack.TotalBars;
           
           // Assign phrases to section types (deterministic based on seed)
           var sectionPhraseMap = AssignPhrasesToSectionTypes(phrases, sectionTrack);
           
           // Place phrases section by section
           foreach (var section in sectionTrack.Sections)
           {
               if (section.StartBar > totalBars)
                   break;
                   
               PlacePhrasesInSection(plan, section, sectionPhraseMap, totalBars);
           }
           
           return plan;
       }
       
       private Dictionary<MusicConstants.eSectionType, DrumPhrase> AssignPhrasesToSectionTypes(
           IReadOnlyList<DrumPhrase> phrases,
           SectionTrack sectionTrack)
       {
           var map = new Dictionary<MusicConstants.eSectionType, DrumPhrase>();
           var rng = new Random(_seed);
           
           // Get unique section types in song
           var sectionTypes = sectionTrack.Sections
               .Select(s => s.SectionType)
               .Distinct()
               .ToList();
           
           foreach (var sectionType in sectionTypes)
           {
               // Select phrase for this section type (deterministic)
               // Prefer phrases with matching tags, fall back to any
               var matchingPhrases = phrases
                   .Where(p => p.Tags.Contains(sectionType.ToString(), StringComparer.OrdinalIgnoreCase))
                   .ToList();
               
               if (matchingPhrases.Count > 0)
               {
                   map[sectionType] = matchingPhrases[rng.Next(matchingPhrases.Count)];
               }
               else
               {
                   // Assign based on index for variety
                   int index = rng.Next(phrases.Count);
                   map[sectionType] = phrases[index];
               }
           }
           
           return map;
       }
       
       private void PlacePhrasesInSection(
           DrumPhrasePlacementPlan plan,
           Section section,
           Dictionary<MusicConstants.eSectionType, DrumPhrase> phraseMap,
           int maxBar)
       {
           if (!phraseMap.TryGetValue(section.SectionType, out var phrase))
               return;
           
           int sectionStart = section.StartBar;
           int sectionEnd = Math.Min(section.StartBar + section.BarCount - 1, maxBar);
           
           int currentBar = sectionStart;
           while (currentBar <= sectionEnd)
           {
               int barsRemaining = sectionEnd - currentBar + 1;
               int placementBars = Math.Min(phrase.BarCount, barsRemaining);
               
               plan.Placements.Add(new DrumPhrasePlacement
               {
                   PhraseId = phrase.PhraseId,
                   StartBar = currentBar,
                   BarCount = placementBars
               });
               
               currentBar += placementBars;
           }
       }
   }
   ```

2. **Update `DrumGenerator.cs`**:
   ```csharp
   // Replace CreateSimplePlacementPlan with:
   public PartTrack Generate(SongContext songContext, string genre, int seed = 0, int maxBars = 0)
   {
       ValidateSongContext(songContext);
       
       var phrases = _materialBank.GetDrumPhrasesByGenre(genre);
       if (phrases.Count == 0)
           throw new InvalidOperationException($"No phrases for genre '{genre}'");
       
       int effectiveSeed = seed > 0 ? seed : Random.Shared.Next(1, 100000);
       
       var planner = new DrumPhrasePlacementPlanner(_materialBank, effectiveSeed);
       var plan = planner.CreatePlan(songContext.SectionTrack, genre, maxBars);
       
       return GenerateFromPlan(plan, songContext.BarTrack);
   }
   ```

**Acceptance Criteria:**
- [ ] `DrumPhrasePlacementPlanner` created
- [ ] Same section type uses same phrase throughout song
- [ ] Different section types can use different phrases
- [ ] Phrase selection is deterministic based on seed
- [ ] Tags influence phrase selection (verse phrase for verse, etc.)
- [ ] Works with fewer phrases than section types (reuses phrases)

**Test Cases:**
1. Single phrase → repeated everywhere
2. Two phrases, verse/chorus sections → each gets its own phrase
3. Phrase tagged "verse" → preferentially used for verse sections
4. Same seed → same placement; different seed → different placement

---

### Phase 4: Phrase Evolution

---

#### Story 4.1: Create DrumPhraseEvolver

**Size:** Medium (3-4 hours)

**Goal:** Create a component that applies bounded evolution to phrases for variation.

**Files to Create:**
- `Generator/Agents/Drums/DrumPhraseEvolver.cs`

**Implementation Steps:**

1. **Create `DrumPhraseEvolver.cs`**:
   ```csharp
   // AI: purpose=Applies bounded evolution to drum phrases for purposeful variation.
   // AI: invariants=Evolution preserves phrase identity; changes are deterministic and bounded.
   // AI: deps=Uses existing drum operators for variation; respects physicality constraints.
   
   namespace Music.Generator.Agents.Drums;
   
   public sealed class DrumPhraseEvolver
   {
       private readonly int _seed;
       
       public DrumPhraseEvolver(int seed)
       {
           _seed = seed;
       }
       
       /// <summary>
       /// Evolves a phrase according to evolution parameters.
       /// </summary>
       public DrumPhrase Evolve(
           DrumPhrase original,
           DrumPhraseEvolutionParams evolution,
           BarTrack barTrack)
       {
           if (evolution == null || IsNoEvolution(evolution))
               return original;
           
           var events = original.Events.ToList();
           var rng = new Random(_seed ^ original.PhraseId.GetHashCode());
           
           // Apply evolution operators in order
           if (evolution.Simplification > 0)
               events = ApplySimplification(events, evolution.Simplification, rng);
           
           if (evolution.GhostIntensity > 0)
               events = AddGhostNotes(events, evolution.GhostIntensity, barTrack, rng);
           
           if (evolution.HatVariation > 0)
               events = ApplyHatVariation(events, evolution.HatVariation, rng);
           
           if (evolution.RandomVariation > 0)
               events = ApplyRandomVariation(events, evolution.RandomVariation, rng);
           
           // Sort and create evolved phrase
           events = events.OrderBy(e => e.AbsoluteTimeTicks).ToList();
           
           return original with
           {
               PhraseId = $"{original.PhraseId}_ev{_seed % 1000}",
               Name = $"{original.Name} (evolved)",
               Events = events
           };
       }
       
       private static bool IsNoEvolution(DrumPhraseEvolutionParams p) =>
           p.GhostIntensity == 0 && 
           p.HatVariation == 0 && 
           p.Simplification == 0 && 
           p.RandomVariation == 0;
       
       private List<PartTrackEvent> ApplySimplification(
           List<PartTrackEvent> events, 
           double intensity, 
           Random rng)
       {
           // Remove some non-essential hits (ghosts, some hats)
           // Higher intensity = more removal
           var ghostNotes = new HashSet<int> { 38 }; // Snare ghost notes
           var hatNotes = new HashSet<int> { 42, 44, 46 }; // Hi-hats
           
           return events
               .Where(e =>
               {
                   // Always keep kick and main snare
                   if (e.NoteNumber == 36) return true; // Kick
                   if (e.NoteNumber == 38 && e.NoteOnVelocity > 80) return true; // Main snare hits
                   
                   // Probabilistically remove based on intensity
                   return rng.NextDouble() > intensity * 0.5;
               })
               .ToList();
       }
       
       private List<PartTrackEvent> AddGhostNotes(
           List<PartTrackEvent> events,
           double intensity,
           BarTrack barTrack,
           Random rng)
       {
           var result = new List<PartTrackEvent>(events);
           
           // Find snare hits and potentially add ghosts before them
           var snareHits = events
               .Where(e => e.NoteNumber == 38 && e.NoteOnVelocity > 80)
               .ToList();
           
           foreach (var snare in snareHits)
           {
               if (rng.NextDouble() < intensity * 0.3)
               {
                   // Add ghost note ~60 ticks before
                   long ghostTick = snare.AbsoluteTimeTicks - 60;
                   if (ghostTick > 0)
                   {
                       result.Add(new PartTrackEvent
                       {
                           AbsoluteTimeTicks = ghostTick,
                           Type = PartTrackEventType.Note,
                           NoteNumber = 38,
                           NoteDurationTicks = 30,
                           NoteOnVelocity = 40 + rng.Next(20) // Ghost velocity
                       });
                   }
               }
           }
           
           return result;
       }
       
       private List<PartTrackEvent> ApplyHatVariation(
           List<PartTrackEvent> events, 
           double intensity, 
           Random rng)
       {
           return events
               .Select(e =>
               {
                   // Randomly open some closed hats
                   if (e.NoteNumber == 42 && rng.NextDouble() < intensity * 0.2)
                   {
                       return e with { NoteNumber = 46 }; // Open hat
                   }
                   return e;
               })
               .ToList();
       }
       
       private List<PartTrackEvent> ApplyRandomVariation(
           List<PartTrackEvent> events,
           double intensity,
           Random rng)
       {
           return events
               .Select(e =>
               {
                   // Small velocity variations
                   int velocityDelta = (int)(intensity * 15 * (rng.NextDouble() - 0.5));
                   int newVelocity = Math.Clamp(e.NoteOnVelocity + velocityDelta, 1, 127);
                   
                   // Small timing variations (only for non-essential hits)
                   long timingDelta = 0;
                   if (e.NoteNumber != 36 && e.NoteNumber != 38) // Not kick or snare
                   {
                       timingDelta = (long)(intensity * 10 * (rng.NextDouble() - 0.5));
                   }
                   
                   return e with
                   {
                       NoteOnVelocity = newVelocity,
                       AbsoluteTimeTicks = Math.Max(0, e.AbsoluteTimeTicks + timingDelta)
                   };
               })
               .ToList();
       }
   }
   ```

**Acceptance Criteria:**
- [ ] `DrumPhraseEvolver` created with all evolution methods
- [ ] Simplification removes non-essential hits progressively
- [ ] Ghost intensity adds ghost notes appropriately
- [ ] Hat variation opens some closed hats
- [ ] Random variation adds small velocity/timing changes
- [ ] Evolution is deterministic (same seed → same result)
- [ ] Original phrase is not modified (immutable)

---

#### Story 4.2: Integrate Evolution into Phrase Placement

**Size:** Small (1-2 hours)

**Goal:** Apply evolution to phrase placements based on position and section.

**Files to Modify:**
- `Generator/Agents/Drums/DrumPhrasePlacementPlanner.cs` — add evolution params
- `Generator/Agents/Drums/DrumGenerator.cs` — apply evolution during generation

**Implementation Steps:**

1. **Update `DrumPhrasePlacementPlanner.CreatePlan`**:
   ```csharp
   private void PlacePhrasesInSection(...)
   {
       // ... existing placement code ...
       
       // Add evolution for repeated placements within section
       int placementIndex = 0;
       while (currentBar <= sectionEnd)
       {
           // First placement: no evolution
           // Subsequent placements: progressive evolution
           var evolution = placementIndex == 0 
               ? null 
               : CreateEvolutionForRepeat(placementIndex, section.SectionType);
           
           plan.Placements.Add(new DrumPhrasePlacement
           {
               PhraseId = phrase.PhraseId,
               StartBar = currentBar,
               BarCount = placementBars,
               EvolutionLevel = placementIndex,
               Evolution = evolution
           });
           
           currentBar += placementBars;
           placementIndex++;
       }
   }
   
   private DrumPhraseEvolutionParams? CreateEvolutionForRepeat(int repeatIndex, eSectionType sectionType)
   {
       // Progressive evolution: later repeats have more variation
       double baseVariation = Math.Min(repeatIndex * 0.1, 0.3);
       
       return new DrumPhraseEvolutionParams
       {
           RandomVariation = baseVariation,
           GhostIntensity = sectionType == eSectionType.Chorus ? baseVariation * 0.5 : 0,
           HatVariation = baseVariation * 0.3
       };
   }
   ```

2. **Update `DrumGenerator.GenerateFromPlan`**:
   ```csharp
   private PartTrack GenerateFromPlan(DrumPhrasePlacementPlan plan, BarTrack barTrack, int seed)
   {
       var allEvents = new List<PartTrackEvent>();
       var evolver = new DrumPhraseEvolver(seed);
       
       foreach (var placement in plan.Placements)
       {
           var phrase = _materialBank.GetDrumPhraseById(placement.PhraseId);
           if (phrase == null)
               continue;
           
           // Apply evolution if specified
           if (placement.Evolution != null)
           {
               phrase = evolver.Evolve(phrase, placement.Evolution, barTrack);
           }
           
           // Convert phrase to events at placement location
           var phraseTrack = phrase.ToPartTrack(barTrack, placement.StartBar);
           
           // ... rest of existing code ...
       }
       
       // ... rest of existing code ...
   }
   ```

**Acceptance Criteria:**
- [ ] First phrase placement uses original phrase
- [ ] Subsequent placements apply progressive evolution
- [ ] Evolution intensity increases with repeat index
- [ ] Section type influences evolution (chorus gets more ghosts)
- [ ] Generated track sounds similar but not identical across repetitions

---

### Phase 5: Fill Bar Handling

---

#### Story 5.1: Reserve Fill Bars in Placement Plan

**Size:** Small (1-2 hours)

**Goal:** Mark bars at section boundaries as fill bars (to be filled later).

**Files to Modify:**
- `Generator/Agents/Drums/DrumPhrasePlacementPlanner.cs` — add fill bar detection

**Implementation Steps:**

1. **Update placement planning to reserve fill bars**:
   ```csharp
   public DrumPhrasePlacementPlan CreatePlan(...)
   {
       var plan = new DrumPhrasePlacementPlan();
       
       // ... existing code ...
       
       // Identify fill bars (last bar of each section, except final section)
       for (int i = 0; i < sectionTrack.Sections.Count - 1; i++)
       {
           var section = sectionTrack.Sections[i];
           int fillBar = section.StartBar + section.BarCount - 1;
           
           if (fillBar <= totalBars)
           {
               plan.FillBars.Add(fillBar);
           }
       }
       
       // Place phrases avoiding fill bars
       foreach (var section in sectionTrack.Sections)
       {
           PlacePhrasesInSection(plan, section, sectionPhraseMap, totalBars);
       }
       
       return plan;
   }
   
   private void PlacePhrasesInSection(...)
   {
       // ... modified to skip fill bars ...
       while (currentBar <= sectionEnd)
       {
           if (plan.IsFillBar(currentBar))
           {
               currentBar++; // Skip fill bar
               continue;
           }
           
           // ... rest of placement code ...
       }
   }
   ```

**Acceptance Criteria:**
- [ ] Fill bars identified at section boundaries
- [ ] Phrase placements do not cover fill bars
- [ ] Fill bars are empty in generated track (placeholder for fills)
- [ ] `plan.FillBars` correctly populated

---

#### Story 5.2: Simple Fill Placeholder

**Size:** Small (1-2 hours)

**Goal:** Add minimal drum activity to fill bars as a placeholder.

**Files to Create:**
- `Generator/Agents/Drums/DrumFillPlaceholder.cs`

**Files to Modify:**
- `Generator/Agents/Drums/DrumGenerator.cs` — add fill placeholder after phrase generation

**Implementation Steps:**

1. **Create `DrumFillPlaceholder.cs`**:
   ```csharp
   // AI: purpose=Generates simple placeholder fills for fill bars until full fill library is implemented.
   // AI: invariants=Fills are simple and musically safe; one fill pattern per bar.
   
   namespace Music.Generator.Agents.Drums;
   
   public static class DrumFillPlaceholder
   {
       /// <summary>
       /// Generates a simple placeholder fill for a single bar.
       /// </summary>
       public static List<PartTrackEvent> GenerateFill(
           BarTrack barTrack,
           int barNumber,
           int seed)
       {
           var events = new List<PartTrackEvent>();
           var rng = new Random(seed ^ barNumber);
           
           long barStartTick = barTrack.GetTickForBar(barNumber);
           int ticksPerBeat = MusicConstants.TicksPerQuarterNote;
           
           // Simple fill: snare hits building to crash
           // Beat 3: snare
           // Beat 3.5: snare
           // Beat 4: snare
           // Beat 4.5: snare + crash on next bar handled by phrase
           
           var fillPattern = new[] { 3.0m, 3.5m, 4.0m, 4.5m };
           
           foreach (var beat in fillPattern)
           {
               long tick = barStartTick + (long)((beat - 1) * ticksPerBeat);
               
               events.Add(new PartTrackEvent
               {
                   AbsoluteTimeTicks = tick,
                   Type = PartTrackEventType.Note,
                   NoteNumber = 38, // Snare
                   NoteDurationTicks = 60,
                   NoteOnVelocity = 90 + rng.Next(20)
               });
           }
           
           // Keep kick on beat 3
           events.Add(new PartTrackEvent
           {
               AbsoluteTimeTicks = barStartTick + (long)(2 * ticksPerBeat),
               Type = PartTrackEventType.Note,
               NoteNumber = 36, // Kick
               NoteDurationTicks = 60,
               NoteOnVelocity = 100
           });
           
           return events;
       }
   }
   ```

2. **Update `DrumGenerator.GenerateFromPlan`**:
   ```csharp
   private PartTrack GenerateFromPlan(DrumPhrasePlacementPlan plan, BarTrack barTrack, int seed)
   {
       var allEvents = new List<PartTrackEvent>();
       
       // ... existing phrase placement code ...
       
       // Add placeholder fills
       foreach (var fillBar in plan.FillBars)
       {
           var fillEvents = DrumFillPlaceholder.GenerateFill(barTrack, fillBar, seed);
           allEvents.AddRange(fillEvents);
       }
       
       // Sort and create final track
       allEvents = allEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();
       
       // ... rest of existing code ...
   }
   ```

**Acceptance Criteria:**
- [ ] Fill bars have simple snare build pattern
- [ ] Fill pattern is deterministic based on seed
- [ ] Fills don't overlap with phrase content
- [ ] Generated track has fills at section boundaries

**Notes:**
- This is intentionally simple; full fill library is a future epic
- Placeholder provides audible section transitions

---

## Testing Strategy

### Manual Testing Workflow

Each phase has a clear testing path:

**Phase 1:**
1. Set test scenario D1
2. Execute "Generate Phrase" with 4 bars, seed 12345
3. Play phrase — hear 4 bars of drums
4. Repeat with different seed — hear different phrase
5. Same seed — hear identical phrase ✓

**Phase 2:**
1. Generate 2-3 phrases with different seeds
2. Select each phrase, save to bank with appropriate names/tags
3. View Material Bank — verify phrases stored
4. Clear one, verify it's gone ✓

**Phase 3:**
1. Ensure bank has 2+ phrases
2. Execute "Generate Drums (Phrases)"
3. Play full track — hear phrases repeated appropriately
4. Verify section boundaries get correct phrase ✓

**Phase 4:**
1. Generate drum track from phrases
2. Listen — hear subtle variations on repeats
3. Verify identity preserved but not robotic ✓

**Phase 5:**
1. Generate drum track from phrases
2. Listen at section boundaries — hear simple fills
3. Verify fills don't overlap phrases ✓

### Unit Test Coverage

| Story | Tests |
|-------|-------|
| 1.1 | Existing tests pass after rename |
| 1.2 | Dialog validation, determinism |
| 1.3 | DrumPhrase creation, MaterialBank storage |
| 1.4 | ToPartTrack tick offset calculations |
| 2.1 | Save workflow, phrase extraction |
| 2.2 | Viewer displays correct data |
| 3.1 | Simple placement, event merging |
| 3.2 | Command validation, integration |
| 3.3 | Section-aware selection, determinism |
| 4.1 | Each evolution operator, determinism |
| 4.2 | Evolution integration, progressive increase |
| 5.1 | Fill bar identification, avoidance |
| 5.2 | Fill generation, non-overlap |

---

## Dependencies

```
Story 1.1 (rename) — no dependencies
Story 1.2 (generate phrase command) — depends on 1.1
Story 1.3 (DrumPhrase type) — no dependencies
Story 1.4 (ToPartTrack) — depends on 1.3
Story 2.1 (save to bank) — depends on 1.2, 1.3, 1.4
Story 2.2 (bank viewer) — depends on 1.3
Story 3.1 (DrumGenerator skeleton) — depends on 1.3, 1.4
Story 3.2 (generate drums command) — depends on 3.1
Story 3.3 (section-aware selection) — depends on 3.1
Story 4.1 (evolver) — depends on 1.3
Story 4.2 (evolution integration) — depends on 3.3, 4.1
Story 5.1 (fill bar reservation) — depends on 3.3
Story 5.2 (fill placeholder) — depends on 5.1
```

**Recommended Execution Order:**
1. Story 1.1 (rename)
2. Story 1.3, 1.4 (DrumPhrase type)
3. Story 1.2 (generate phrase command)
4. Story 2.1 (save to bank)
5. Story 2.2 (bank viewer)
6. Story 3.1 (generator skeleton)
7. Story 3.2 (generate command)
8. Story 3.3 (section-aware)
9. Story 4.1 (evolver)
10. Story 4.2 (evolution integration)
11. Story 5.1 (fill bars)
12. Story 5.2 (fill placeholder)

---

## Future Work (Not In This Epic)

- **Fill Library Import:** Import fills from JSON/CSV data files
- **Energy-Based Phrase Selection:** Choose phrases based on energy arc
- **Phrase Learning:** Extract phrases from imported MIDI files
- **Multi-Genre Support:** Add Jazz, Metal, Funk style phrases
- **Advanced Evolution:** More sophisticated evolution operators
- **Fill Placement Intelligence:** Context-aware fill selection

---

## Success Metrics

After implementing this epic:

1. **Different seed = Different sound:** Changing seed produces recognizably different drum tracks, not just different randomness
2. **Pattern recognition:** Listeners can identify repeated phrases
3. **Purposeful variation:** Variations feel intentional, not random
4. **Section identity:** Different sections can have different rhythmic character
5. **Musical coherence:** Track sounds like one drummer playing, not random events

---

*Epic Created:* Based on analysis of current drum generator limitations and human drummer behavior patterns.

*Last Updated:* Ready for implementation
