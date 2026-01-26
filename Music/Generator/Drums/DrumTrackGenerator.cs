// AI: purpose=Generate drum track using DrummerAgent (Story 10.8.1) or fallback to anchor-based generation.
// AI: deps=DrummerAgent for operator-based generation; BarTrack for tick conversion; returns PartTrack sorted by AbsoluteTimeTicks.
// AI: change=Story 10.8.1: integrated DrummerAgent; old anchor-based approach preserved as fallback.

using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Generator
{
    // AI: DrumRole identifies drum instrument for MIDI mapping and onset processing; extend for additional kit pieces.
    public enum DrumRole
    {
        Kick,
        Snare,
        ClosedHat,
        OpenHat,
        Ride,
        Crash,
        TomHigh,
        TomMid,
        TomLow
    }

    // AI: DrumOnset captures a single drum hit; minimal fields for MVP. Strength field added in Story 18 (velocity shaping).
    // AI: invariants=Beat is 1-based within bar; BarNumber is 1-based; Velocity 1-127; TickPosition computed from BarTrack.
    // AI: Story 9 adds protection flags: IsMustHit, IsNeverRemove, IsProtected for enforcement logic.
    public sealed record DrumOnset(
        DrumRole Role,
        int BarNumber,
        decimal Beat,
        int Velocity,
        long TickPosition)
    {
        public bool IsMustHit { get; set; }
        public bool IsNeverRemove { get; set; }
        public bool IsProtected { get; set; }
    }

    // AI: DrumBarContext removed in Story G1; replaced by shared Music.Generator.BarContext.
    // AI: change=Use BarContext from Music.Generator namespace for cross-generator bar context.

    public static class DrumTrackGenerator
    {
        // AI: MIDI drum note numbers (General MIDI standard); extend mapping as roles are added.
        private const int KickMidiNote = 36;
        private const int SnareMidiNote = 38;
        private const int ClosedHatMidiNote = 42;
        private const int OpenHatMidiNote = 46;
        private const int RideMidiNote = 51;
        private const int CrashMidiNote = 49;
        private const int TomHighMidiNote = 50;
        private const int TomMidMidiNote = 47;
        private const int TomLowMidiNote = 45;

        /// <summary>
        /// Generates drum track using DrummerAgent (Story 10.8.1).
        /// Falls back to anchor-based generation if agent is not available.
        /// </summary>
        /// <param name="songContext">Song context containing all required data.</param>
        /// <returns>Generated drum PartTrack.</returns>
        /// <exception cref="ArgumentNullException">If songContext is null.</exception>
        public static PartTrack Generate(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);

            // Story 10.8.1: Use DrummerAgent for operator-based generation
            try
            {
                var drummerAgent = new DrummerAgent(
                    StyleConfigurationLibrary.PopRock,
                    DrummerAgentSettings.Default);

                return drummerAgent.Generate(songContext);
            }
            catch (Exception ex)
            {
                // Fallback to anchor-based generation if DrummerAgent fails
                Console.WriteLine($"DrummerAgent generation failed, falling back to anchor-based: {ex.Message}");
                return GenerateLegacyAnchorBased(songContext);
            }
        }

        /// <summary>
        /// Legacy anchor-based generation (pre-Story 10.8.1).
        /// Preserved as fallback for compatibility.
        /// </summary>
        private static PartTrack GenerateLegacyAnchorBased(SongContext songContext)
        {
            var barTrack = songContext.BarTrack;
            var sectionTrack = songContext.SectionTrack;
            var segmentProfiles = songContext.SegmentGrooveProfiles;
            var groovePresetDefinition = songContext.GroovePresetDefinition;
            int totalBars = sectionTrack.TotalBars;
            int midiProgramNumber = GetDrumProgramNumber(songContext.Voices);

            return GenerateLegacyAnchorBasedInternal(
                barTrack,
                sectionTrack,
                segmentProfiles,
                groovePresetDefinition,
                totalBars,
                midiProgramNumber);
        }

        /// <summary>
        /// Original Generate method signature preserved for backward compatibility.
        /// Internally uses DrummerAgent.
        /// </summary>
        public static PartTrack Generate(
            BarTrack barTrack,
            SectionTrack sectionTrack,
            IReadOnlyList<SegmentGrooveProfile> segmentProfiles,
            GroovePresetDefinition groovePresetDefinition,
            int totalBars,
            int midiProgramNumber)
        {
            // Build minimal SongContext for DrummerAgent
            var songContext = new SongContext
            {
                BarTrack = barTrack,
                SectionTrack = sectionTrack,
                SegmentGrooveProfiles = segmentProfiles,
                GroovePresetDefinition = groovePresetDefinition
            };

            // Use new entry point
            return Generate(songContext);
        }

        /// <summary>
        /// Legacy anchor-based generation implementation.
        /// MVP: extracts anchor onsets and emits MIDI events with default velocity.
        /// Story 5: adds per-bar context building for section/phrase awareness.
        /// Story 6: adds role presence check (orchestration policy).
        /// Story 8: adds protection hierarchy merger.
        /// Story 9: adds protection enforcement (must-hits, never-remove, never-add).
        /// Story 10: applies subdivision grid filter to onsets.
        /// Story 11: applies syncopation/anticipation filter per role vocabulary.
        /// Story 12: applies phrase hook policy (protect anchors in phrase-end windows).
        /// </summary>
        private static PartTrack GenerateLegacyAnchorBasedInternal(
            BarTrack barTrack,
            SectionTrack sectionTrack,
            IReadOnlyList<SegmentGrooveProfile> segmentProfiles,
            GroovePresetDefinition groovePresetDefinition,
            int totalBars,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();

            // Story 5: Build per-bar context (section, phrase position, segment profile)
            var barContexts = BarContextBuilder.Build(sectionTrack, segmentProfiles, totalBars);

            // Story 8: Merge protection hierarchy layers per-bar using shared ProtectionPerBarBuilder
            var mergedProtections = ProtectionPerBarBuilder.Build(barContexts, groovePresetDefinition.ProtectionPolicy);

            // Story 12: Apply phrase hook policy (protect anchors near phrase/section ends) using shared PhraseHookProtectionAugmenter
            PhraseHookProtectionAugmenter.Augment(
                mergedProtections,
                barContexts,
                groovePresetDefinition.ProtectionPolicy?.PhraseHookPolicy,
                groovePresetDefinition.Identity.BeatsPerBar);

            // Story 2: Extract anchor patterns from GroovePreset per bar (drum-specific)
            var allOnsets = ExtractAnchorOnsets(groovePresetDefinition, totalBars);

            // Story 10: Filter onsets by allowed subdivision grid using shared OnsetGrid
            var subdivisionPolicy = groovePresetDefinition.ProtectionPolicy?.SubdivisionPolicy;
            if (subdivisionPolicy != null)
            {
                var grid = OnsetGridBuilder.Build(groovePresetDefinition.Identity.BeatsPerBar, subdivisionPolicy.AllowedSubdivisions);
                allOnsets = allOnsets.Where(o => grid.IsAllowed(o.Beat)).ToList();
            }

            // Story 11 : Filter onsets by syncopation/anticipation rules using shared RhythmVocabularyFilter
            allOnsets = RhythmVocabularyFilter.Filter(
                allOnsets,
                getRoleName: onset => onset.Role.ToString(),
                getBeat: onset => onset.Beat,
                beatsPerBar: groovePresetDefinition.Identity.BeatsPerBar,
                roleConstraintPolicy: groovePresetDefinition.ProtectionPolicy?.RoleConstraintPolicy);

            // Story 6: Filter onsets by role presence (orchestration policy) using shared RolePresenceGate
            var barContextDict = barContexts?.ToDictionary(ctx => ctx.BarNumber) ?? new Dictionary<int, BarContext>();
            var filteredOnsets = new List<DrumOnset>();
            foreach (var onset in allOnsets)
            {
                if (!barContextDict.TryGetValue(onset.BarNumber, out var barCtx))
                {
                    filteredOnsets.Add(onset);
                    continue;
                }

                string sectionType = barCtx.Section?.SectionType.ToString() ?? string.Empty;
                if (RolePresenceGate.IsRolePresent(sectionType, onset.Role.ToString(), groovePresetDefinition.ProtectionPolicy?.OrchestrationPolicy))
                    filteredOnsets.Add(onset);
            }

            // Use generic ProtectionApplier to enforce protections on DrumOnset events.
            var enforcedOnsets = ProtectionApplier.Apply(
                filteredOnsets,
                mergedProtections,
                getBar: o => o.BarNumber,
                getRoleName: o => o.Role.ToString(),
                getBeat: o => o.Beat,
                // setFlags: mutate DrumOnset flags and return it
                setFlags: (o, isMustHit, isNeverRemove, isProtected) =>
                {
                    o.IsMustHit = isMustHit || o.IsMustHit;
                    o.IsNeverRemove = isNeverRemove || o.IsNeverRemove;
                    o.IsProtected = isProtected || o.IsProtected;
                    return o;
                },
                // createEvent: create a new DrumOnset for missing MustHit onsets
                createEvent: (bar, roleName, beat) =>
                {
                    if (!Enum.TryParse<DrumRole>(roleName, ignoreCase: true, out var parsedRole))
                        parsedRole = DrumRole.Kick; // fallback though role names should match
                    return new DrumOnset(parsedRole, bar, beat, Velocity: 100, TickPosition: 0);
                });

            // Story 3: Convert onsets to MIDI events
            ConvertOnsetsToMidiEvents(enforcedOnsets, barTrack, notes);

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        // AI: ExtractAnchorOnsets reads kick/snare/hat patterns from GroovePreset anchor layer per bar; returns DrumOnset list with beat positions and default velocity.
        private static List<DrumOnset> ExtractAnchorOnsets(GroovePresetDefinition groovePresetDefinition, int totalBars)
        {
            var allOnsets = new List<DrumOnset>();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = groovePresetDefinition.GetActiveGroovePreset(bar);
                var anchorLayer = groovePreset.AnchorLayer;

                foreach (var beat in anchorLayer.KickOnsets)
                {
                    allOnsets.Add(new DrumOnset(
                        Role: DrumRole.Kick,
                        BarNumber: bar,
                        Beat: beat,
                        Velocity: 100,
                        TickPosition: 0));
                }

                foreach (var beat in anchorLayer.SnareOnsets)
                {
                    allOnsets.Add(new DrumOnset(
                        Role: DrumRole.Snare,
                        BarNumber: bar,
                        Beat: beat,
                        Velocity: 100,
                        TickPosition: 0));
                }

                foreach (var beat in anchorLayer.HatOnsets)
                {
                    allOnsets.Add(new DrumOnset(
                        Role: DrumRole.ClosedHat,
                        BarNumber: bar,
                        Beat: beat,
                        Velocity: 100,
                        TickPosition: 0));
                }
            }

            return allOnsets;
        }

        // AI: ConvertOnsetsToMidiEvents converts DrumOnset list to PartTrackEvent notes using BarTrack for tick conversion.
        private static void ConvertOnsetsToMidiEvents(List<DrumOnset> onsets, BarTrack barTrack, List<PartTrackEvent> notes)
        {
            if (onsets == null) return;
            if (barTrack == null) throw new ArgumentNullException(nameof(barTrack));
            if (notes == null) throw new ArgumentNullException(nameof(notes));

            foreach (var onset in onsets)
            {
                long absoluteTick = barTrack.ToTick(onset.BarNumber, onset.Beat);
                int midiNote = GetMidiNoteNumber(onset.Role);

                var noteEvent = new PartTrackEvent(
                    noteNumber: midiNote,
                    absoluteTimeTicks: (int)absoluteTick,
                    noteDurationTicks: 60,
                    noteOnVelocity: onset.Velocity);

                notes.Add(noteEvent);
            }

            notes.Sort((a, b) => a.AbsoluteTimeTicks.CompareTo(b.AbsoluteTimeTicks));
        }

        // AI: GetMidiNoteNumber maps DrumRole to General MIDI note; throws for unknown roles.
        public static int GetMidiNoteNumber(DrumRole role) => role switch
        {
            DrumRole.Kick => KickMidiNote,
            DrumRole.Snare => SnareMidiNote,
            DrumRole.ClosedHat => ClosedHatMidiNote,
            DrumRole.OpenHat => OpenHatMidiNote,
            DrumRole.Ride => RideMidiNote,
            DrumRole.Crash => CrashMidiNote,
            DrumRole.TomHigh => TomHighMidiNote,
            DrumRole.TomMid => TomMidMidiNote,
            DrumRole.TomLow => TomLowMidiNote,
            _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unknown drum role: {role}")
        };

        // AI: GetDrumProgramNumber retrieves drum MIDI program from VoiceSet (defaults to 0 for standard GM drums).
        private static int GetDrumProgramNumber(VoiceSet voiceSet)
        {
            // GM drums are on channel 10 (MIDI track 10) and typically use program 0
            // Look for a voice with "Drum" in the name
            var drumVoice = voiceSet.Voices.FirstOrDefault(v =>
                v.VoiceName.Contains("Drum", StringComparison.OrdinalIgnoreCase) ||
                v.GrooveRole == GrooveRoles.DrumKit);

            // GM drums default to program 0 (Standard Kit)
            return drumVoice != null ? 0 : 0;
        }
    }
}
