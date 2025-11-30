# MIDI Implementation Session - Chord and Timing Support

## Session Overview
This document captures the complete development session for implementing chord expansion and proper MIDI timing in a music composition application.

---

## Prompt 1: Initial Chord Support Request

**User:**
> Update #class:'Music.Writer.PhrasesToTimedNotesConverter':285-4210 to properly handle generating the chord notes. Currently it is not correct. It needs to use [Music.Writer.ChordConverter](#music.writer.chordconverter-context) to generate the individual notes based on the Phrase chord-related properties. It should continue to handle the "single note" NoteEvents as well. Chords will be process to attribute time to the first note in the chord while the rest of the chord notes will be simultanous with the first chord note.
>
> Limit scope to just this request.
>
> See #class:'Music.Writer.AppendNotes':342-12187 for code example.

**Assistant Response:**

I'll update the `PhrasesToTimedNotesConverter` class to properly handle chord generation using the `ChordConverter` class, similar to how `AppendNotes` does it.

```csharp Writer\Transforms\PhrasesToTimedNotesConverter.cs
using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts Phrase objects to flat lists of TimedNote objects.
    /// Sequential notes have positive delta times, while simultaneous notes (chords) have delta = 0.
    /// </summary>
    public static class PhrasesToTimedNotesConverter
    {
        // ... (Convert method implementation)
        
        private static List<TimedNote> ConvertSinglePhrase(Phrase phrase, short ticksPerQuarterNote)
        {
            var timedNotes = new List<TimedNote>();
            long currentTime = 0;

            foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
            {
                if (noteEvent.IsRest)
                {
                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
                    currentTime += duration;
                    continue;
                }

                // Check if this is a chord that needs to be expanded
                if (!string.IsNullOrWhiteSpace(noteEvent.ChordKey) && 
                    noteEvent.ChordDegree.HasValue && 
                    !string.IsNullOrWhiteSpace(noteEvent.ChordQuality) && 
                    !string.IsNullOrWhiteSpace(noteEvent.ChordBase))
                {
                    // Use ChordConverter to generate chord notes
                    var chordNotes = ChordConverter.Convert(
                        noteEvent.ChordKey,
                        noteEvent.ChordDegree.Value,
                        noteEvent.ChordQuality,
                        noteEvent.ChordBase,
                        baseOctave: noteEvent.Octave,
                        noteValue: noteEvent.Duration);

                    // Apply dots and tuplet settings to chord notes
                    foreach (var cn in chordNotes)
                    {
                        cn.Dots = noteEvent.Dots;
                        if (!string.IsNullOrWhiteSpace(noteEvent.TupletNumber))
                        {
                            cn.TupletNumber = noteEvent.TupletNumber;
                            cn.TupletActualNotes = noteEvent.TupletActualNotes;
                            cn.TupletNormalNotes = noteEvent.TupletNormalNotes;
                        }
                    }

                    // Convert chord notes to TimedNotes
                    for (int i = 0; i < chordNotes.Count; i++)
                    {
                        var cn = chordNotes[i];
                        var duration = CalculateDuration(cn, ticksPerQuarterNote);
                        var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);

                        timedNotes.Add(new TimedNote
                        {
                            Delta = i == 0 ? currentTime : 0,
                            NoteNumber = (byte)noteNumber,
                            Duration = duration,
                            Velocity = 100,
                            IsRest = false
                        });
                    }

                    var chordDuration = CalculateDuration(chordNotes[0], ticksPerQuarterNote);
                    currentTime = chordDuration;
                }
                else
                {
                    // Handle single note
                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
                    var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);

                    timedNotes.Add(new TimedNote
                    {
                        Delta = currentTime,
                        NoteNumber = (byte)noteNumber,
                        Duration = duration,
                        Velocity = 100,
                        IsRest = false
                    });

                    currentTime = duration;
                }
            }

            return timedNotes;
        }
    }
}
```

### Changes Made:
1. **Chord Detection**: Added logic to detect when a `NoteEvent` represents a chord
2. **ChordConverter Integration**: Calls `ChordConverter.Convert()` to generate individual notes
3. **Chord Note Properties**: Applied dots and tuplet settings to all generated chord notes
4. **Time Attribution**: First note gets accumulated `currentTime` delta, subsequent notes get delta of 0
5. **Single Note Handling**: Preserved existing logic for non-chord notes

---

## Prompt 2: Merge by Instrument Implementation

