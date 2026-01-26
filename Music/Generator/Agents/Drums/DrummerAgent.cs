// AI: purpose=DrummerAgent facade class; unifies policy provider, candidate source, memory, and registry for drum generation.
// AI: invariants=Implements IGroovePolicyProvider and IGrooveCandidateSource via delegation; deterministic output for same inputs.
// AI: deps=DrummerPolicyProvider, DrummerCandidateSource, DrummerMemory, DrumOperatorRegistry, StyleConfiguration.
// AI: change=Story 8.1; facade pattern enables integration with Generator.cs and future testing.

using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums.Diagnostics;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Groove;
using Music.Generator.Material;
using Music.MyMidi;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Settings for DrummerAgent behavior.
    /// </summary>
    public sealed record DrummerAgentSettings
    {
        /// <summary>Whether to enable diagnostics collection (default: false for zero-cost).</summary>
        public bool EnableDiagnostics { get; init; } = false;

        /// <summary>Policy settings for fill windows and density modifiers.</summary>
        public DrummerPolicySettings? PolicySettings { get; init; }

        /// <summary>Candidate source settings for error handling.</summary>
        public DrummerCandidateSourceSettings? CandidateSourceSettings { get; init; }

        /// <summary>Physicality rules (limb model, sticking, overcrowding caps).</summary>
        public PhysicalityRules? PhysicalityRules { get; init; }

        /// <summary>Default settings instance.</summary>
        public static DrummerAgentSettings Default => new();
    }

    /// <summary>
    /// Facade class that unifies all drummer agent components for integration with the generator pipeline.
    /// Implements IGroovePolicyProvider and IGrooveCandidateSource to hook into the groove system.
    /// Story 8.1: Wire Drummer Agent into Generator.
    /// </summary>
    /// <remarks>
    /// <para>This class owns and manages the lifecycle of:</para>
    /// <list type="bullet">
    ///   <item>DrumOperatorRegistry (built from DrumOperatorRegistryBuilder)</item>
    ///   <item>DrummerMemory (persists for agent lifetime)</item>
    ///   <item>DrummerPolicyProvider (delegates IGroovePolicyProvider)</item>
    ///   <item>DrummerCandidateSource (delegates IGrooveCandidateSource)</item>
    /// </list>
    /// <para>Memory persists across Generate() calls for song-level anti-repetition.
    /// Different songs should use different DrummerAgent instances.</para>
    /// </remarks>
    public sealed class DrummerAgent : IGroovePolicyProvider, IGrooveCandidateSource
    {
        private readonly StyleConfiguration _styleConfig;
        private readonly DrumOperatorRegistry _registry;
        private readonly DrummerMemory _memory;
        private readonly DrummerPolicyProvider _policyProvider;
        private readonly DrummerCandidateSource _candidateSource;
        private readonly PhysicalityFilter? _physicalityFilter;
        private readonly DrummerAgentSettings _settings;

        /// <summary>
        /// Creates a DrummerAgent with the specified style configuration.
        /// </summary>
        /// <param name="styleConfig">Style configuration (PopRock, Jazz, Metal, etc.).</param>
        /// <param name="settings">Optional agent settings (diagnostics, policy, physicality).</param>
        /// <param name="motifPresenceMap">Optional motif presence map for ducking (Story 9.3).</param>
        /// <exception cref="ArgumentNullException">If styleConfig is null.</exception>
        public DrummerAgent(
            StyleConfiguration styleConfig,
            DrummerAgentSettings? settings = null,
            MotifPresenceMap? motifPresenceMap = null)
        {
            ArgumentNullException.ThrowIfNull(styleConfig);

            _styleConfig = styleConfig;
            _settings = settings ?? DrummerAgentSettings.Default;

            // Build operator registry (internally builds and freezes all 28 operators)
            _registry = DrumOperatorRegistryBuilder.BuildComplete();

            // Create memory (persists for agent lifetime)
            _memory = new DrummerMemory();

            // Create physicality filter if rules provided
            _physicalityFilter = _settings.PhysicalityRules != null
                ? new PhysicalityFilter(_settings.PhysicalityRules, diagnosticsCollector: null)
                : null;

            // Create policy provider (delegates IGroovePolicyProvider)
            _policyProvider = new DrummerPolicyProvider(
                styleConfig,
                _memory,
                _settings.PolicySettings,
                motifPresenceMap);

            // Create candidate source (delegates IGrooveCandidateSource)
            _candidateSource = new DrummerCandidateSource(
                _registry,
                styleConfig,
                _memory,
                _physicalityFilter,
                diagnosticsCollector: null,
                _settings.CandidateSourceSettings);
        }

        /// <summary>
        /// Gets the style configuration used by this agent.
        /// </summary>
        public StyleConfiguration StyleConfiguration => _styleConfig;

        /// <summary>
        /// Gets the operator registry (for diagnostics/inspection).
        /// </summary>
        public DrumOperatorRegistry Registry => _registry;

        /// <summary>
        /// Gets the drummer memory (for diagnostics/inspection).
        /// </summary>
        public DrummerMemory Memory => _memory;

        #region IGroovePolicyProvider Implementation

        /// <inheritdoc />
        public GroovePolicyDecision? GetPolicy(GrooveBarContext barContext, string role)
        {
            return _policyProvider.GetPolicy(barContext, role);
        }

        #endregion

        #region IGrooveCandidateSource Implementation

        /// <inheritdoc />
        public IReadOnlyList<GrooveCandidateGroup> GetCandidateGroups(
            GrooveBarContext barContext,
            string role)
        {
            return _candidateSource.GetCandidateGroups(barContext, role);
        }

        #endregion

        #region Generate Entry Point

        /// <summary>
        /// Generates a drum track from the provided song context.
        /// This is the main entry point for drum generation using the drummer agent.
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
        ///   <item>For each bar/role: get policy decisions and candidate groups</item>
        ///   <item>Select candidates using groove selection engine</item>
        ///   <item>Apply performance rendering (velocity, timing)</item>
        ///   <item>Convert to MIDI events</item>
        /// </list>
        /// <para>If operators generate no candidates, anchor-only track is returned.</para>
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
            int drumProgramNumber = GetDrumProgramNumber(songContext.Voices);

            // Build per-bar contexts
            var barContexts = BarContextBuilder.Build(sectionTrack, segmentProfiles, totalBars);

            // Merge protection hierarchy layers
            var mergedProtections = ProtectionPerBarBuilder.Build(barContexts, groovePresetDefinition.ProtectionPolicy);

            // Apply phrase hook policy
            PhraseHookProtectionAugmenter.Augment(
                mergedProtections,
                barContexts,
                groovePresetDefinition.ProtectionPolicy?.PhraseHookPolicy,
                groovePresetDefinition.Identity.BeatsPerBar);

            // Extract anchor onsets (foundation that's always present)
            var anchorOnsets = ExtractAnchorOnsets(groovePresetDefinition, totalBars, barTrack);

            // Generate operator-based candidates for each bar
            var operatorOnsets = GenerateOperatorOnsets(barContexts, barTrack, totalBars);

            // Combine anchors with operator candidates
            var allOnsets = CombineOnsets(anchorOnsets, operatorOnsets);

            // Convert to MIDI events
            var notes = ConvertToMidiEvents(allOnsets);

            return new PartTrack(notes) { MidiProgramNumber = drumProgramNumber };
        }

        #endregion

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

        private static int GetDrumProgramNumber(VoiceSet voices)
        {
            const int DefaultDrumProgram = 255;

            var voice = voices.Voices.FirstOrDefault(v =>
                string.Equals(v.GrooveRole, "DrumKit", StringComparison.OrdinalIgnoreCase));

            if (voice == null)
                return DefaultDrumProgram;

            var midiVoice = MidiVoices.MidiVoiceList()
                .FirstOrDefault(mv => string.Equals(mv.Name, voice.VoiceName, StringComparison.OrdinalIgnoreCase));

            return midiVoice?.ProgramNumber ?? DefaultDrumProgram;
        }

        private static List<DrumOnset> ExtractAnchorOnsets(
            GroovePresetDefinition groovePresetDefinition,
            int totalBars,
            BarTrack barTrack)
        {
            var onsets = new List<DrumOnset>();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = groovePresetDefinition.GetActiveGroovePreset(bar);
                var anchorLayer = groovePreset.AnchorLayer;

                foreach (var beat in anchorLayer.KickOnsets)
                {
                    long tickPosition = barTrack.ToTick(bar, beat);
                    onsets.Add(new DrumOnset(DrumRole.Kick, bar, beat, 100, tickPosition));
                }

                foreach (var beat in anchorLayer.SnareOnsets)
                {
                    long tickPosition = barTrack.ToTick(bar, beat);
                    onsets.Add(new DrumOnset(DrumRole.Snare, bar, beat, 100, tickPosition));
                }

                foreach (var beat in anchorLayer.HatOnsets)
                {
                    long tickPosition = barTrack.ToTick(bar, beat);
                    onsets.Add(new DrumOnset(DrumRole.ClosedHat, bar, beat, 100, tickPosition));
                }
            }

            return onsets;
        }

        private List<DrumOnset> GenerateOperatorOnsets(
            IReadOnlyList<BarContext> barContexts,
            BarTrack barTrack,
            int totalBars)
        {
            var onsets = new List<DrumOnset>();
            var drumRoles = new[] { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat };

            foreach (var barContext in barContexts)
            {
                var grooveBarContext = CreateGrooveBarContext(barContext);

                foreach (var role in drumRoles)
                {
                    // Get candidate groups from candidate source
                    var candidateGroups = GetCandidateGroups(grooveBarContext, role);

                    // Convert candidates to DrumOnsets
                    foreach (var group in candidateGroups)
                    {
                        foreach (var candidate in group.Candidates)
                        {
                            long tickPosition = barTrack.ToTick(barContext.BarNumber, candidate.OnsetBeat);
                            var drumRole = ParseDrumRole(candidate.Role);
                            onsets.Add(new DrumOnset(
                                drumRole,
                                barContext.BarNumber,
                                candidate.OnsetBeat,
                                candidate.VelocityHint ?? 100,
                                tickPosition));
                        }
                    }
                }
            }

            return onsets;
        }

        private static GrooveBarContext CreateGrooveBarContext(BarContext barContext)
        {
            return new GrooveBarContext(
                barContext.BarNumber,
                barContext.Section,
                barContext.SegmentProfile,
                barContext.BarWithinSection,
                barContext.BarsUntilSectionEnd);
        }

        private static DrumRole ParseDrumRole(string roleName)
        {
            return roleName switch
            {
                GrooveRoles.Kick => DrumRole.Kick,
                GrooveRoles.Snare => DrumRole.Snare,
                GrooveRoles.ClosedHat => DrumRole.ClosedHat,
                GrooveRoles.OpenHat => DrumRole.OpenHat,
                "Crash" => DrumRole.Crash,
                "Ride" => DrumRole.Ride,
                "Tom1" => DrumRole.TomHigh,
                "Tom2" => DrumRole.TomMid,
                "FloorTom" => DrumRole.TomLow,
                _ => DrumRole.Snare // Safe fallback
            };
        }

        private static List<DrumOnset> CombineOnsets(List<DrumOnset> anchors, List<DrumOnset> operators)
        {
            // Combine anchors and operator onsets, removing duplicates by position
            var combined = new Dictionary<(int, decimal, DrumRole), DrumOnset>();

            foreach (var onset in anchors)
            {
                var key = (onset.BarNumber, onset.Beat, onset.Role);
                combined[key] = onset;
            }

            foreach (var onset in operators)
            {
                var key = (onset.BarNumber, onset.Beat, onset.Role);
                // Operators can add but not replace anchors (anchors have priority)
                if (!combined.ContainsKey(key))
                {
                    combined[key] = onset;
                }
            }

            return combined.Values
                .OrderBy(o => o.TickPosition)
                .ToList();
        }

        private static List<PartTrackEvent> ConvertToMidiEvents(List<DrumOnset> onsets)
        {
            var notes = new List<PartTrackEvent>();

            foreach (var onset in onsets)
            {
                int midiNote = GetMidiNote(onset.Role);
                notes.Add(new PartTrackEvent
                {
                    AbsoluteTimeTicks = onset.TickPosition,
                    Type = PartTrackEventType.NoteOn,
                    NoteNumber = midiNote,
                    NoteDurationTicks = 240, // Default 8th note duration
                    NoteOnVelocity = onset.Velocity
                });
            }

            return notes.OrderBy(n => n.AbsoluteTimeTicks).ToList();
        }

        private static int GetMidiNote(DrumRole role)
        {
            return role switch
            {
                DrumRole.Kick => 36,      // Acoustic Bass Drum
                DrumRole.Snare => 38,     // Acoustic Snare
                DrumRole.ClosedHat => 42, // Closed Hi-Hat
                DrumRole.OpenHat => 46,   // Open Hi-Hat
                DrumRole.Ride => 51,      // Ride Cymbal 1
                DrumRole.Crash => 49,     // Crash Cymbal 1
                DrumRole.TomHigh => 50,   // High Tom
                DrumRole.TomMid => 47,    // Mid Tom
                DrumRole.TomLow => 45,    // Low Tom
                _ => 38                   // Default to snare
            };
        }

        #endregion

        #region Reset

        /// <summary>
        /// Resets the agent memory for a new song.
        /// Call this if reusing the same DrummerAgent instance for multiple songs.
        /// </summary>
        public void ResetMemory()
        {
            _memory.Clear();
        }

        #endregion
    }
}
