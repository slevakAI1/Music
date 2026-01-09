// AI: purpose=Generate drum track: kick, snare, hi-hat using DrumVariationEngine for living performance (Story 6.1).
// AI: invariants=Calls DrumVariationEngine per bar; converts variation plan to MIDI PartTrackEvent list.
// AI: deps=Uses DrumVariationEngine, RandomHelpers, PitchRandomizer.SelectDrumVelocity for velocity shaping.

using Music.MyMidi;

namespace Music.Generator
{
    internal static class DrumTrackGenerator
    {
        /// <summary>
        /// Generates drum track: kick, snare, hi-hat with deterministic variations.
        /// Updated to use DrumVariationEngine for Story 6.1 acceptance criteria.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            int totalBars,
            RandomizationSettings settings,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);

            // MIDI drum note numbers (General MIDI)
            const int kickNote = 36;
            const int snareNote = 38;
            const int closedHiHatNote = 42;
            const int openHiHatNote = 46;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGrooveEvent(bar);

                // Get section type for variation engine
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse; // Default
                if (sectionTrack.GetActiveSection(bar, out var section) && section != null)
                {
                    sectionType = section.SectionType;
                }

                // Generate per-bar variation plan using DrumVariationEngine
                var variation = DrumVariationEngine.Generate(grooveEvent, sectionType, bar, settings.Seed);

                // Convert variation plan to MIDI events
                foreach (var hit in variation.Hits)
                {
                    // Build onset grid to resolve beat position to ticks
                    var singleOnsetList = new List<decimal> { hit.OnsetBeat };
                    var slots = OnsetGrid.Build(bar, singleOnsetList, barTrack);
                    if (slots == null || slots.Count == 0)
                        continue;

                    var slot = slots[0];
                    int baseTick = (int)slot.StartTick + hit.TimingOffsetTicks;

                    switch (hit.Role)
                    {
                        case "kick":
                            {
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "kick", baseVelocity: 100);
                                // Reduce velocity for non-main kicks
                                int vel = hit.IsMain ? baseVel : Math.Max(25, (int)(baseVel * 0.82));
                                
                                notes.Add(new PartTrackEvent(
                                    noteNumber: kickNote,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                                    noteOnVelocity: vel));
                                break;
                            }

                        case "snare":
                            {
                                if (hit.IsFlam)
                                {
                                    // Flam pre-hit: softer, placed before main hit
                                    int preVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 60);
                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                                        noteOnVelocity: preVel));
                                }
                                else if (hit.IsGhost)
                                {
                                    // Ghost note: very quiet
                                    int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 40);
                                    int vel = Math.Max(8, (int)(baseVel * 0.6));
                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                                        noteOnVelocity: vel));
                                }
                                else
                                {
                                    // Main snare hit
                                    int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 90);
                                    int vel = hit.IsMain ? baseVel : Math.Max(30, (int)(baseVel * 0.85));
                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                                        noteOnVelocity: vel));
                                }
                                break;
                            }

                        case "hat":
                            {
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "hat", baseVelocity: 70);

                                // Apply hand pattern: accent strong beats
                                int vel = baseVel;
                                if (RandomHelpers.IsStrongBeat(slot.OnsetBeat))
                                    vel = Math.Min(127, vel + 8);

                                // Soften non-main hats
                                if (!hit.IsMain) 
                                    vel = Math.Max(20, (int)(vel * 0.85));

                                // Open vs closed articulation
                                int noteNumber = hit.IsOpenHat ? openHiHatNote : closedHiHatNote;

                                notes.Add(new PartTrackEvent(
                                    noteNumber: noteNumber,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: MusicConstants.TicksPerQuarterNote / 2,
                                    noteOnVelocity: vel));
                                break;
                            }
                    }
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }
    }
}