**User:**
> implement step 2 only. create this logic as a new method in [Music.Writer.PhrasesToTimedNotesConverter](#music.writer.phrasestotimednotesconverter-context).
>
> Step 2 - Merge timed notes lists that are for the same instrument, i.e. it has phrases for multiple staves
> Pseudo-code:
> 1. Get list of unique instruments
> 2. Loop through each unique instrument
>    - Get timed note lists for this instrument
>    - Merge the timed note lists for this instrument into one timed note list that assumes the lists will play simultaneously
>    - Output the merged list
> 3. Return the composite List<List<TimedNote>> containing the merged data

**Assistant Response:**

I'll implement Step 2 as a new method in the `PhrasesToTimedNotesConverter` class.

```csharp Writer\Transforms\PhrasesToTimedNotesConverter.cs
public static Dictionary<byte, List<TimedNote>> MergeByInstrument(
    List<Phrase> phrases, 
    List<List<TimedNote>> timedNoteLists)
{
    if (phrases == null)
        throw new ArgumentNullException(nameof(phrases));
    if (timedNoteLists == null)
        throw new ArgumentNullException(nameof(timedNoteLists));
    if (phrases.Count != timedNoteLists.Count)
        throw new ArgumentException("Phrases and timedNoteLists must have the same count.");

    var result = new Dictionary<byte, List<TimedNote>>();

    // Group phrases by their MIDI program number (instrument)
    var groupedByInstrument = new Dictionary<byte, List<List<TimedNote>>>();

    for (int i = 0; i < phrases.Count; i++)
    {
        var instrument = phrases[i].MidiProgramNumber;
        
        if (!groupedByInstrument.ContainsKey(instrument))
        {
            groupedByInstrument[instrument] = new List<List<TimedNote>>();
        }
        
        groupedByInstrument[instrument].Add(timedNoteLists[i]);
    }

    // Merge timed note lists for each instrument
    foreach (var kvp in groupedByInstrument)
    {
        var instrument = kvp.Key;
        var listsToMerge = kvp.Value;

        if (listsToMerge.Count == 1)
        {
            result[instrument] = listsToMerge[0];
        }
        else
        {
            result[instrument] = MergeSimultaneousTimedNoteLists(listsToMerge);
        }
    }

    return result;
}

private static List<TimedNote> MergeSimultaneousTimedNoteLists(List<List<TimedNote>> lists)
{
    var merged = new List<TimedNote>();
    var currentPositions = new int[lists.Count];
    var currentTimes = new long[lists.Count];

    bool hasMoreNotes = true;

    while (hasMoreNotes)
    {
        hasMoreNotes = false;
        long minTime = long.MaxValue;

        // Find the minimum time across all lists
        for (int i = 0; i < lists.Count; i++)
        {
            if (currentPositions[i] < lists[i].Count)
            {
                hasMoreNotes = true;
                if (currentTimes[i] < minTime)
                {
                    minTime = currentTimes[i];
                }
            }
        }

        if (!hasMoreNotes)
            break;

        long deltaFromLastNote = merged.Count == 0 ? minTime : minTime - GetLastAbsoluteTime(merged);
        bool isFirstNoteAtThisTime = true;

        // Add all notes that occur at minTime
        for (int i = 0; i < lists.Count; i++)
        {
            while (currentPositions[i] < lists[i].Count && currentTimes[i] == minTime)
            {
                var note = lists[i][currentPositions[i]];
                
                var mergedNote = new TimedNote
                {
                    NoteNumber = note.NoteNumber,
                    Duration = note.Duration,
                    Velocity = note.Velocity,
                    IsRest = note.IsRest,
                    Delta = isFirstNoteAtThisTime ? deltaFromLastNote : 0
                };

                merged.Add(mergedNote);
                isFirstNoteAtThisTime = false;

                currentPositions[i]++;
                
                if (currentPositions[i] < lists[i].Count)
                {
                    currentTimes[i] += lists[i][currentPositions[i]].Delta;
                }
            }
        }
    }

    return merged;
}

private static long GetLastAbsoluteTime(List<TimedNote> notes)
{
    long time = 0;
    foreach (var note in notes)
    {
        time += note.Delta;
    }
    return time;
}
```

---

## Prompt 3: Update PhrasesToMidiConverter

**User:**
> Update PhrasesToMidiConverter.Convert() to accept mergedByInstrument as the input. The mergedByInstrument is in a midi-friendly format, one list per track. Do not maintain backward compatibility. Limit scope to this request.
>
> also update: `var midiDoc = PhrasesToMidiConverter.Convert(phrases);` to do the call.

**Assistant Response:**

I'll update both files to accept the `mergedByInstrument` dictionary and update the call in `WriterFormEvents.cs`.

```csharp Writer\Transforms\PhrasesToMidiConverter.cs
public static class PhrasesToMidiConverter
{
    public static MidiSongDocument Convert(Dictionary<byte, List<TimedNote>> mergedByInstrument)
    {
        if (mergedByInstrument == null)
            throw new ArgumentNullException(nameof(mergedByInstrument));

        var midiFile = new MidiFile();
        var ticksPerQuarterNote = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision
            ?? new TicksPerQuarterNoteTimeDivision(480)).TicksPerQuarterNote;

        // Global tempo / time signature track (track 0)
        var tempoTrack = new TrackChunk();
        var microsecondsPerQuarterNote = 60_000_000 / 112;
        tempoTrack.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote) { DeltaTime = 0 });
        tempoTrack.Events.Add(new TimeSignatureEvent(4, 4) { DeltaTime = 0 });
        tempoTrack.Events.Add(CreateEndOfTrackEvent());
        midiFile.Chunks.Add(tempoTrack);

        int trackNumber = 1;
        int channelCursor = 0;

        foreach (var kvp in mergedByInstrument)
        {
            byte programNumber = kvp.Key;
            var timedNotes = kvp.Value;

            bool isDrumSet = IsDrumSet(programNumber);
            
            int channel;
            if (isDrumSet)
            {
                channel = 9;
            }
            else
            {
                if (channelCursor == 9) channelCursor++;
                channel = channelCursor;
                channelCursor = (channelCursor + 1) % 16;
            }
            
            var trackChunk = CreateTrackFromTimedNotes(timedNotes, programNumber, trackNumber, channel, isDrumSet);
            midiFile.Chunks.Add(trackChunk);
            trackNumber++;
        }

        return new MidiSongDocument(midiFile);
    }

    private static TrackChunk CreateTrackFromTimedNotes(
        List<TimedNote> timedNotes,
        byte programNumber,
        int trackNumber,
        int channel,
        bool isDrumSet)
    {
        var trackChunk = new TrackChunk();

        var instrumentName = GetInstrumentName(programNumber);
        var trackName = $"{instrumentName} - Track {trackNumber}";
        trackChunk.Events.Add(new SequenceTrackNameEvent(trackName) { DeltaTime = 0 });

        if (!isDrumSet)
        {
            trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)programNumber)
            {
                Channel = (FourBitNumber)channel,
                DeltaTime = 0
            });
        }

        foreach (var timedNote in timedNotes)
        {
            if (timedNote.IsRest)
                continue;

            trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)timedNote.NoteNumber, (SevenBitNumber)timedNote.Velocity)
            {
                Channel = (FourBitNumber)channel,
                DeltaTime = timedNote.Delta
            });

            trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)timedNote.NoteNumber, (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)channel,
                DeltaTime = timedNote.Duration
            });
        }

        trackChunk.Events.Add(CreateEndOfTrackEvent());
        return trackChunk;
    }
}
```

```csharp Writer\Form\WriterFormEvents.cs
// Step 3 - Convert merged timed notes to MIDI document
var midiDoc = PhrasesToMidiConverter.Convert(mergedByInstrument);
```

### Changes Made:
- Removed old overloads for backward compatibility
- `Convert` now works directly with `Dictionary<byte, List<TimedNote>>`
- Simplified track creation using pre-calculated `TimedNote` objects
- Updated call in `WriterFormEvents.cs` to pass `mergedByInstrument`

---

## Prompt 4: Create TimedPhrase Class

**User:**
> timedPhrases needs it's own class definition. This new class must include The instrument name and midi programnumber as they are needed later, populate from the Phrase objects.
>
> Only update PhrasesToTimedNotesConverter and one the call to it.

**Assistant Response:**

I'll create a new `TimedPhrase` class to encapsulate the timed notes along with instrument information.

```csharp Writer\Transforms\PhrasesToTimedNotesConverter.cs
public sealed class TimedPhrase
{
    public byte MidiProgramNumber { get; set; }
    public string MidiPartName { get; set; } = string.Empty;
    public List<TimedNote> TimedNotes { get; set; } = new();
}

public static class PhrasesToTimedNotesConverter
{
    public static List<TimedPhrase> Convert(List<Phrase> phrases, short ticksPerQuarterNote = 480)
    {
        if (phrases == null)
            throw new ArgumentNullException(nameof(phrases));

        if (phrases.Count == 0)
            return new List<TimedPhrase>();

        var result = new List<TimedPhrase>();

        foreach (var phrase in phrases)
        {
            result.Add(new TimedPhrase
            {
                MidiProgramNumber = phrase.MidiProgramNumber,
                MidiPartName = phrase.MidiPartName ?? string.Empty,
                TimedNotes = ConvertSinglePhrase(phrase, ticksPerQuarterNote)
            });
        }

        return result;
    }

    public static Dictionary<byte, List<TimedNote>> MergeByInstrument(List<TimedPhrase> timedPhrases)
    {
        if (timedPhrases == null)
            throw new ArgumentNullException(nameof(timedPhrases));

        var result = new Dictionary<byte, List<TimedNote>>();
        var groupedByInstrument = new Dictionary<byte, List<List<TimedNote>>>();

        foreach (var timedPhrase in timedPhrases)
        {
            var instrument = timedPhrase.MidiProgramNumber;
            
            if (!groupedByInstrument.ContainsKey(instrument))
            {
                groupedByInstrument[instrument] = new List<List<TimedNote>>();
            }
            
            groupedByInstrument[instrument].Add(timedPhrase.TimedNotes);
        }

        foreach (var kvp in groupedByInstrument)
        {
            var instrument = kvp.Key;
            var listsToMerge = kvp.Value;

            if (listsToMerge.Count == 1)
            {
                result[instrument] = listsToMerge[0];
            }
            else
            {
                result[instrument] = MergeSimultaneousTimedNoteLists(listsToMerge);
            }
        }

        return result;
    }
}
```

```csharp Writer\Form\WriterFormEvents.cs
var timedPhrases = PhrasesToTimedNotesConverter.Convert(phrases);
var mergedByInstrument = PhrasesToTimedNotesConverter.MergeByInstrument(timedPhrases);
```

---

## Prompt 5: Add Tempo and Time Signature Parameters

**User:**
> better, but not correct. (1) I want the static values for the time signature and tempo to be passing in on the calling line signature, not hardcoded in the convert method. (2) I see code: `int tempo = 120`. Not sure why there is another tempo value being set other than the one that will get passed in on the call to this method.
>
> Update as needed

**Assistant Response:**

I'll update the code to remove the default parameter values and ensure the tempo and time signature are passed explicitly from the calling code.

```csharp Writer\Transforms\PhrasesToMidiConverter.cs
public static MidiSongDocument Convert(
    Dictionary<byte, List<TimedNote>> mergedByInstrument,
    int tempo,
    int timeSignatureNumerator,
    int timeSignatureDenominator,
    short ticksPerQuarterNote = 480)
{
    if (mergedByInstrument == null)
        throw new ArgumentNullException(nameof(mergedByInstrument));
    if (tempo <= 0)
        throw new ArgumentException("Tempo must be greater than 0", nameof(tempo));
    if (timeSignatureNumerator <= 0)
        throw new ArgumentException("Time signature numerator must be greater than 0", nameof(timeSignatureNumerator));
    if (timeSignatureDenominator <= 0)
        throw new ArgumentException("Time signature denominator must be greater than 0", nameof(timeSignatureDenominator));

    var midiFile = new MidiFile
    {
        TimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerQuarterNote)
    };

    var tempoTrack = new TrackChunk();
    
    var microsecondsPerQuarterNote = 60_000_000 / tempo;
    tempoTrack.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote) { DeltaTime = 0 });
    
    byte denominatorPower = (byte)Math.Log2(timeSignatureDenominator);
    tempoTrack.Events.Add(new TimeSignatureEvent(
        (byte)timeSignatureNumerator, 
        denominatorPower) 
    { 
        DeltaTime = 0 
    });
    
    tempoTrack.Events.Add(CreateEndOfTrackEvent());
    midiFile.Chunks.Add(tempoTrack);

    // ... rest of implementation
}
```

```csharp Writer\Form\WriterFormEvents.cs
var midiDoc = PhrasesToMidiConverter.Convert(
    mergedByInstrument,
    tempo: 112,
    timeSignatureNumerator: 4,
    timeSignatureDenominator: 4);
```

### Changes Made:
1. Removed default values for required parameters
2. Added parameter validation
3. Kept default for `ticksPerQuarterNote` (technical detail)
4. Removed hardcoded tempo value
5. Calling code explicitly passes tempo: 112 and 4/4 time signature

---

## Prompt 6: Fix Note Duration Issue

**User:**
> in a prior version of creating the mididocument, when it was played, the notes were much longer than with the current code. previously the first note ended around when the next started. Now it cuts off sharply and there is a small gap until the next note starts. Maybe is related to midi on and off velocities for the notes?

**Assistant Response:**

Looking at the code, I can see the issue. The problem is in how the `Delta` values are being set in `ConvertSinglePhrase`. The `Delta` represents the time to wait *before* playing the note, but `currentTime` is being set to the full duration after each note, creating gaps.

```csharp Writer\Transforms\PhrasesToTimedNotesConverter.cs
private static List<TimedNote> ConvertSinglePhrase(Phrase phrase, short ticksPerQuarterNote)
{
    var timedNotes = new List<TimedNote>();
    long currentTime = 0;

    foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
    {
        if (noteEvent.IsRest)
        {
            var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
            currentTime += duration;
            continue;
        }

        if (!string.IsNullOrWhiteSpace(noteEvent.ChordKey) && 
            noteEvent.ChordDegree.HasValue && 
            !string.IsNullOrWhiteSpace(noteEvent.ChordQuality) && 
            !string.IsNullOrWhiteSpace(noteEvent.ChordBase))
        {
            var chordNotes = ChordConverter.Convert(
                noteEvent.ChordKey,
                noteEvent.ChordDegree.Value,
                noteEvent.ChordQuality,
                noteEvent.ChordBase,
                baseOctave: noteEvent.Octave,
                noteValue: noteEvent.Duration);

            foreach (var cn in chordNotes)
            {
                cn.Dots = noteEvent.Dots;
                if (!string.IsNullOrWhiteSpace(noteEvent.TupletNumber))
                {
                    cn.TupletNumber = noteEvent.TupletNumber;
                    cn.TupletActualNotes = noteEvent.TupletActualNotes;
                    cn.TupletNormalNotes = noteEvent.TupletNormalNotes;
                }
            }

            var chordDuration = CalculateDuration(chordNotes[0], ticksPerQuarterNote);

            for (int i = 0; i < chordNotes.Count; i++)
            {
                var cn = chordNotes[i];
                var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);

                timedNotes.Add(new TimedNote
                {
                    Delta = i == 0 ? currentTime : 0,
                    NoteNumber = (byte)noteNumber,
                    Duration = chordDuration,
                    Velocity = 100,
                    IsRest = false
                });
            }

            // Reset currentTime to 0 after assigning it
            currentTime = 0;
        }
        else
        {
            var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
            var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);

            timedNotes.Add(new TimedNote
            {
                Delta = currentTime,
                NoteNumber = (byte)noteNumber,
                Duration = duration,
                Velocity = 100,
                IsRest = false
            });

            // Reset currentTime to 0 after assigning it
            currentTime = 0;
        }
    }

    return timedNotes;
}
```

### Key Changes:
- After assigning `currentTime` to a note's `Delta`, reset it to `0`
- For sequential notes, this creates legato playback (notes touching each other)
- The `Delta = 0` means the next note starts immediately after the previous note ends

---

## Prompt 7: Understanding MIDI Delta Time

**User:**
> question about how midi plays notes, especially simultaneous notes: What exactly is delta that midi uses? it's the difference between what two note events?

**Assistant Response:**

Great question! Understanding MIDI delta time is crucial for proper playback timing.

## What is MIDI Delta Time?

**Delta time** in MIDI represents the **time interval between consecutive MIDI events**, measured in **ticks**.

### Key Points:

1. **Delta is relative, not absolute**: Each MIDI event's delta represents how many ticks to wait **since the previous event** before executing this event.

2. **The two events**: Delta is the time difference between:
   - **Previous event** (could be a NoteOn, NoteOff, or any MIDI event)
   - **Current event** (the event carrying this delta value)

3. **Cumulative timing**: To find the absolute time of any event, you sum up all the deltas from the start of the track.

## Examples:

### Sequential Notes (one after another):
```
Event 1: NoteOn  C4, Delta=0    (starts at tick 0)
Event 2: NoteOff C4, Delta=480  (ends at tick 480)
Event 3: NoteOn  D4, Delta=0    (starts immediately at tick 480)
Event 4: NoteOff D4, Delta=480  (ends at tick 960)
```

### Simultaneous Notes (chord):
```
Event 1: NoteOn  C4, Delta=0    (starts at tick 0)
Event 2: NoteOn  E4, Delta=0    (starts at tick 0 - simultaneous)
Event 3: NoteOn  G4, Delta=0    (starts at tick 0 - simultaneous)
Event 4: NoteOff C4, Delta=480  (ends at tick 480)
Event 5: NoteOff E4, Delta=0    (ends at tick 480 - simultaneous)
Event 6: NoteOff G4, Delta=0    (ends at tick 480 - simultaneous)
```

### Your Current Issue:

What you need for 4 quarter-note chords:
```json
[
  // Chord 1 (tick 0)
  {"Delta": 0,   "NoteNumber": 60, "Duration": 480},  // C4 - starts at 0
  {"Delta": 0,   "NoteNumber": 64, "Duration": 480},  // E4 - also at 0
  {"Delta": 0,   "NoteNumber": 67, "Duration": 480},  // G4 - also at 0
  
  // Chord 2 (tick 480)
  {"Delta": 480, "NoteNumber": 60, "Duration": 480},  // Wait 480 ticks
  {"Delta": 0,   "NoteNumber": 64, "Duration": 480},  // Simultaneous
  {"Delta": 0,   "NoteNumber": 67, "Duration": 480},  // Simultaneous
  
  // Chord 3 (tick 960)
  {"Delta": 480, "NoteNumber": 60, "Duration": 480},
  {"Delta": 0,   "NoteNumber": 64, "Duration": 480},
  {"Delta": 0,   "NoteNumber": 67, "Duration": 480},
  
  // Chord 4 (tick 1440)
  {"Delta": 480, "NoteNumber": 60, "Duration": 480},
  {"Delta": 0,   "NoteNumber": 64, "Duration": 480},
  {"Delta": 0,   "NoteNumber": 67, "Duration": 480}
]
```

---

## Prompt 8: Fix Chord Delta Timing

**User:**
> in [WriterFormEvents.cs](#writerformevents.cs-context), timedphrases is not working correctly for chords. Example shows input has 4 quarter note chords (3 note chords), but output has Delta=0 for every note. They don't all start at the first beat. Non-chord note sequences seem to be working.

**Assistant Response:**

I can see the issue! The problem is that after we process a chord and set `currentTime = 0`, the **next** chord will use `currentTime = 0` for its first note's delta. But the delta should actually be the *duration of the previous chord*!

Here's the correct fix:

```csharp Writer\Transforms\PhrasesToTimedNotesConverter.cs
private static List<TimedNote> ConvertSinglePhrase(Phrase phrase, short ticksPerQuarterNote)
{
    var timedNotes = new List<TimedNote>();
    long absoluteTime = 0;  // Track absolute time position
    long lastNoteStartTime = 0;  // Track when the last note/chord started

    foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
    {
        if (noteEvent.IsRest)
        {
            var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
            absoluteTime += duration;
            continue;
        }

        if (!string.IsNullOrWhiteSpace(noteEvent.ChordKey) && 
            noteEvent.ChordDegree.HasValue && 
            !string.IsNullOrWhiteSpace(noteEvent.ChordQuality) && 
            !string.IsNullOrWhiteSpace(noteEvent.ChordBase))
        {
            var chordNotes = ChordConverter.Convert(
                noteEvent.ChordKey,
                noteEvent.ChordDegree.Value,
                noteEvent.ChordQuality,
                noteEvent.ChordBase,
                baseOctave: noteEvent.Octave,
                noteValue: noteEvent.Duration);

            foreach (var cn in chordNotes)
            {
                cn.Dots = noteEvent.Dots;
                if (!string.IsNullOrWhiteSpace(noteEvent.TupletNumber))
                {
                    cn.TupletNumber = noteEvent.TupletNumber;
                    cn.TupletActualNotes = noteEvent.TupletActualNotes;
                    cn.TupletNormalNotes = noteEvent.TupletNormalNotes;
                }
            }

            var chordDuration = CalculateDuration(chordNotes[0], ticksPerQuarterNote);

            for (int i = 0; i < chordNotes.Count; i++)
            {
                var cn = chordNotes[i];
                var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);

                var delta = i == 0 ? (absoluteTime - lastNoteStartTime) : 0;

                timedNotes.Add(new TimedNote
                {
                    Delta = delta,
                    NoteNumber = (byte)noteNumber,
                    Duration = chordDuration,
                    Velocity = 100,
                    IsRest = false
                });
            }

            lastNoteStartTime = absoluteTime;
            absoluteTime += chordDuration;
        }
        else
        {
            var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
            var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);

            var delta = absoluteTime - lastNoteStartTime;

            timedNotes.Add(new TimedNote
            {
                Delta = delta,
                NoteNumber = (byte)noteNumber,
                Duration = duration,
                Velocity = 100,
                IsRest = false
            });

            lastNoteStartTime = absoluteTime;
            absoluteTime += duration;
        }
    }

    return timedNotes;
}
```

### Key Changes:
1. **Introduced `absoluteTime`**: Tracks absolute time position (cumulative time)
2. **Introduced `lastNoteStartTime`**: Tracks when the last note/chord started
3. **Calculate delta correctly**: `delta = absoluteTime - lastNoteStartTime`
4. **Update tracking**: After each note/chord, update both time trackers

With this fix, your 4 quarter-note chords produce:
- Chord 1: First note delta=0, others delta=0
- Chord 2: First note delta=480, others delta=0
- Chord 3: First note delta=480, others delta=0
- Chord 4: First note delta=480, others delta=0

---

## Prompt 9: Common MIDI Events Reference

**User:**
> list some other common midi events that may occur and show examples how they would look embedded in an event sequence with notes.

**Assistant Response:**

Here are some common MIDI events that can occur in a sequence, along with examples:

## Common MIDI Events

### 1. Program Change (Instrument Selection)
```
Event 1: ProgramChange Piano (0),     Delta=0
Event 2: NoteOn  C4,                   Delta=0
Event 3: NoteOff C4,                   Delta=480
Event 4: ProgramChange Violin (40),    Delta=0
Event 5: NoteOn  D4,                   Delta=0
Event 6: NoteOff D4,                   Delta=480
```

### 2. Control Change (CC messages)

**Volume change:**
```
Event 1: ControlChange Volume(7)=100,  Delta=0
Event 2: NoteOn  C4,                   Delta=0
Event 3: NoteOff C4,                   Delta=480
Event 4: ControlChange Volume(7)=50,   Delta=0
Event 5: NoteOn  D4,                   Delta=0
Event 6: NoteOff D4,                   Delta=480
```

**Sustain pedal:**
```
Event 1: NoteOn  C4,                   Delta=0
Event 2: ControlChange Sustain(64)=127, Delta=0
Event 3: NoteOff C4,                   Delta=480
Event 4: NoteOn  E4,                   Delta=0
Event 5: NoteOff E4,                   Delta=480
Event 6: ControlChange Sustain(64)=0,  Delta=0
```

**Pan (stereo position):**
```
Event 1: ControlChange Pan(10)=0,      Delta=0  (hard left)
Event 2: NoteOn  C4,                   Delta=0
Event 3: NoteOff C4,                   Delta=480
Event 4: ControlChange Pan(10)=127,    Delta=0  (hard right)
Event 5: NoteOn  D4,                   Delta=0
Event 6: NoteOff D4,                   Delta=480
```

### 3. Pitch Bend
```
Event 1: NoteOn  C4,                   Delta=0
Event 2: PitchBend 0,                  Delta=0
Event 3: PitchBend +2048,              Delta=240
Event 4: PitchBend +4096,              Delta=240
Event 5: PitchBend 0,                  Delta=240
Event 6: NoteOff C4,                   Delta=240
```

### 4. Tempo Changes (Meta Event)
```
Event 1: SetTempo 500000µs/qn (120 BPM), Delta=0
Event 2: NoteOn  C4,                     Delta=0
Event 3: NoteOff C4,                     Delta=480
Event 4: SetTempo 375000µs/qn (160 BPM), Delta=0
Event 5: NoteOn  D4,                     Delta=0
Event 6: NoteOff D4,                     Delta=480
```

### 5. Time Signature Changes (Meta Event)
```
Event 1: TimeSignature 4/4,            Delta=0
Event 2: NoteOn  C4,                   Delta=0
Event 3: NoteOff C4,                   Delta=480
Event 4: TimeSignature 3/4,            Delta=0
Event 5: NoteOn  D4,                   Delta=0
Event 6: NoteOff D4,                   Delta=360
```

### 6. Track Name / Instrument Name (Meta Event)
```
Event 1: SequenceTrackName "Piano",    Delta=0
Event 2: ProgramChange 0,              Delta=0
Event 3: NoteOn  C4,                   Delta=0
Event 4: NoteOff C4,                   Delta=480
```

### 7. Text / Lyrics / Markers (Meta Events)
```
Event 1: NoteOn  C4,                   Delta=0
Event 2: TextEvent "Verse 1",          Delta=0
Event 3: NoteOff C4,                   Delta=480
Event 4: NoteOn  D4,                   Delta=0
Event 5: LyricEvent "Hel-",            Delta=0
Event 6: NoteOff D4,                   Delta=480
Event 7: NoteOn  E4,                   Delta=0
Event 8: LyricEvent "lo",              Delta=0
Event 9: NoteOff E4,                   Delta=480
```

### 8. Aftertouch (Channel Pressure)
```
Event 1: NoteOn  C4,                   Delta=0
Event 2: ChannelPressure 80,           Delta=240
Event 3: ChannelPressure 120,          Delta=240
Event 4: ChannelPressure 0,            Delta=240
Event 5: NoteOff C4,                   Delta=240
```

### 9. System Exclusive (SysEx)
```
Event 1: SysEx [F0 43 12 00 ... F7],   Delta=0
Event 2: NoteOn  C4,                   Delta=0
Event 3: NoteOff C4,                   Delta=480
```

### 10. End of Track (Meta Event)
```
Event 1: NoteOn  C4,                   Delta=0
Event 2: NoteOff C4,                   Delta=480
Event 3: EndOfTrack,                   Delta=0
```

## Complete Realistic Example

```
// Track 0 (Tempo/Time Signature Track)
Event 1: TimeSignature 4/4,            Delta=0
Event 2: SetTempo 500000µs/qn (120BPM),Delta=0
Event 3: EndOfTrack,                   Delta=0

// Track 1 (Piano)
Event 1: SequenceTrackName "Piano",    Delta=0
Event 2: ProgramChange Piano(0),       Delta=0
Event 3: ControlChange Volume(7)=100,  Delta=0
Event 4: ControlChange Pan(10)=64,     Delta=0

// Play a C major chord with sustain
Event 5: ControlChange Sustain(64)=127,Delta=0
Event 6: NoteOn  C4, Velocity=100,     Delta=0
Event 7: NoteOn  E4, Velocity=100,     Delta=0
Event 8: NoteOn  G4, Velocity=100,     Delta=0

Event 9: NoteOff C4,                   Delta=480
Event 10: NoteOff E4,                  Delta=0
Event 11: NoteOff G4,                  Delta=0

// New chord with crescendo
Event 12: NoteOn  D4, Velocity=80,     Delta=0
Event 13: NoteOn  F4, Velocity=80,     Delta=0
Event 14: NoteOn  A4, Velocity=80,     Delta=0
Event 15: ControlChange Volume(7)=120, Delta=240

Event 16: NoteOff D4,                  Delta=240
Event 17: NoteOff F4,                  Delta=0
Event 18: NoteOff A4,                  Delta=0
Event 19: ControlChange Sustain(64)=0, Delta=0

Event 20: EndOfTrack,                  Delta=0
```

## Key Controller Numbers (CC)

- **CC 1**: Modulation Wheel
- **CC 7**: Volume
- **CC 10**: Pan (stereo position)
- **CC 11**: Expression
- **CC 64**: Sustain Pedal (0-63 = off, 64-127 = on)
- **CC 65**: Portamento On/Off
- **CC 66**: Sostenuto Pedal
- **CC 67**: Soft Pedal
- **CC 91**: Reverb Depth
- **CC 93**: Chorus Depth
- **CC 121**: Reset All Controllers
- **CC 123**: All Notes Off

---

## Summary

This session covered:
1. Implementing chord expansion using `ChordConverter`
2. Creating a merge algorithm for simultaneous playback of multiple staves
3. Refactoring the MIDI conversion pipeline
4. Creating the `TimedPhrase` class to carry instrument metadata
5. Adding explicit tempo and time signature parameters
6. Fixing delta time calculation for proper sequential and simultaneous note playback
7. Understanding MIDI delta time concepts
8. Reference documentation for common MIDI events

The final implementation provides a complete pipeline: **Phrase ? TimedPhrase ? Merged by Instrument ? MIDI Document** with proper timing, chord expansion, and metadata preservation.
