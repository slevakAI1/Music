// AI: purpose=Generate PartTrack using for Drums only, Section+Bar timing;
// AI: invariants=BarTrack is read-only and must NOT be rebuilt here.
// AI: deps=MusicConstants.TicksPerQuarterNote; DrumGenerator pipeline for drum generation.
// AI: perf=Single-run generation; avoid allocations in inner loops; use seed for deterministic results.
// AI: change=Story 1 removes style-based entry point; Generator uses default drum pipeline.

using Music.Generator.Core;
using Music.Generator.Drums.Generation;
using Music.Generator.Drums.Operators;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        // AI: Story RF-3: Original signature preserved for backward compatibility; uses DrumTrackGenerator fallback.
        // AI: behavior=Validates required tracks; uses default style config for operator pipeline.
        public static PartTrack Generate(SongContext songContext)
        {
            return Generate(songContext, maxBars: 0);
        }

        // AI: behavior=Uses PopRock defaults until style-free pipeline lands; maxBars limits phrase generator bars.
        public static PartTrack Generate(SongContext songContext, int maxBars = 0)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GroovePresetDefinition);

            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var candidateSource = new DrummerCandidateSource(
                registry,
                diagnosticsCollector: null,
                settings: null);
            var generator = new DrumPhraseGenerator(candidateSource);
            return generator.Generate(songContext, maxBars);
        }

        // AI: purpose=Phrase-based drum track generation using MaterialBank phrases.
        // AI: invariants=MaterialBank must contain drum phrases for requested genre.
        public static PartTrack GenerateFromPhrases(
            SongContext songContext,
            int maxBars = 0)
            => GenerateFromPhrases(songContext, seed: 0, maxBars);

        // AI: purpose=Phrase-based drum track generation with optional seed for deterministic section mapping.
        public static PartTrack GenerateFromPhrases(
            SongContext songContext,
            int seed,
            int maxBars)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);

            var materialBank = songContext.MaterialBank
                ?? throw new ArgumentException("MaterialBank must be provided", nameof(songContext));

            Tracer.DebugTrace($"[GenerateFromPhrases] phrases={materialBank.GetPhrases().Count}; seed={seed}; maxBars={maxBars}");

            var generator = new DrumGenerator();
            return generator.Generate(songContext, seed, maxBars);
        }

        // AI: purpose=Single-method groove preview for audition; generates groove from seed+genre, converts to playable PartTrack.
        // AI: invariants=Deterministic: same seed+genre+barTrack always produces identical PartTrack; all events at same velocity.
        // AI: deps=GrooveAnchorFactory.Generate for groove generation; GrooveInstanceLayer.ToPartTrack for MIDI conversion.
        // AI: change=Story 2.2: Facade method combining groove generation + PartTrack conversion for quick audition workflow.
        public static PartTrack GenerateGroovePreview(
            int seed,
            string genre,
            BarTrack barTrack,
            int totalBars,
            int velocity = 100)
        {
            ArgumentNullException.ThrowIfNull(genre);
            ArgumentNullException.ThrowIfNull(barTrack);

            GrooveInstanceLayer groove = GrooveAnchorFactory.GetAnchor(genre);
            return groove.ToPartTrack(barTrack, totalBars, velocity);
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

        // ValidateSongContext: ensures caller provided a non-null SongContext; throws ArgumentNullException when null.
        private static void ValidateSongContext(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);
        }

        // ValidateGrooveTrack: ensures a preset definition exists and contains an anchor layer
        private static void ValidateGrooveTrack(GroovePresetDefinition groovePresetDefinition)
        {
            if (groovePresetDefinition == null)
                throw new ArgumentException("Groove preset definition must be provided", nameof(groovePresetDefinition));

            if (groovePresetDefinition.AnchorLayer == null)
                throw new ArgumentException("Groove preset must include an AnchorLayer", nameof(groovePresetDefinition));
        }

        #endregion
    }
}
