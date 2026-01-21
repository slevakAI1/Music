# Story 8.3 Testing Guide

## Running the Tests

Story 8.3 tests are self-contained and can be executed directly:

```csharp
// In any test harness or main entry point:
Music.Song.Material.Tests.MotifLibraryTests.RunAll();
```

## What is Tested

### Determinism Tests
- `ClassicRockHookA()` produces consistent results (same name, role, rhythm count, contour)
- `SteadyVerseRiffA()` produces consistent results
- `BrightSynthHookA()` produces consistent results
- `BassTransitionFillA()` produces consistent results
- Note: MotifId will be different each call (expected), but all other fields are deterministic

### Structure Validation Tests
- Each motif has non-empty name
- Each motif has non-empty intended role
- Each motif has valid rhythm shape (count > 0, all ticks >= 0)
- Each motif has valid register (CenterMidiNote in [21..108], RangeSemitones in [1..24])
- Each motif has valid tone policy (ChordToneBias in [0..1])
- Each motif has at least one intent tag

### MaterialBank Storage Tests
- Each motif can be converted to PartTrack
- Each motif can be stored in MaterialBank
- Each motif can be retrieved from MaterialBank by MotifId
- Retrieved motif preserves name and other properties

### Motif Characteristics Tests
- `ClassicRockHookA`: Name="Classic Rock Hook A", Role="Lead", Kind=Hook, Contour=Arch, has "chorus-hook" tag
- `SteadyVerseRiffA`: Name="Steady Verse Riff A", Role="Guitar", Kind=Riff, Contour=Flat, has "verse-riff" tag
- `BrightSynthHookA`: Name="Bright Synth Hook A", Role="Keys", Kind=Hook, Contour=Up, has "synth-hook" tag
- `BassTransitionFillA`: Name="Bass Transition Fill A", Role="Bass", Kind=BassFill, Contour=Up

### Collection Tests
- `GetAllTestMotifs()` returns all 4 motifs (or more if additional ones added)
- All motifs returned by `GetAllTestMotifs()` pass basic validation (non-null, valid fields)
- Specific motifs can be found by name in the collection

## Expected Output

```
=== Story 8.3: MotifLibrary Tests ===

  ✓ ClassicRockHookA is deterministic
  ✓ SteadyVerseRiffA is deterministic
  ✓ BrightSynthHookA is deterministic
  ✓ BassTransitionFillA is deterministic
  ✓ ClassicRockHookA has valid structure
  ✓ SteadyVerseRiffA has valid structure
  ✓ BrightSynthHookA has valid structure
  ✓ BassTransitionFillA has valid structure
  ✓ ClassicRockHookA can be stored in MaterialBank
  ✓ SteadyVerseRiffA can be stored in MaterialBank
  ✓ BrightSynthHookA can be stored in MaterialBank
  ✓ BassTransitionFillA can be stored in MaterialBank
  ✓ ClassicRockHookA has correct characteristics
  ✓ SteadyVerseRiffA has correct characteristics
  ✓ BrightSynthHookA has correct characteristics
  ✓ BassTransitionFillA has correct characteristics
  ✓ GetAllTestMotifs returns all motifs
  ✓ All motifs from GetAllTestMotifs are valid

✓ All Story 8.3 MotifLibrary tests passed!
```

## Motif Details

### ClassicRockHookA
- **Purpose**: Chorus hook for high-energy sections
- **Role**: Lead
- **Rhythm Pattern**: Syncopated "da-da DUM" pattern over 2 bars
  - Onsets at: Beat 1, Beat 1.5, Beat 2, Beat 4 (bar 2), Beat 4.5 (bar 2), Downbeat (bar 2)
- **Contour**: Arch (rises then falls)
- **Register**: G4 (MIDI 67) ± 10 semitones
- **Tone Policy**: 80% chord tone bias, passing tones allowed
- **Tags**: "hooky", "chorus-hook", "energetic"
- **Inspiration**: Classic rock hook archetypes (non-derivative)

### SteadyVerseRiffA
- **Purpose**: Verse riff for mid-energy foundation
- **Role**: Guitar
- **Rhythm Pattern**: Steady eighth-note pattern with slight variation (1 bar)
  - Onsets at: Beats 1, 1.5, 2, 2+ (16th pickup), 2.5, 3, 4, 4.5
- **Contour**: Flat (mostly horizontal)
- **Register**: G3 (MIDI 55) ± 7 semitones (lower register)
- **Tone Policy**: 85% chord tone bias, no passing tones (tight to harmony)
- **Tags**: "verse-riff", "steady", "foundation"
- **Inspiration**: Steady verse riff archetypes

### BrightSynthHookA
- **Purpose**: Bright synth hook for high-energy sections
- **Role**: Keys
- **Rhythm Pattern**: Quick ascending pattern with syncopation (1 bar)
  - Onsets at: Beat 1, 16th after 1, Beat 1.5, Beat 2, Beat 2.5, Beat 3, Beat 4
- **Contour**: Up (ascending)
- **Register**: C5 (MIDI 72) ± 12 semitones (bright upper register)
- **Tone Policy**: 75% chord tone bias, passing tones allowed
- **Tags**: "bright", "synth-hook", "energetic", "ascending"

### BassTransitionFillA
- **Purpose**: Short bass fill for section transitions
- **Role**: Bass
- **Rhythm Pattern**: Approach pattern with 16th note run at end (1 bar)
  - Onsets at: Beats 1, 2, 4, then 16th note run
- **Contour**: Up (ascending approach)
- **Register**: G2 (MIDI 43) ± 12 semitones (bass register)
- **Tone Policy**: 90% chord tone bias, passing tones allowed on run
- **Tags**: "bass-fill", "transition", "approach"

## Notes

- All motifs use placeholder pitches (MIDI 60) when converted to PartTrack
- Stage 9 renderer will replace placeholder pitches with actual harmony-aware notes
- All rhythm ticks are at 480 PPQN (MusicConstants.TicksPerQuarterNote)
- Motifs are stored in MaterialLocal domain (ticks from 0)
- Non-derivative rule: patterns are archetype-level, not transcriptions of recognizable riffs
