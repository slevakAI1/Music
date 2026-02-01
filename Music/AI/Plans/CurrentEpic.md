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
  User → Select phrases in grid → "Save to Bank" button → MaterialBank (stores as MaterialPhrase)

Phase 3: Track Generation
  SongContext + MaterialBank → DrumGenerator → (phrase placement + evolution) → PartTrack
```

**NOTE:** Keep MVP contracts stable so later layers (coordination/performance shaping/evolution) are additive.

**Contract:** Phrase=material (`MaterialPhrase`), Plan=intent (`DrumPhrasePlacementPlan`), Renderer=realization (track assembly from plan).

### Key Types

| Type | Location | Purpose |
|------|----------|---------|
| `MaterialPhrase` | `Song/Material/` | Phrase data model (extends MotifSpec or new record) |
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

#### Story 1.2: Add "Generate Phrase" Command to WriterForm (Completed)

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

3. **Update WriterForm.cs** (in `btnExecute_Click`):

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

#### Story 1.3: Create MaterialPhrase Material Type

**Size:** Small (1-2 hours)

**Goal:** Create a data model for drum phrases that can be stored in `MaterialBank`.

**Files to Create:**
- `Song/Material/MaterialPhrase.cs`

**Files to Modify:**
- `Song/Material/MaterialKind.cs` — add `MaterialPhrase` enum value
- `Song/Material/MaterialBank.cs` — add phrase-specific query methods

**Implementation Steps:**

1. **Add `MaterialPhrase` to `MaterialKind` enum**:
   
2. **Create `MaterialPhrase.cs`**:
   
3. **Add to MaterialBank.cs**:
   
**Acceptance Criteria:**
- [ ] `DrumPhrase` record created with all required properties
- [ ] `MaterialKind.DrumPhrase` enum value added
- [ ] `MaterialBank` has add/get/query methods for drum phrases
- [ ] `FromPartTrack` factory method works correctly
- [ ] Unit tests for MaterialPhrase creation and MaterialBank storage

---

#### Story 1.4: Implement DrumPhrase.ToPartTrack for Phrase Placement

**Size:** Small (1-2 hours)

**Goal:** Implement the `ToPartTrack` method that converts a `DrumPhrase` to a `PartTrack` at a specified start bar.

**Files to Modify:**
- `Song/Material/DrumPhrase.cs` — implement `ToPartTrack`

**Implementation Steps:**

1. **Implement `ToPartTrack`**:

2. **Add helper if needed in BarTrack**:

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

4. **Update WriterForm.cs**:

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

#### Story 3.1: Create DrumGenerator Skeleton with Phrase Placement (Completed)

**Size:** Medium (3-4 hours) (Completed)

**Goal:** Create a new `DrumGenerator` class that generates drum tracks by placing phrases from the MaterialBank.

**Files to Create:**
- `Generator/Agents/Drums/DrumGenerator.cs`
- `Generator/Agents/Drums/DrumPhrasePlacementPlan.cs`

**Files to Modify:**
- `Generator/Core/Generator.cs` — add new entry point for phrase-based generation

**Implementation Steps:**

1. **Create `DrumPhrasePlacementPlan.cs`**:

2. **Create `DrumGenerator.cs`**:

3. **Add entry point in Generator.cs**:

**Acceptance Criteria:**
- [x] `DrumGenerator` class created with `Generate` method
- [x] `DrumPhrasePlacementPlan` and related types created
- [x] Simple placement repeats single phrase throughout song
- [x] Events correctly offset to placement start bars
- [x] Partial phrases at song end handled correctly
- [x] Entry point added to `Generator.cs`

---

#### Story 3.2: Add "Generate Drums (Phrases)" Command to WriterForm (Completed)

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

3. **Update WriterForm.cs btnExecute_Click**:

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

#### Story 3.3: Section-Aware Phrase Selection (Completed)

**Size:** Medium (2-3 hours)

**Goal:** Enhance phrase placement to select different phrases for different section types.

**Files to Create:**
- `Generator/Agents/Drums/DrumPhrasePlacementPlanner.cs`

**Files to Modify:**
- `Generator/Agents/Drums/DrumGenerator.cs` — use planner instead of simple placement

**Implementation Steps:**

1. **Create `DrumPhrasePlacementPlanner.cs`**:

2. **Update `DrumGenerator.cs`**:

**Acceptance Criteria:**
- [x] `DrumPhrasePlacementPlanner` created
- [x] Same section type uses same phrase throughout song
- [x] Different section types can use different phrases
- [x] Phrase selection is deterministic based on seed
- [x] Tags influence phrase selection (verse phrase for verse, etc.)
- [x] Works with fewer phrases than section types (reuses phrases)

**Test Cases:**
1. Single phrase → repeated everywhere
2. Two phrases, verse/chorus sections → each gets its own phrase
3. Phrase tagged "verse" → preferentially used for verse sections
4. Same seed → same placement; different seed → different placement

---

### Phase 4: Phrase Evolution

---

#### Story 4.1: Create DrumPhraseEvolver (Completed)

**Size:** Medium (3-4 hours)

**Goal:** Create a component that applies bounded evolution to phrases for variation.

**Contract note (do not expand scope):**
- Evolver operates on phrase event data only.
- Placement remains in `DrumPhrasePlacementPlan`.
- Rendering remains in `DrumGenerator` (assemble `PartTrack` from plan + phrase material).

**Files to Create:**
- `Generator/Agents/Drums/DrumPhraseEvolver.cs`

**Implementation Steps:**

1. **Create `DrumPhraseEvolver.cs`**:

**Acceptance Criteria:**
- [x] `DrumPhraseEvolver` created with all evolution methods
- [x] Simplification removes non-essential hits progressively
- [x] Ghost intensity adds ghost notes appropriately
- [x] Hat variation opens some closed hats
- [x] Random variation adds small velocity/timing changes
- [x] Evolution is deterministic (same seed → same result)
- [x] Original phrase is not modified (immutable)

---

#### Story 4.1X (Small): Phrase start offset / pickup support (Completed)

**Why:** Some phrases start before beat 1 (pickup) and must be placed earlier to preserve cadence.

**Goal:** Support phrase material with a non-zero start offset (e.g., last 1/8 of prior bar) during placement/rendering.

**Design constraint:** Do not add extra future-layer hooks; only add minimal metadata needed for correct placement.

**Acceptance Criteria:**
- [x] Phrase material can declare a start offset within its first bar (ticks from bar start)
- [x] Renderer uses this offset when converting phrase events to `PartTrack` at a target bar
- [x] Placement can target a bar boundary while phrase audio starts before that boundary when offset < 0
- [x] Determinism preserved: same phrase + same placement → identical absolute ticks

---

#### Story 4.2: Integrate Evolution into Phrase Placement (Completed)

**Size:** Small (1-2 hours)

**Goal:** Apply evolution to phrase placements based on position and section.

**Files to Modify:**
- `Generator/Agents/Drums/DrumPhrasePlacementPlanner.cs` — add evolution params
- `Generator/Agents/Drums/DrumGenerator.cs` — apply evolution during generation

**Implementation Steps:**

1. **Update `DrumPhrasePlacementPlanner.CreatePlan`**:

2. **Update `DrumGenerator.GenerateFromPlan`**:

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

2. **Update `DrumGenerator.GenerateFromPlan`**:

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
| 1.3 | MaterialPhrase creation, MaterialBank storage |
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
Story 1.3 (MaterialPhrase type) — no dependencies
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
2. Story 1.3, 1.4 (MaterialPhrase type)
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
