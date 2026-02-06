// AI: purpose=Generates a drum phrase (1-N bars) using operator-based variation over anchors.
// AI: invariants=Output is a PartTrack representing a single phrase; reusable for MaterialBank storage.licy.
// AI: deps=DrummerCandidateSource, DrumSelectionEngine, BarTrack bars, SongContext, PartTrack.
// AI: change=correct architecture replaces DrummerAgent.Generate() with proper groove integration.

using Music.Generator.Core;
using Music.Generator.Drums.Operators;
using Music.Generator.Drums.Selection;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Generator.Drums.Generation
{
    /// <summary>
    /// Settings for DrumGenerator behavior.
    /// </summary>
    public sealed record DrumGeneratorSettings
    {
        /// <summary>Whether to enable diagnostics collection (default: false).</summary>
        public bool EnableDiagnostics { get; init; } = false;

        /// <summary>
        /// Active drum roles to generate (default: Kick, Snare, ClosedHat).
        /// </summary>
        public IReadOnlyList<string>? ActiveRoles { get; init; }

        /// <summary>Default MIDI velocity for generated notes (default: 100).</summary>
        public int DefaultVelocity { get; init; } = 100;

        /// <summary>Default settings instance.</summary>
        public static DrumGeneratorSettings Default => new();

        /// <summary>Gets the active roles, falling back to default if not set.</summary>
        public IReadOnlyList<string> GetActiveRoles()
        {
            return ActiveRoles ?? new[] { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat };
        }
    }

    /// <summary>
    /// Pipeline orchestrator for drum generation using groove system integration.
    /// Story RF-2: Creates drum tracks by properly using IDrumPolicyProvider + IDrumCandidateSource
    /// with DrumSelectionEngine for weighted selection and density enforcement.
    /// Story 4.2: Moved interface ownership from Groove to Drums namespace.
    /// </summary>
    /// <remarks>
    /// <para>Architecture:</para>
    /// <list type="bullet">
    ///   <item>Takes policy provider + candidate source (built from operator registry)</item>
    ///   <item>Extracts anchors from groove preset</item>
    ///   <item>For each bar+role: gets policy → calculates target → gets candidates → selects via DrumSelectionEngine</item>
    ///   <item>Combines anchors + selected operators → converts to MIDI</item>
    /// </list>
    /// <para>Enforces density targets, operator caps, and weighted selection per policy decisions.</para>
    /// </remarks>
    public sealed class DrumPhraseGenerator
    {
        private readonly IDrumCandidateSource _candidateSource;
        private readonly DrumGeneratorSettings _settings;

        // AI: purpose=Create candidate source from operator registry; keeps generator style-free.
        public DrumPhraseGenerator()
            : this(BuildCandidateSource(), settings: null)
        {
        }

        /// <summary>
        /// Creates a DrumGenerator with the specified candidate provider.
        /// </summary>
        /// <param name="candidateSource">Candidate source (provides operator-generated candidates).</param>
        /// <param name="settings">Optional settings (diagnostics, active roles, default velocity).</param>
        /// <exception cref="ArgumentNullException">If candidateSource is null.</exception>
        public DrumPhraseGenerator(
            IDrumCandidateSource candidateSource,
            DrumGeneratorSettings? settings = null)
        {
            ArgumentNullException.ThrowIfNull(candidateSource);

            _candidateSource = candidateSource;
            _settings = settings ?? DrumGeneratorSettings.Default;
        }

        private static IDrumCandidateSource BuildCandidateSource()
        {
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            return new DrummerCandidateSource(
                registry,
                diagnosticsCollector: null,
                settings: null);
        }

        /// <summary>
        /// Generates a drum track from the provided song context.
        /// </summary>
        /// <param name="songContext">Song context with section, groove, and timing data.</param>
        /// <param name="maxBars">Maximum number of bars to generate. When 0, generates full song. When > 0, limits generation to first N bars.</param>
        /// <returns>Generated drum PartTrack.</returns>
        /// <exception cref="ArgumentNullException">If songContext is null.</exception>
        /// <exception cref="ArgumentException">If required tracks are missing.</exception>
        /// <remarks>
        /// <para>Generation steps:</para>
        /// <list type="number">
        ///   <item>Validate song context</item>
        ///   <item>Extract anchor onsets from groove preset</item>
        ///   <item>Build per-bar contexts</item>
        ///   <item>For each bar+role: get policy → calculate target → get candidates → select via GrooveSelectionEngine</item>
        ///   <item>Combine anchors + selected operators</item>
        ///   <item>Convert to MIDI events</item>
        /// </list>
        /// </remarks>
        public PartTrack Generate(SongContext songContext, int maxBars = 0)
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

            // Resolve MIDI program number for drums
            int drumProgramNumber = GetDrumProgramNumber(songContext);

            var bars = barTrack.Bars.Where(b => b.BarNumber <= totalBars).ToList();

            // Extract anchor onsets (foundation that's always present)
            var anchorOnsets = ExtractAnchorOnsets(groovePresetDefinition, totalBars, barTrack);





            // Generate operator-based candidates for each bar using DrumSelectionEngine
            var operatorOnsets = GenerateOperatorOnsets(bars, anchorOnsets, totalBars);



            // Combine anchors with operator onsets
            var allOnsets = CombineOnsets(anchorOnsets, operatorOnsets);

            // AI: disconnect=Performance; no timing/velocity shaping in this phrase pass.
            // Convert to MIDI events
            return ConvertToPartTrack(allOnsets, barTrack, drumProgramNumber);
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

        private int GetDrumProgramNumber(SongContext songContext)
        {
            const int DefaultDrumProgram = 255;

            var voice = songContext.Voices.Voices.FirstOrDefault(v =>
                string.Equals(v.GrooveRole, "DrumKit", StringComparison.OrdinalIgnoreCase));

            if (voice == null)
                return DefaultDrumProgram;

            var midiVoice = MidiVoices.MidiVoiceList()
                .FirstOrDefault(mv => string.Equals(mv.Name, voice.VoiceName, StringComparison.OrdinalIgnoreCase));

            return midiVoice?.ProgramNumber ?? DefaultDrumProgram;
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

        private List<GrooveOnset> GenerateOperatorOnsets(
            IReadOnlyList<Bar> bars,
            List<GrooveOnset> anchors,
            int totalBars)
        {
            var result = new List<GrooveOnset>();
            var activeRoles = _settings.GetActiveRoles();

            // Iterate only through bars up to totalBars (respects maxBars limit)
            foreach (var bar in bars.Where(b => b.BarNumber <= totalBars))
            {
                                // Get anchors for this bar to avoid conflicts
                var barAnchors = anchors.Where(a => a.BarNumber == bar.BarNumber).ToList();

                foreach (var role in activeRoles)
                {
                    // AI: disconnect=Policy; use default density targets to isolate operator behavior.
                    int targetCount = DrumDensityCalculator
                        .ComputeDensityTarget(bar, role)
                        .TargetCount;


                    targetCount = 2;


                    if (targetCount <= 0)
                        continue; // No operators needed for this bar+role

                    // Get candidate groups from candidate source
                    var candidateGroups = _candidateSource.GetCandidateGroups(bar, role);

                    if (candidateGroups.Count == 0)
                        continue; // No candidates available

                    // Filter anchors for this role to pass to DrumSelectionEngine
                    var roleAnchors = barAnchors
                        .Where(a => string.Equals(a.Role, role, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // SELECT using DrumSelectionEngine
                    var selected = DrumSelectionEngine.SelectUntilTargetReached(
                        bar,
                        role,
                        candidateGroups,
                        targetCount,
                        roleAnchors,
                        diagnostics: null);

                    // Convert selected candidates to GrooveOnset
                    foreach (var candidate in selected)
                    {
                        result.Add(new GrooveOnset
                        {
                            Role = role,
                            BarNumber = bar.BarNumber,
                            Beat = candidate.OnsetBeat,
                            Velocity = candidate.VelocityHint ?? _settings.DefaultVelocity,
                            Strength = candidate.Strength,
                            TimingOffsetTicks = candidate.TimingHint
                        });
                    }
                }
            }

            return result;
        }

        private static List<GrooveOnset> CombineOnsets(
            List<GrooveOnset> anchors,
            List<GrooveOnset> operators)
        {
            // Create dictionary keyed by (bar, beat, role)
            var combined = new Dictionary<(int Bar, decimal Beat, string Role), GrooveOnset>();

            // Add all anchors first (they win conflicts)
            foreach (var onset in anchors)
            {
                var key = (onset.BarNumber, onset.Beat, onset.Role);
                combined[key] = onset;
            }

            // Add operators, skipping if position already occupied
            foreach (var onset in operators)
            {
                var key = (onset.BarNumber, onset.Beat, onset.Role);
                if (!combined.ContainsKey(key))
                {
                    combined[key] = onset;
                }
            }

            // Return sorted by bar, then beat
            return combined.Values
                .OrderBy(o => o.BarNumber)
                .ThenBy(o => o.Beat)
                .ToList();
        }

        private static PartTrack ConvertToPartTrack(
            List<GrooveOnset> onsets,
            BarTrack barTrack,
            int drumProgramNumber)
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

            return new PartTrack(events) { MidiProgramNumber = drumProgramNumber };
        }

        private static int MapRoleToMidiNote(string role)
        {
            return role switch
            {
                GrooveRoles.Kick => 36,         // Acoustic Bass Drum
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
