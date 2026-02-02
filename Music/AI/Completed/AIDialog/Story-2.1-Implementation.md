# Story 2.1 Implementation Summary

**Story:** Add ToPartTrack Method to GrooveInstanceLayer

**Status:** ✅ COMPLETED

---

## Changes Made

### Modified Files

#### `Music\Generator\Groove\GrooveInstanceLayer.cs`
Added MIDI conversion method to transform groove patterns into playable drum tracks.

**New Method:**
```csharp
public PartTrack ToPartTrack(BarTrack barTrack, int totalBars, int velocity = 100)
```

**Helper Method:**
```csharp
private static PartTrackEvent CreateDrumEvent(
    BarTrack barTrack, 
    int barNumber, 
    decimal beat, 
    int midiNote, 
    int velocity)
```

**Key Features:**
1. **GM MIDI drum note mapping:**
   - Kick → 36 (Bass Drum 1)
   - Snare → 38 (Acoustic Snare)
   - ClosedHat → 42 (Closed Hi-Hat)

2. **Bar repetition** — Repeats groove pattern across specified number of bars

3. **Fixed velocity** — All events use same velocity (no shaping applied)

4. **Event sorting** — All events sorted by AbsoluteTimeTicks for proper playback

5. **Standard kit setup** — PartTrack configured with "Standard Kit" (program 0)

### Created Files

#### `Music.Tests\Generator\Groove\GrooveInstanceLayerToPartTrackTests.cs`
Comprehensive test suite with 24 tests covering:

- **Basic Tests (7 tests)**
  - Null barTrack handling (throws `ArgumentNullException`)
  - Invalid bar count handling (≤0 throws exception)
  - Invalid velocity handling (<1 or >127 throws exception)
  - Returns valid PartTrack
  - Sets "Standard Kit" properties

- **MIDI Note Mapping (3 tests)**
  - Kick onsets → note 36
  - Snare onsets → note 38
  - Hat onsets → note 42

- **Timing Conversion (2 tests)**
  - Beat 1 starts at bar start tick
  - Multiple beats converted correctly with increasing ticks

- **Velocity Tests (2 tests)**
  - Default velocity = 100
  - Custom velocity applied to all events

- **Multi-Bar Tests (2 tests)**
  - Pattern repeats across bars
  - Events timing increases across bars

- **Event Sorting (1 test)**
  - Events sorted by AbsoluteTimeTicks

- **Empty/Minimal Groove (2 tests)**
  - Empty groove returns empty track
  - Single role groove only produces that role's events

- **Integration Tests (3 tests)**
  - Full groove produces correct event count
  - Integration with Generate facade works
  - Different seeds produce different tracks

- **Event Properties (2 tests)**
  - Duration fixed at 120 ticks
  - Type set to NoteOn

---

## Test Results

✅ **All 24 tests passed**

```
Test summary: total: 24, failed: 0, succeeded: 24, skipped: 0
```

---

## Implementation Details

### MIDI Note Mapping

| Role | MIDI Note | GM Instrument |
|------|-----------|---------------|
| Kick | 36 | Bass Drum 1 (Standard Kit) |
| Snare | 38 | Acoustic Snare |
| ClosedHat | 42 | Closed Hi-Hat |
| OpenHat | 46 | Open Hi-Hat (not yet used) |

**Note:** Currently only Kick, Snare, and ClosedHat are mapped. OpenHat constant exists but HatOnsets use ClosedHat MIDI note.

### Timing Conversion

The method uses `BarTrack.ToTick(barNumber, beat)` to convert bar+beat positions to absolute MIDI ticks:

```
Bar 1, Beat 1.0 → Tick 0
Bar 1, Beat 2.0 → Tick 480 (1 quarter note @ 480 TPQ)
Bar 1, Beat 3.0 → Tick 960
Bar 2, Beat 1.0 → Tick 1920 (start of bar 2)
```

### Design Decisions

