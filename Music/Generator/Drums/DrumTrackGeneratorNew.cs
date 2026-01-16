// AI: purpose=Generate drum track from GroovePresetDefinition anchor patterns; MVP for Story 1 of GroovePlan.
// AI: deps=GrooveTrack for preset lookup; BarTrack for tick conversion; returns PartTrack sorted by AbsoluteTimeTicks.
// AI: change=Phase 1 MVP; subsequent stories add velocity shaping, timing, variations per GroovePlan.md.

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
    public sealed record DrumOnset(
        DrumRole Role,
        int BarNumber,
        decimal Beat,
        int Velocity,
        long TickPosition);

    // AI: DrumBarContext provides per-bar context for Story 5; section, phrase position, segment profile for downstream logic.
    // AI: invariants=BarNumber is 1-based; BarWithinSection is 0-based; BarsUntilSectionEnd is >= 0.
    public sealed record DrumBarContext(
        int BarNumber,
        Section? Section,
        SegmentGrooveProfile? SegmentProfile,
        int BarWithinSection,
        int BarsUntilSectionEnd);

    internal static class DrumTrackGeneratorNew
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
        /// Generates drum track from groove preset anchor patterns.
        /// MVP: extracts anchor onsets and emits MIDI events with default velocity.
        /// Story 5: adds per-bar context building for section/phrase awareness.
        /// Story 6: adds role presence check (orchestration policy).
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            IReadOnlyList<SegmentGrooveProfile> segmentProfiles,
            GroovePresetDefinition groovePresetDefinition,
            int totalBars,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();

            // Story 5: Build per-bar context (section, phrase position, segment profile)
            var barContexts = BuildBarContexts(sectionTrack, segmentProfiles.ToList(), totalBars);

            // Story 2: Extract anchor patterns from GroovePreset per bar
            var allOnsets = ExtractAnchorOnsets(grooveTrack, totalBars);

            // Story 6: Filter onsets by role presence (orchestration policy)
            var filteredOnsets = ApplyRolePresenceFilter(allOnsets, barContexts, groovePresetDefinition.ProtectionPolicy.OrchestrationPolicy);

            // Story 3: Convert onsets to MIDI events
            ConvertOnsetsToMidiEvents(filteredOnsets, barTrack, notes);

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        // AI: ExtractAnchorOnsets reads kick/snare/hat patterns from GroovePreset anchor layer per bar; returns DrumOnset list with beat positions and default velocity.
        private static List<DrumOnset> ExtractAnchorOnsets(GrooveTrack grooveTrack, int totalBars)
        {
            var allOnsets = new List<DrumOnset>();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);
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

        // AI: BuildBarContexts builds per-bar context for Story 5; maps bar → section, calculates phrase position, resolves segment profile.
        // AI: deps=SectionTrack.GetActiveSection; SegmentGrooveProfiles list from song context.
        private static List<DrumBarContext> BuildBarContexts(
            SectionTrack sectionTrack,
            List<SegmentGrooveProfile> segmentProfiles,
            int totalBars)
        {
            var contexts = new List<DrumBarContext>(totalBars);

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Map bar to section
                sectionTrack.GetActiveSection(bar, out var section);

                // Calculate phrase position within section
                int barWithinSection = 0;
                int barsUntilSectionEnd = 0;

                if (section != null)
                {
                    barWithinSection = bar - section.StartBar;
                    int sectionEndBar = section.StartBar + section.BarCount - 1;
                    barsUntilSectionEnd = sectionEndBar - bar;
                }

                // Resolve segment profile for this bar
                SegmentGrooveProfile? segmentProfile = null;
                foreach (var profile in segmentProfiles)
                {
                    bool inRange = true;

                    if (profile.StartBar.HasValue && bar < profile.StartBar.Value)
                        inRange = false;

                    if (profile.EndBar.HasValue && bar > profile.EndBar.Value)
                        inRange = false;

                    if (inRange)
                    {
                        segmentProfile = profile;
                        break; // Use first matching profile
                    }
                }

                contexts.Add(new DrumBarContext(
                    BarNumber: bar,
                    Section: section,
                    SegmentProfile: segmentProfile,
                    BarWithinSection: barWithinSection,
                    BarsUntilSectionEnd: barsUntilSectionEnd));
            }

            return contexts;
        }

        // AI: ApplyRolePresenceFilter removes onsets for roles disabled by orchestration policy per section; Story 6 implementation.
        // AI: deps=GrooveOrchestrationPolicy.DefaultsBySectionType; returns filtered onset list with only present roles.
        private static List<DrumOnset> ApplyRolePresenceFilter(
            List<DrumOnset> onsets,
            List<DrumBarContext> barContexts,
            GrooveOrchestrationPolicy orchestrationPolicy)
        {
            var filtered = new List<DrumOnset>();
            var barContextDict = barContexts.ToDictionary(ctx => ctx.BarNumber);

            foreach (var onset in onsets)
            {
                // Look up bar context
                if (!barContextDict.TryGetValue(onset.BarNumber, out var barContext))
                {
                    // No context for this bar, default to present
                    filtered.Add(onset);
                    continue;
                }

                // Get section type (use SectionType enum value as string)
                string sectionType = barContext.Section?.SectionType.ToString() ?? "";

                // Find matching orchestration defaults for this section type
                var sectionDefaults = orchestrationPolicy.DefaultsBySectionType
                    .FirstOrDefault(d => string.Equals(d.SectionType, sectionType, StringComparison.OrdinalIgnoreCase));

                // Check if role is present (default to true if not specified)
                bool isRolePresent = true;

                if (sectionDefaults != null)
                {
                    string roleName = onset.Role.ToString();

                    // Check individual role first (Kick, Snare, Hat, etc.)
                    if (sectionDefaults.RolePresent.TryGetValue(roleName, out bool rolePresent))
                    {
                        isRolePresent = rolePresent;
                    }
                    // Also check DrumKit (master switch for all drums)
                    else if (sectionDefaults.RolePresent.TryGetValue("DrumKit", out bool drumKitPresent))
                    {
                        isRolePresent = drumKitPresent;
                    }
                }

                if (isRolePresent)
                {
                    filtered.Add(onset);
                }
            }

            return filtered;
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
    }
}
