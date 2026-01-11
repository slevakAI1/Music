// AI: purpose=Generate bass track using BassPatternLibrary for pattern selection and BassChordChangeDetector for approach notes.
// AI: keep MIDI program number 33; patterns replace randomizer for more structured bass lines (Story 5.1 + 5.2); returns sorted by AbsoluteTimeTicks.

using Music.MyMidi;

namespace Music.Generator
{
    internal static class BassTrackGenerator
    {
        /// <summary>
        /// Generates bass track: pattern-based bass lines with optional approach notes to chord changes.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();
            const int bassOctave = 2;

            // Policy setting: allow approach notes (default false for strict diatonic)
            bool allowApproaches = policy.AllowNonDiatonicChordTones; // Use policy flag

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var bassOnsets = grooveEvent.AnchorLayer.BassOnsets;
                if (bassOnsets == null || bassOnsets.Count == 0)
                    continue;

                // Get current section type for pattern selection (default to Verse if none)
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse;
                // Note: SectionTrack not passed to GenerateBassTrack yet, will use default for now
                // TODO: Pass SectionTrack to enable section-aware pattern selection

                // Select bass pattern for this bar using BassPatternLibrary
                var bassPattern = BassPatternLibrary.SelectPattern(
                    grooveEvent.Name,
                    sectionType,
                    bar,
                    allowPolicyGated: allowApproaches);

                // Build onset grid for this bar
                var onsetSlots = OnsetGrid.Build(bar, bassOnsets, barTrack);

                // Get active harmony for this bar
                var currentHarmony = harmonyTrack.GetActiveHarmonyEvent(bar, 1m);
                if (currentHarmony == null)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    currentHarmony.Key,
                    currentHarmony.Degree,
                    currentHarmony.Quality,
                    currentHarmony.Bass,
                    bassOctave,
                    policy);

                // Get root MIDI note for pattern rendering
                int rootMidi = ctx.ChordMidiNotes.Count > 0 ? ctx.ChordMidiNotes[0] : 36; // Default to C2

                // Render pattern into bass hits
                var patternHits = bassPattern.Render(rootMidi, onsetSlots.Count);

                // Process each pattern hit and check for chord change opportunities
                foreach (var hit in patternHits)
                {
                    if (hit.SlotIndex < 0 || hit.SlotIndex >= onsetSlots.Count)
                        continue; // Skip invalid slot indices

                    var slot = onsetSlots[hit.SlotIndex];

                    // Check if chord change is imminent and approach should be inserted
                    bool isChangeImminent = BassChordChangeDetector.IsChangeImminent(
                        harmonyTrack,
                        slot.Bar,
                        slot.OnsetBeat,
                        currentHarmony,
                        lookaheadBeats: 2m);

                    bool shouldInsertApproach = isChangeImminent &&
                        BassChordChangeDetector.ShouldInsertApproach(
                            onsetSlots,
                            hit.SlotIndex,
                            allowApproaches);

                    int midiNote;
                    if (shouldInsertApproach)
                    {
                        // Insert approach note to next chord
                        var nextHarmony = BassChordChangeDetector.GetNextHarmonyEvent(
                            harmonyTrack,
                            slot.Bar,
                            slot.OnsetBeat);

                        if (nextHarmony != null)
                        {
                            // Build context for next chord to get target root
                            var nextCtx = HarmonyPitchContextBuilder.Build(
                                nextHarmony.Key,
                                nextHarmony.Degree,
                                nextHarmony.Quality,
                                nextHarmony.Bass,
                                bassOctave,
                                policy);

                            int targetRoot = nextCtx.ChordMidiNotes.Count > 0 ? nextCtx.ChordMidiNotes[0] : rootMidi;
                            midiNote = BassChordChangeDetector.CalculateDiatonicApproach(targetRoot, approachFromBelow: true);
                        }
                        else
                        {
                            midiNote = hit.MidiNote; // Fallback to pattern note
                        }
                    }
                    else
                    {
                        midiNote = hit.MidiNote; // Use pattern note
                    }

                    var noteStart = (int)slot.StartTick;
                    var noteDuration = slot.DurationTicks;

                    // Prevent overlap: trim previous notes of the same pitch that would extend past this note-on
                    NoteOverlapHelper.PreventOverlap(notes, midiNote, noteStart);

                    notes.Add(new PartTrackEvent(
                        noteNumber: midiNote,
                        absoluteTimeTicks: noteStart,
                        noteDurationTicks: noteDuration,
                        noteOnVelocity: 95));
                }
            }

            // Ensure events are sorted by AbsoluteTimeTicks before returning
            notes = notes.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }
    }
}