1. **Fixed duration (120 ticks)** — Short drum hit, approximately 1/4 of a quarter note
2. **No velocity shaping** — All hits same velocity (Part Generator's job to shape)
3. **Simple role mapping** — Only drum roles converted (Bass, Comp, Pads ignored)
4. **Event sorting** — Ensures proper MIDI playback order
5. **Bar repetition** — Groove pattern loops for audition purposes

### Algorithm

```
For each bar from 1 to totalBars:
    For each kick onset:
        Create event at bar+beat → tick (note 36)
    For each snare onset:
        Create event at bar+beat → tick (note 38)
    For each hat onset:
        Create event at bar+beat → tick (note 42)

Sort all events by AbsoluteTimeTicks
Return PartTrack with sorted events
```

### Performance Characteristics

- **Time complexity:** O(n × m) where n = bars, m = onsets per bar
- **Space complexity:** O(n × m) for event list
- **Typical load:** 
  - 4 bars × 12 onsets/bar = 48 events
  - 8 bars × 12 onsets/bar = 96 events

---

## Acceptance Criteria Status

- ✅ Add method `ToPartTrack(BarTrack, int, int)` to `GrooveInstanceLayer.cs`
- ✅ Maps roles to GM MIDI drum notes:
  - ✅ "Kick" → 36
  - ✅ "Snare" → 38
  - ✅ "ClosedHat" → 42
  - ⏸️ "OpenHat" → 46 (constant exists, not yet used in this story)
- ✅ For each bar 1..totalBars, for each role, for each onset beat:
  - ✅ Calculate `AbsoluteTimeTicks` from bar + beat using `barTrack`
  - ✅ Create `PartTrackEvent` with fixed velocity
- ✅ Events sorted by `AbsoluteTimeTicks`
- ✅ Unit tests: conversion produces correct MIDI events

---

## Example Usage

### Basic Conversion
```csharp
// Create groove from factory
GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);

// Setup bar track
Timingtrack timing = TimingTests.CreateTestTrackD1();
BarTrack barTrack = new();
barTrack.RebuildFromTimingTrack(timing, 8);

// Convert to PartTrack
PartTrack drumTrack = groove.ToPartTrack(barTrack, 8, velocity: 100);

// Result:
// - 8 bars of drum pattern
// - All hits at velocity 100
// - Standard Kit (program 0)
// - Events sorted by time
```

### Custom Velocity
```csharp
// Softer drums
PartTrack softDrums = groove.ToPartTrack(barTrack, 4, velocity: 60);

// Louder drums
PartTrack loudDrums = groove.ToPartTrack(barTrack, 4, velocity: 110);
```

### Different Seed Audition
```csharp
// Generate multiple variations for comparison
for (int seed = 1; seed <= 10; seed++)
{
    GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", seed);
    PartTrack track = groove.ToPartTrack(barTrack, 8);
    
    Console.WriteLine($"Seed {seed}: {track.PartTrackNoteEvents.Count} events");
    // Export to MIDI or play back for audition
}
```

---

## Integration Points

### Current Usage (Story 2.1)
```
GrooveInstanceLayer → ToPartTrack(barTrack, bars) → PartTrack (playable MIDI)
```

### Future Usage (Story 2.2)
```
Generator.GenerateGroovePreview(seed, genre, barTrack, bars, velocity)
    ↓
GrooveAnchorFactory.Generate(genre, seed)
    ↓
groove.ToPartTrack(barTrack, bars, velocity)
    ↓
Return PartTrack
```

### Future Usage (Story 3.1 - UI)
```
User enters seed in dialog
    ↓
Generator.GenerateGroovePreview(seed, genre, barTrack, bars)
    ↓
Load PartTrack into song grid
    ↓
Playback for audition
```

---

## Limitations & Future Enhancements

### Current Limitations

1. **No OpenHat differentiation** — All hats use ClosedHat MIDI note
2. **Fixed duration** — All drums 120 ticks (could vary by role)
3. **No articulations** — No support for rim shots, flams, etc.
4. **Drums only** — Bass, Comp, Pads roles not converted

### Future Enhancements

#### Easy Additions

1. **OpenHat support:**
   ```csharp
   // Alternate between closed/open based on position
   if (beat % 1.0m == 0.5m) // Offbeats
       midiNote = 46; // OpenHat
   else
       midiNote = 42; // ClosedHat
   ```

2. **Role-specific durations:**
   ```csharp
   int duration = role switch
   {
       36 => 120,  // Kick - short
       38 => 100,  // Snare - shorter
       42 => 80,   // Hat - shortest
       _ => 120
   };
   ```

3. **Support all roles:**
   ```csharp
   // Add bass, comp, pads conversion
   foreach (decimal beat in BassOnsets)
       events.Add(CreateEvent(bar, beat, GetBassMidiNote(), velocity));
   ```

4. **Articulation support:**
   ```csharp
   public PartTrack ToPartTrack(
       BarTrack barTrack, 
       int totalBars, 
       int velocity = 100,
       Dictionary<string, DrumArticulation>? articulations = null)
   ```

---

## Notes

### MIDI Note Constants

The MIDI note numbers are hardcoded in the `ToPartTrack` method for simplicity. They match the constants defined in `DrumTrackGenerator.cs`:
- `KickMidiNote = 36`
- `SnareMidiNote = 38`
- `ClosedHatMidiNote = 42`
- `OpenHatMidiNote = 46`

These follow the General MIDI Level 1 Percussion Key Map (Standard Kit).

### Duration Choice

The 120-tick duration was chosen as a reasonable short drum hit:
- At 480 TPQ (ticks per quarter note), 120 ticks = 1/4 of a quarter note
- At 120 BPM, this equals approximately 62ms
- Provides clear articulation without overlap

### BarTrack Dependency

The method requires a properly initialized `BarTrack` built from a `Timingtrack`. This ensures accurate tick conversion for any time signature. The `BarTrack.ToTick()` method handles the conversion internally.

---

## Files Modified

1. `Music\Generator\Groove\GrooveInstanceLayer.cs` — Added ToPartTrack method

## Files Created

1. `Music.Tests\Generator\Groove\GrooveInstanceLayerToPartTrackTests.cs` — Test suite

---

**Implementation Date:** 2025-01-27  
**Build Status:** ✅ Successful  
**All Tests:** ✅ Passing (24/24)  
**Story Phase:** Phase 2 (Groove to PartTrack Conversion)  
**Next Story:** 2.2 — Add GenerateGroovePreview to Generator.cs
