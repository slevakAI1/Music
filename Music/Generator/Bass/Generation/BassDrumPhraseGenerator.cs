// AI: purpose=Generate Bass phrase (1-N bars) using operator-based variation over anchor onsets.
// AI: invariants=Output is a PartTrack; reusable for MaterialBank storage.
// AI: deps=bassOperatorApplicator; bassOperatorRegistry; BarTrack; SongContext; PartTrack.

using Music.Generator.Bass.Operators;
using Music.Generator.Drums.Operators;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Generator.Bass.Generation
{
    // AI: purpose=Configuration for bassPhraseGenerator; controls roles, velocity, diagnostics.
    // AI: invariants=Defaults chosen for sensible generation; ActiveRoles null => default set.
    public sealed record BassGeneratorSettings
    {
        // Enable diagnostics collection. Keep false in production pipelines.
        public bool EnableDiagnostics { get; init; } = false;

        // Active roles to generate. When null, GetActiveRoles() returns conservative default roles.
        public IReadOnlyList<string>? ActiveRoles { get; init; }

        // Default MIDI velocity applied when candidate/anchor provides no hint.
        public int DefaultVelocity { get; init; } = 100;

        // Default settings instance used by parameterless constructor paths.
        public static BassGeneratorSettings Default => new();

        // Return configured roles or conservative default set used by generation logic.
        public IReadOnlyList<string> GetActiveRoles()
        {
            return ActiveRoles ?? new[] { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat };
        }
    }

    // AI: purpose=Orchestrates Bass generation: anchors from groove + random operator application via bassOperatorApplicator.
    // AI: arch=validate → extract anchors → apply N random operators (dedup) → convert to MIDI PartTrack.
    public sealed class BassPhraseGenerator
    {
        private readonly DrumOperatorRegistry _registry;
        private readonly BassGeneratorSettings _settings;

        // AI: purpose=Default entry point; builds operator registry; settings fixed to defaults.
        public BassPhraseGenerator()
        {
            _registry = DrumOperatorRegistryBuilder.BuildComplete();
            _settings = BassGeneratorSettings.Default;
        }

        // AI: purpose=Generate a Bass PartTrack using anchors + random operator application.
        // AI: invariants=Throws on null/missing tracks; respects maxBars limit when >0.
        // AI: flow=validate → extract anchors → apply random operators → convert to MIDI.
        public PartTrack Generate(SongContext songContext, int bassProgramNumber, int maxBars = 0)
        {
            ValidateSongContext(songContext);

            var barTrack = songContext.BarTrack;
            var sectionTrack = songContext.SectionTrack;
            var groovePresetDefinition = songContext.GroovePresetDefinition;
            int totalBars = sectionTrack.TotalBars;

            // Limit bars if maxBars > 0
            if (maxBars > 0 && maxBars < totalBars)
            {
                totalBars = maxBars;
            }

            var bars = barTrack.Bars.Where(b => b.BarNumber <= totalBars).ToList();

            // Extract anchor onsets (foundation that's always present)
            var anchorOnsets = ExtractAnchorOnsets(groovePresetDefinition, totalBars, barTrack);

        var NumberOfOperators = 2;
        var allOnsets = ApplyBassOperators(bars, anchorOnsets, totalBars, NumberOfOperators);

            // Convert to MIDI events
            return ConvertToPartTrack(allOnsets, barTrack, bassProgramNumber);
        }

        #region Private Helper Methods

        private static void ValidateSongContext(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);

            if (songContext.BarTrack == null)
                throw new ArgumentException("BarTrack must be provided", nameof(songContext));

            if (songContext.SectionTrack == null || songContext.SectionTrack.Sections.Count == 0)
                throw new ArgumentException("SectionTrack must have sections", nameof(songContext));

            if (songContext.GroovePresetDefinition == null)
                throw new ArgumentException("GroovePresetDefinition must be provided", nameof(songContext));

            if (songContext.GroovePresetDefinition.AnchorLayer == null)
                throw new ArgumentException("GroovePresetDefinition.AnchorLayer must be provided", nameof(songContext));
        }

        private List<GrooveOnset> ExtractAnchorOnsets(
            GroovePresetDefinition groovePresetDefinition,
            int totalBars,
            BarTrack barTrack)
        {
            var onsets = new List<GrooveOnset>();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = groovePresetDefinition.GetActiveGroovePreset(bar);
                var anchorLayer = groovePreset.AnchorLayer;

                // Extract kick onsets
                foreach (var beat in anchorLayer.KickOnsets)
                {
                    onsets.Add(new GrooveOnset
                    {
                        Role = GrooveRoles.Kick,
                        BarNumber = bar,
                        Beat = beat,
                        Velocity = _settings.DefaultVelocity,
                        IsMustHit = true
                    });
                }

                // Extract snare onsets
                foreach (var beat in anchorLayer.SnareOnsets)
                {
                    onsets.Add(new GrooveOnset
                    {
                        Role = GrooveRoles.Snare,
                        BarNumber = bar,
                        Beat = beat,
                        Velocity = _settings.DefaultVelocity,
                        IsMustHit = true
                    });
                }

                // Extract hat onsets
                foreach (var beat in anchorLayer.HatOnsets)
                {
                    onsets.Add(new GrooveOnset
                    {
                        Role = GrooveRoles.ClosedHat,
                        BarNumber = bar,
                        Beat = beat,
                        Velocity = _settings.DefaultVelocity,
                        IsMustHit = true
                    });
                }
            }

            return onsets.OrderBy(o => o.BarNumber).ThenBy(o => o.Beat).ToList();
        }

        // AI: purpose=Delegate to bassOperatorApplicator; simple random operator application over anchors.
        private List<GrooveOnset> ApplyBassOperators(
            IReadOnlyList<Bar> bars,
            List<GrooveOnset> anchorOnsets,
            int totalBars,
            int numberOfOperators)
        {
            return DrumOperatorApplicator.Apply(bars, anchorOnsets, totalBars, numberOfOperators, _registry);
        }

        private static PartTrack ConvertToPartTrack(
            List<GrooveOnset> onsets,
            BarTrack barTrack,
            int bassProgramNumber)
        {
            var events = new List<PartTrackEvent>();

            foreach (var onset in onsets)
            {
                // Get absolute tick position
                long tickPosition = barTrack.ToTick(onset.BarNumber, onset.Beat);

                // Apply timing offset if present
                if (onset.TimingOffsetTicks.HasValue)
                {
                    tickPosition += onset.TimingOffsetTicks.Value;
                }

                // Map role to MIDI note number
                int midiNote = MapRoleToMidiNote(onset.Role);

                // Get velocity
                int velocity = onset.Velocity ?? 100;

                // Create MIDI event
                events.Add(new PartTrackEvent
                {
                    AbsoluteTimeTicks = tickPosition,
                    Type = PartTrackEventType.NoteOn,
                    NoteNumber = midiNote,
                    NoteDurationTicks = 120, // Default 8th note duration
                    NoteOnVelocity = velocity
                });
            }

            // CRITICAL: Sort events by AbsoluteTimeTicks for MIDI export validation
            events = events.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(events) { MidiProgramNumber = bassProgramNumber };
        }

        private static int MapRoleToMidiNote(string role)
        {
            return role switch
            {
                GrooveRoles.Kick => 36,         // Acoustic Bass Bass
                GrooveRoles.Snare => 38,        // Acoustic Snare
                GrooveRoles.ClosedHat => 42,    // Closed Hi-Hat
                GrooveRoles.OpenHat => 46,      // Open Hi-Hat
                GrooveRoles.Crash => 49,        // Crash Cymbal 1
                GrooveRoles.Ride => 51,         // Ride Cymbal 1
                GrooveRoles.Tom1 => 50,         // High Tom
                GrooveRoles.Tom2 => 47,         // Mid Tom
                GrooveRoles.FloorTom => 45,     // Low Tom
                _ => 38                         // Default to snare for unknown roles
            };
        }

        #endregion
    }
}
