// AI: purpose=Pipeline orchestrator for drum generation using IGroovePolicyProvider + IGrooveCandidateSource.
// AI: invariants=Uses GrooveSelectionEngine for weighted selection; enforces density targets/caps from policy.
// AI: deps=DrummerAgent (data source), GrooveSelectionEngine, BarContextBuilder, SongContext, PartTrack.
// AI: change=Story RF-2; correct architecture replaces DrummerAgent.Generate() with proper groove integration.

using Music.Generator.Groove;
using Music.Generator.Material;
using Music.MyMidi;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Settings for GrooveBasedDrumGenerator behavior.
    /// </summary>
    public sealed record GrooveBasedDrumGeneratorSettings
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
        public static GrooveBasedDrumGeneratorSettings Default => new();

        /// <summary>Gets the active roles, falling back to default if not set.</summary>
        public IReadOnlyList<string> GetActiveRoles()
        {
            return ActiveRoles ?? new[] { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat };
        }
    }

    /// <summary>
    /// Pipeline orchestrator for drum generation using groove system integration.
    /// Story RF-2: Creates drum tracks by properly using IGroovePolicyProvider + IGrooveCandidateSource
    /// with GrooveSelectionEngine for weighted selection and density enforcement.
    /// </summary>
    /// <remarks>
    /// <para>Architecture:</para>
    /// <list type="bullet">
    ///   <item>Takes policy provider + candidate source (typically DrummerAgent)</item>
    ///   <item>Extracts anchors from groove preset</item>
    ///   <item>For each bar+role: gets policy → calculates target → gets candidates → selects via GrooveSelectionEngine</item>
    ///   <item>Combines anchors + selected operators → converts to MIDI</item>
    /// </list>
    /// <para>Enforces density targets, operator caps, and weighted selection per policy decisions.</para>
    /// </remarks>
    public sealed class GrooveBasedDrumGenerator
    {
        private readonly IGroovePolicyProvider _policyProvider;
        private readonly IGrooveCandidateSource _candidateSource;
        private readonly GrooveBasedDrumGeneratorSettings _settings;

        /// <summary>
        /// Creates a GrooveBasedDrumGenerator with the specified policy and candidate providers.
        /// </summary>
        /// <param name="policyProvider">Policy provider (provides density targets, caps, weights).</param>
        /// <param name="candidateSource">Candidate source (provides operator-generated candidates).</param>
        /// <param name="settings">Optional settings (diagnostics, active roles, default velocity).</param>
        /// <exception cref="ArgumentNullException">If policyProvider or candidateSource is null.</exception>
        public GrooveBasedDrumGenerator(
            IGroovePolicyProvider policyProvider,
            IGrooveCandidateSource candidateSource,
            GrooveBasedDrumGeneratorSettings? settings = null)
        {
            ArgumentNullException.ThrowIfNull(policyProvider);
            ArgumentNullException.ThrowIfNull(candidateSource);

            _policyProvider = policyProvider;
            _candidateSource = candidateSource;
            _settings = settings ?? GrooveBasedDrumGeneratorSettings.Default;
        }

        /// <summary>
        /// Generates a drum track from the provided song context.
        /// </summary>
        /// <param name="songContext">Song context with section, groove, and timing data.</param>
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
        public PartTrack Generate(SongContext songContext)
        {
            ValidateSongContext(songContext);

            var barTrack = songContext.BarTrack;
            var sectionTrack = songContext.SectionTrack;
            var segmentProfiles = songContext.SegmentGrooveProfiles;
            var groovePresetDefinition = songContext.GroovePresetDefinition;
            int totalBars = sectionTrack.TotalBars;

            // Resolve MIDI program number for drums
            int drumProgramNumber = GetDrumProgramNumber(songContext);

            // Build per-bar contexts
            var barContexts = BarContextBuilder.Build(sectionTrack, segmentProfiles, totalBars);

            // Extract anchor onsets (foundation that's always present)
            var anchorOnsets = ExtractAnchorOnsets(groovePresetDefinition, totalBars, barTrack);

            // Generate operator-based candidates for each bar using GrooveSelectionEngine
            var operatorOnsets = GenerateOperatorOnsets(barContexts, anchorOnsets, barTrack);

            // Combine anchors with operator onsets
            var allOnsets = CombineOnsets(anchorOnsets, operatorOnsets);

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
            IReadOnlyList<BarContext> barContexts,
            List<GrooveOnset> anchors,
            BarTrack barTrack)
        {
            var result = new List<GrooveOnset>();
            var activeRoles = _settings.GetActiveRoles();

            foreach (var barContext in barContexts)
            {
                var grooveBarContext = GrooveBarContext.FromBarContext(barContext);

                // Get anchors for this bar to avoid conflicts
                var barAnchors = anchors.Where(a => a.BarNumber == barContext.BarNumber).ToList();

                foreach (var role in activeRoles)
                {
                    // Get policy decision for this bar+role
                    var policy = _policyProvider.GetPolicy(grooveBarContext, role);

                    // Calculate target count from density
                    int targetCount = CalculateTargetCount(policy, role);

                    if (targetCount <= 0)
                        continue; // No operators needed for this bar+role

                    // Get candidate groups from candidate source
                    var candidateGroups = _candidateSource.GetCandidateGroups(grooveBarContext, role);

                    if (candidateGroups.Count == 0)
                        continue; // No candidates available

                    // Filter anchors for this role to pass to GrooveSelectionEngine
                    var roleAnchors = barAnchors
                        .Where(a => string.Equals(a.Role, role, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // SELECT using GrooveSelectionEngine
                    var selected = DrumSelectionEngine.SelectUntilTargetReached(
                        grooveBarContext,
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
                            BarNumber = barContext.BarNumber,
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

        private int CalculateTargetCount(GroovePolicyDecision? policy, string role)
        {
            // If no policy or no density override, return 0 (anchor-only)
            if (policy?.Density01Override == null)
                return 0;

            double density01 = policy.Density01Override.Value;

            // Base count on beats per bar (assume 4/4 for MVP)
            const int beatsPerBar = 4;
            int baseCount = beatsPerBar;

            // Scale by density (allows up to 8 hits in 4/4 at density 1.0)
            int targetCount = (int)(baseCount * density01 * 2.0);

            // Clamp to policy max if present
            int maxCount = policy.MaxEventsPerBarOverride ?? 16;
            targetCount = Math.Clamp(targetCount, 0, maxCount);

            return targetCount;
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
