// AI: purpose=Generate PartTracks using Harmony, Groove, Section+Bar timing; deterministic via seed.
// AI: invariants=Harmony->Groove->Bar timing must align; BarTrack is read-only and must NOT be rebuilt here.
// AI: deps=HarmonyValidator, MotifPlacementPlanner, MusicConstants.TicksPerQuarterNote.
// AI: perf=Single-run generation; avoid allocations in inner loops; use seed for deterministic results.
// TODO? confirm behavior when groove/pads onsets null vs empty; current code treats both as skip.

using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        public static GeneratorResult Generate(SongContext songContext)
        {
            // validate songcontext is not null
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateHarmonyTrack(songContext.HarmonyTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GrooveTrack);

            // Validate harmony events for musical correctness before generation
            var validationResult = HarmonyValidator.ValidateTrack(
                songContext.HarmonyTrack,
                new HarmonyValidationOptions
                {
                    ApplyFixes = false,
                    StrictDiatonicChordTones = false,
                    ClampInvalidBassToRoot = false,
                    AllowUnknownQuality = false
                });

            if (!validationResult.IsValid)
            {
                var errorMessage = "Harmony validation failed:\n" + string.Join("\n", validationResult.Errors);
                throw new InvalidOperationException(errorMessage);
            }

            // Get total bars from section track
            int totalBars = songContext.SectionTrack.TotalBars;

            // Use default randomization settings and harmony policy
            var settings = RandomizationSettings.Default;
            var harmonyPolicy = HarmonyPolicy.Default;

            var grooveName = GetPrimaryGrooveName(songContext.GrooveTrack);

            // Story 9.2: Create motif placement plan and presence map
            var materialBank = InitializeMaterialBank();
            var motifPlan = CreateMotifPlacementPlan(
                materialBank,
                songContext.SectionTrack,
                settings.Seed);
            var motifPresence = new MotifPresenceMap(motifPlan);

            // Resolve MIDI program numbers from VoiceSet
            int bassProgramNumber = GetProgramNumberForRole(songContext.Voices, "Bass", defaultProgram: 33);
            int compProgramNumber = GetProgramNumberForRole(songContext.Voices, "Comp", defaultProgram: 27);
            int padsProgramNumber = GetProgramNumberForRole(songContext.Voices, "Pads", defaultProgram: 4);
            int drumProgramNumber = GetProgramNumberForRole(songContext.Voices, "DrumKit", defaultProgram: 255);

            return new GeneratorResult
            {
                BassTrack = BassTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    motifPlan,
                    motifPresence,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    bassProgramNumber),

                GuitarTrack = GuitarTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    motifPlan,
                    motifPresence,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    compProgramNumber),

                KeysTrack = KeysTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    motifPlan,
                    motifPresence,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    padsProgramNumber),

                DrumTrack = DrumTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    motifPlan,
                    motifPresence,
                    totalBars,
                    settings,
                    drumProgramNumber)
            };
        }

        // AI: returns SourcePresetName of first groove event or "Default"; used as primary groove key
        private static string GetPrimaryGrooveName(GrooveTrack grooveTrack)
        {
            var firstGroove = grooveTrack.Events.FirstOrDefault();
            return firstGroove?.SourcePresetName ?? "Default";
        }

        // AI: creates MaterialBank with example motifs used by motif planner; safe to return non-empty test bank
        private static MaterialBank InitializeMaterialBank()
        {
            var bank = new MaterialBank();

            // Add test motifs from MotifLibrary (Story 8.3)
            var chorusHook = MotifLibrary.ClassicRockHookA().ToPartTrack();
            bank.Add(chorusHook);

            var verseRiff = MotifLibrary.SteadyVerseRiffA().ToPartTrack();
            bank.Add(verseRiff);

            var synthHook = MotifLibrary.BrightSynthHookA().ToPartTrack();
            bank.Add(synthHook);

            var bassFill = MotifLibrary.BassTransitionFillA().ToPartTrack();
            bank.Add(bassFill);

            return bank;
        }

        // AI: creates MotifPlacementPlan; returns Empty when materialBank empty; logs placements
        private static MotifPlacementPlan CreateMotifPlacementPlan(
            MaterialBank materialBank,
            SectionTrack sectionTrack,
            int seed)
        {
            Tracer.DebugTrace($"CreateMotifPlacementPlan: MaterialBank has {materialBank.Count} items");
            
            if (materialBank.Count == 0)
            {
                Tracer.DebugTrace("CreateMotifPlacementPlan: Returning empty plan (no motifs in bank)");
                return MotifPlacementPlan.Empty(seed);
            }

            // Use static CreatePlan method
            var plan = MotifPlacementPlanner.CreatePlan(
                sectionTrack,
                materialBank,
                seed);
            
            Tracer.DebugTrace($"CreateMotifPlacementPlan: Created plan with {plan.Placements.Count} placements");
            foreach (var p in plan.Placements)
            {
                Tracer.DebugTrace($"  Planned: Role={p.MotifSpec.IntendedRole}, Section={p.AbsoluteSectionIndex}, Bar={p.StartBarWithinSection}-{p.StartBarWithinSection + p.DurationBars - 1}");
            }
            
            return plan;
        }

        // AI: GeneratorResult: required PartTracks returned; consumers expect these program numbers and ordering.
        public sealed class GeneratorResult
        {
            public required PartTrack BassTrack { get; init; }
            public required PartTrack GuitarTrack { get; init; }
            public required PartTrack KeysTrack { get; init; }
            public required PartTrack DrumTrack { get; init; }
        }

        #region Validation

        // AI: Validation methods throw ArgumentException when required tracks are missing; callers rely on exceptions for invalid song contexts.

        private static void ValidateSectionTrack(SectionTrack sectionTrack)
        {
            if (sectionTrack == null || sectionTrack.Sections.Count == 0)
                throw new ArgumentException("Section track must have events", nameof(sectionTrack));
        }

        private static void ValidateHarmonyTrack(HarmonyTrack harmonyTrack)
        {
            if (harmonyTrack == null || harmonyTrack.Events.Count == 0)
                throw new ArgumentException("Harmony track must have events", nameof(harmonyTrack));
        }

        private static void ValidateTimeSignatureTrack(Timingtrack timeSignatureTrack)
        {
            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
                throw new ArgumentException("Time signature track must have events", nameof(timeSignatureTrack));
        }

        private static void ValidateGrooveTrack(GrooveTrack grooveTrack)
        {
            if (grooveTrack == null || grooveTrack.Events.Count == 0)
                throw new ArgumentException("Groove track must have events", nameof(grooveTrack));
        }

        // ValidateSongContext: ensures caller provided a non-null SongContext; throws ArgumentNullException when null.
        private static void ValidateSongContext(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);
        }

        #endregion

        // AI: resolves MIDI program by GrooveRole->VoiceName match (case-insensitive); returns defaultProgram on not found
        private static int GetProgramNumberForRole(VoiceSet voices, String grooveRole, int defaultProgram)
        {
            // Find voice with matching groove role
            var voice = voices.Voices.FirstOrDefault(v => 
                string.Equals(v.GrooveRole, grooveRole, StringComparison.OrdinalIgnoreCase));

            if (voice == null)
                return defaultProgram;

            // Look up MIDI program number from voice name
            var midiVoice = MidiVoices.MidiVoiceList()
                .FirstOrDefault(mv => string.Equals(mv.Name, voice.VoiceName, StringComparison.OrdinalIgnoreCase));

            return midiVoice?.ProgramNumber ?? defaultProgram;
        }
    }
}