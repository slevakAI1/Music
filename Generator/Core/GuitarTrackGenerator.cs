// AI: purpose=Generate guitar/comp track using CompRhythmPatternLibrary + CompVoicingSelector for multi-note comp voicings.
// AI: keep program number 27 for Electric Guitar; tracks previousVoicing for voice-leading continuity across bars.
// AI: applies strum timing offsets to chord voicings for humanized feel (Story 4.3).

using Music.MyMidi;

namespace Music.Generator
{
    internal static class GuitarTrackGenerator
    {
        /// <summary>
        /// Generates guitar/comp track: rhythm pattern-based chord voicings with strum timing.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();
            List<int>? previousVoicing = null; // Track previous voicing for voice-leading continuity

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);
                var compOnsets = groovePreset.AnchorLayer.CompOnsets;
                if (compOnsets == null || compOnsets.Count == 0)
                    continue;

                // Get section type for pattern selection
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse; // Default
                if (sectionTrack.GetActiveSection(bar, out var section) && section != null)
                {
                    sectionType = section.SectionType;
                }

                // Get section profile for voicing selection
                SectionProfile? sectionProfile = SectionProfile.GetForSectionType(sectionType);

                // Get comp rhythm pattern for this bar
                var pattern = CompRhythmPatternLibrary.GetPattern(
                    groovePreset.Name,
                    sectionType,
                    bar);

                // Filter onset slots using pattern's included indices
                var filteredOnsets = new List<decimal>();
                for (int i = 0; i < pattern.IncludedOnsetIndices.Count; i++)
                {
                    int index = pattern.IncludedOnsetIndices[i];
                    if (index >= 0 && index < compOnsets.Count)
                    {
                        filteredOnsets.Add(compOnsets[index]);
                    }
                }

                // Skip this bar if pattern resulted in no onsets
                if (filteredOnsets.Count == 0)
                    continue;

                // Build onset grid from filtered onsets
                var onsetSlots = OnsetGrid.Build(bar, filteredOnsets, barTrack);

                foreach (var slot in onsetSlots)
                {
                    // Find active harmony at this bar+beat
                    var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                    if (harmonyEvent == null)
                        continue;

                    // Build harmony context (use higher octave for comp than bass)
                    const int guitarOctave = 4;
                    var ctx = HarmonyPitchContextBuilder.Build(
                        harmonyEvent.Key,
                        harmonyEvent.Degree,
                        harmonyEvent.Quality,
                        harmonyEvent.Bass,
                        guitarOctave,
                        policy);

                    // Select comp voicing (2-4 note chord fragment)
                    var voicing = CompVoicingSelector.Select(ctx, slot, previousVoicing, sectionProfile);

                    // Calculate strum timing offsets for this chord
                    var strumOffsets = StrumTimingEngine.CalculateStrumOffsets(
                        voicing,
                        slot.Bar,
                        slot.OnsetBeat,
                        "comp",
                        settings.Seed);

                    // Add all notes from the voicing with strum timing offsets
                    for (int i = 0; i < voicing.Count; i++)
                    {
                        int midiNote = voicing[i];
                        int strumOffset = strumOffsets[i];

                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: (int)slot.StartTick + strumOffset,
                            noteDurationTicks: slot.DurationTicks,
                            noteOnVelocity: 85));
                    }

                    // Update previous voicing for next onset
                    previousVoicing = voicing;
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }
    }
}
