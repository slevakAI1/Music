// AI: purpose=Generate drum track: kick, snare, hi-hat at groove onsets with controlled randomness.

using Music.MyMidi;

namespace Music.Generator
{
    internal static class DrumTrackGenerator
    {
        /// <summary>
        /// Generates drum track: kick, snare, hi-hat at groove onsets with controlled randomness.
        /// Updated to support groove track changes.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
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

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var layer = grooveEvent.AnchorLayer;

                // Kick pattern
                if (layer.KickOnsets != null && layer.KickOnsets.Count > 0)
                {
                    var onsetSlots = OnsetGrid.Build(bar, layer.KickOnsets, barTrack);
                    foreach (var slot in onsetSlots)
                    {
                        int velocity = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "kick", baseVelocity: 100);

                        notes.Add(new PartTrackEvent(
                            noteNumber: kickNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                            noteOnVelocity: velocity));
                    }
                }

                // Snare pattern
                if (layer.SnareOnsets != null && layer.SnareOnsets.Count > 0)
                {
                    var onsetSlots = OnsetGrid.Build(bar, layer.SnareOnsets, barTrack);
                    foreach (var slot in onsetSlots)
                    {
                        int velocity = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 90);

                        notes.Add(new PartTrackEvent(
                            noteNumber: snareNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                            noteOnVelocity: velocity));
                    }
                }

                // Hi-hat pattern
                if (layer.HatOnsets != null && layer.HatOnsets.Count > 0)
                {
                    var onsetSlots = OnsetGrid.Build(bar, layer.HatOnsets, barTrack);
                    foreach (var slot in onsetSlots)
                    {
                        int velocity = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "hat", baseVelocity: 70);

                        notes.Add(new PartTrackEvent(
                            noteNumber: closedHiHatNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote / 2,
                            noteOnVelocity: velocity));
                    }
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }
    }
}
