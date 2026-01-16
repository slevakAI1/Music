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
        /// Story 8: adds protection hierarchy merger.
        /// Story 9: adds protection enforcement (must-hits, never-remove, never-add).
        /// </summary>
        public static PartTrack Generate(
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

            // Story 8: Merge protection hierarchy layers
            var mergedProtections = MergeProtectionLayersPerBar(barContexts, groovePresetDefinition.ProtectionPolicy);

            // Story 2: Extract anchor patterns from GroovePreset per bar
            var allOnsets = ExtractAnchorOnsets(groovePresetDefinition, totalBars);

            // Story 6: Filter onsets by role presence (orchestration policy)
            var filteredOnsets = ApplyRolePresenceFilter(allOnsets, barContexts, groovePresetDefinition.ProtectionPolicy.OrchestrationPolicy);

            // Story 9: Enforce protections (add must-hits, mark protected, remove never-adds)
            var enforcedOnsets = EnforceProtections(filteredOnsets, mergedProtections);

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

        // AI: MergeProtectionLayersPerBar merges protection hierarchy for each bar using enabled tags from segment profile.
        // AI: deps=ProtectionPolicyMerger.MergeProtectionLayers; returns dictionary of bar → role → merged protections.
        private static Dictionary<int, Dictionary<string, RoleProtectionSet>> MergeProtectionLayersPerBar(
            List<DrumBarContext> barContexts,
            GrooveProtectionPolicy protectionPolicy)
        {
            var result = new Dictionary<int, Dictionary<string, RoleProtectionSet>>();

            foreach (var barContext in barContexts)
            {
                // Get enabled protection tags from segment profile (or use empty list)
                var enabledTags = barContext.SegmentProfile?.EnabledProtectionTags ?? new List<string>();

                // Merge protection layers for this bar
                var mergedProtections = ProtectionPolicyMerger.MergeProtectionLayers(
                    protectionPolicy,
                    enabledTags);

                result[barContext.BarNumber] = mergedProtections;
            }

            return result;
        }

        // AI: EnforceProtections applies merged protections to onset pool (Story 9).
        // AI: Ensures MustHitOnsets present, marks NeverRemove/Protected flags, filters NeverAddOnsets.
        // AI: Returns deduplicated onset list with protection flags set.
        private static List<DrumOnset> EnforceProtections(
            List<DrumOnset> onsets,
            Dictionary<int, Dictionary<string, RoleProtectionSet>> mergedProtectionsPerBar)
        {
            var result = new List<DrumOnset>();

            // Group existing onsets by bar
            var onsetsByBar = onsets.GroupBy(o => o.BarNumber).ToDictionary(g => g.Key, g => g.ToList());

            // Process each bar that has protections
            foreach (var (bar, protectionsByRole) in mergedProtectionsPerBar)
            {
                var barOnsets = onsetsByBar.TryGetValue(bar, out var existing) ? new List<DrumOnset>(existing) : new List<DrumOnset>();

                // Process each role's protections
                foreach (var (roleName, protectionSet) in protectionsByRole)
                {
                    // Parse role name to DrumRole enum
                    if (!Enum.TryParse<DrumRole>(roleName, ignoreCase: true, out var drumRole))
                        continue;

                    // Remove onsets in NeverAddOnsets
                    if (protectionSet.NeverAddOnsets != null && protectionSet.NeverAddOnsets.Count > 0)
                    {
                        barOnsets.RemoveAll(o => o.Role == drumRole && protectionSet.NeverAddOnsets.Contains(o.Beat));
                    }

                    // Mark existing onsets that are protected or never-remove
                    foreach (var onset in barOnsets.Where(o => o.Role == drumRole))
                    {
                        if (protectionSet.NeverRemoveOnsets != null && protectionSet.NeverRemoveOnsets.Contains(onset.Beat))
                            onset.IsNeverRemove = true;

                        if (protectionSet.ProtectedOnsets != null && protectionSet.ProtectedOnsets.Contains(onset.Beat))
                            onset.IsProtected = true;
                    }

                    // Add missing MustHitOnsets
                    if (protectionSet.MustHitOnsets != null)
                    {
                        foreach (var mustBeat in protectionSet.MustHitOnsets)
                        {
                            bool exists = barOnsets.Any(o => o.Role == drumRole && o.Beat == mustBeat);
                            if (!exists)
                            {
                                var newOnset = new DrumOnset(drumRole, bar, mustBeat, Velocity: 100, TickPosition: 0)
                                {
                                    IsMustHit = true,
                                    IsNeverRemove = protectionSet.NeverRemoveOnsets?.Contains(mustBeat) ?? false,
                                    IsProtected = protectionSet.ProtectedOnsets?.Contains(mustBeat) ?? false
                                };
                                barOnsets.Add(newOnset);
                            }
                        }
                    }
                }

                result.AddRange(barOnsets);
            }

            // Add onsets from bars without protections
            var barsWithProtections = new HashSet<int>(mergedProtectionsPerBar.Keys);
            foreach (var (bar, barOnsets) in onsetsByBar)
            {
                if (!barsWithProtections.Contains(bar))
                    result.AddRange(barOnsets);
            }

            // Deduplicate by bar + role + beat
            var deduped = new List<DrumOnset>();
            var seen = new HashSet<string>();
            foreach (var onset in result.OrderBy(o => o.BarNumber).ThenBy(o => o.Beat))
            {
                string key = $"{onset.BarNumber}|{onset.Role}|{onset.Beat}";
                if (seen.Add(key))
                    deduped.Add(onset);
            }

            return deduped;
        }

        // AI: ApplyRolePresenceFilter removes onsets for roles disabled by orchestration policy per section; Story 6 implementation.
        // AI: deps=GrooveOrchestrationPolicy.DefaultsBySectionType; returns filtered onset list with only present roles.
        // AI: note=checks specific role names (ClosedHat, OpenHat, Kick, Snare) and DrumKit master switch.
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

                    // Check specific role name (ClosedHat, OpenHat, Kick, Snare, etc.)
                    if (sectionDefaults.RolePresent.TryGetValue(roleName, out bool rolePresent))
                    {
                        isRolePresent = rolePresent;
                    }
                    // Check DrumKit (master switch for all drums)
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
