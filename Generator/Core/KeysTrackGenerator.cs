// AI: purpose=Generate keys/pads track using VoiceLeadingSelector and SectionProfile for dynamic voicing per section.
// AI: keep program number 4; tracks previous ChordRealization for voice-leading continuity; returns sorted by AbsoluteTimeTicks.

using Music.MyMidi;

namespace Music.Generator
{
    internal static class KeysTrackGenerator
    {
        /// <summary>
        /// Generates keys/pads track: voice-led chord voicings with optional color tones per section profile.
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
            var randomizer = new PitchRandomizer(settings);
            const int keysOctave = 3;

            HarmonyEvent? previousHarmony = null;
            ChordRealization? previousVoicing = null; // Track previous voicing for voice leading

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var padsOnsets = grooveEvent.AnchorLayer.PadsOnsets;
                if (padsOnsets == null || padsOnsets.Count == 0)
                    continue;

                // Get section profile for current bar
                SectionProfile? sectionProfile = null;
                if (sectionTrack.GetActiveSection(bar, out var section) && section != null)
                {
                    sectionProfile = SectionProfile.GetForSectionType(section.SectionType);
                }

                // Build onset grid for this bar
                var onsetSlots = OnsetGrid.Build(bar, padsOnsets, barTrack);

                foreach (var slot in onsetSlots)
                {
                    // Find active harmony at this bar+beat
                    var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                    if (harmonyEvent == null)
                        continue;

                    bool isFirstOnset = previousHarmony == null ||
                        harmonyEvent.StartBar != previousHarmony.StartBar ||
                        harmonyEvent.StartBeat != previousHarmony.StartBeat;

                    var ctx = HarmonyPitchContextBuilder.Build(
                        harmonyEvent.Key,
                        harmonyEvent.Degree,
                        harmonyEvent.Quality,
                        harmonyEvent.Bass,
                        keysOctave,
                        policy);

                    ChordRealization chordRealization;

                    // For first onset of new harmony, optionally add color tones via randomizer
                    if (isFirstOnset)
                    {
                        var baseVoicing = randomizer.SelectKeysVoicing(ctx, slot.Bar, slot.OnsetBeat, isFirstOnset, sectionProfile);
                        
                        // If we have previous voicing, apply voice leading to the base voicing
                        if (previousVoicing != null)
                        {
                            chordRealization = VoiceLeadingSelector.Select(previousVoicing, ctx, sectionProfile);
                            

                            // Preserve color tone from randomizer if it was added
                            if (baseVoicing.HasColorTone && !chordRealization.HasColorTone)
                            {
                                // Add the color tone from base voicing
                                var notesWithColor = chordRealization.MidiNotes.ToList();
                                var colorNotes = baseVoicing.MidiNotes.Except(ctx.ChordMidiNotes).ToList();
                                notesWithColor.AddRange(colorNotes);
                                
                                chordRealization = chordRealization with
                                {
                                    MidiNotes = notesWithColor,
                                    HasColorTone = true,
                                    ColorToneTag = baseVoicing.ColorToneTag,
                                    Density = notesWithColor.Count
                                };
                            }
                        }
                        else
                        {
                            // First onset ever: use randomizer result
                            chordRealization = baseVoicing;
                        }
                    }
                    else
                    {
                        // Subsequent onset of same harmony: use voice leading
                        chordRealization = VoiceLeadingSelector.Select(previousVoicing, ctx, sectionProfile);
                    }

                    foreach (int midiNote in chordRealization.MidiNotes)
                    {
                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: slot.DurationTicks,
                            noteOnVelocity: 75));
                    }

                    // Update previous voicing for next onset
                    previousVoicing = chordRealization;
                }

                // Update previousHarmony to the first event active at the bar start (bar,1)
                previousHarmony = harmonyTrack.GetActiveHarmonyEvent(bar, 1m);
            }

            // Ensure events are sorted by AbsoluteTimeTicks before returning
            notes = notes.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }
    }
}
