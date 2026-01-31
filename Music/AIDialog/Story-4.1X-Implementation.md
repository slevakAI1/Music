# Story 4.1X Implementation Summary

**Story:** Phrase Start Offset / Pickup Support

**Status:** ✅ Completed

## Changes Made

- Added `StartOffsetTicks` property to `MaterialPhrase` (default 0 for backward compatibility)
- Updated `MaterialPhrase.ToPartTrack` to apply offset when calculating event positions
- Updated `MaterialPhrase.FromPartTrack` to accept optional `startOffsetTicks` parameter
- Added 7 unit tests covering offset behavior and determinism

## Implementation Details

### Minimal Design
Following the story constraint "Do not add extra future-layer hooks", the implementation:
- Adds only one property: `StartOffsetTicks` to `MaterialPhrase`
- Reuses existing `ToPartTrack` rendering logic with minimal modification
- Maintains backward compatibility (default value 0)

### How It Works
1. **Pickup phrases** can be created with negative `StartOffsetTicks` (e.g., -240 for 1/8 note before bar 1)
2. **ToPartTrack** applies this offset to the calculated tick position when rendering
3. **Placement** targets a bar boundary, but audio starts before/after based on offset
4. **Determinism** preserved: same phrase + placement → identical absolute ticks

### Example Usage
```csharp
// Create a pickup phrase that starts 1/8 note before bar 1
var pickupPhrase = MaterialPhrase.FromPartTrack(
    partTrack,
    phraseNumber: 1,
    phraseId: "pickup1",
    name: "Pickup Phrase",
    description: "Starts before bar",
    barCount: 1,
    seed: 123,
    startOffsetTicks: -240  // Negative = before bar 1
);

// Place at bar 5 - audio will start 1/8 note before bar 5
var result = pickupPhrase.ToPartTrack(barTrack, startBar: 5, midiProgramNumber: 255);
```

## Files Created

- `Music.Tests/Song/Material/MaterialPhrasePickupTests.cs`
- `Music/AIDialog/Story-4.1X-Implementation.md`

## Files Updated

- `Music/Song/Material/MaterialPhrase.cs` - Added property and updated methods
- `Music/AI/Plans/CurrentEpic.md` - Marked acceptance criteria complete

## Build Status

✅ Build successful

## Tests

7 tests created covering:
- Default value is zero
- No offset places events at target bar
- Negative offset places events before target bar (pickup)
- Positive offset places events after target bar start (delay)
- Deterministic placement with offsets
- FromPartTrack preserves offset
- FromPartTrack defaults to zero when not specified

## Design Notes

- **No new layers** - reuses existing MaterialPhrase and ToPartTrack
- **Minimal impact** - single property with default value
- **Backward compatible** - existing code works unchanged
- **Future-ready** - supports both pickup (negative) and delay (positive) offsets
