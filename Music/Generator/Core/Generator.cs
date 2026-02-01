// AI: purpose=Generate PartTrack using for Drums only, Section+Bar timing;
// AI: invariants=BarTrack is read-only and must NOT be rebuilt here.
// AI: deps=MusicConstants.TicksPerQuarterNote; DrumGenerator pipeline for drum generation.
// AI: perf=Single-run generation; avoid allocations in inner loops; use seed for deterministic results.
// AI: change=Story RF-3 replaces DrummerAgent.Generate() with DrumGenerator pipeline architecture.

using Music;
using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Music.Song.Material;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        // AI: Story RF-3: Original signature preserved for backward compatibility; uses DrumTrackGenerator fallback.
        public static PartTrack Generate(SongContext songContext)
        {
            return Generate(songContext, drummerStyle: null);
        }

        /// <summary>
        /// Generates a drum track from the song context using the optional drummer style configuration.
        /// Story RF-3: Wire DrumGenerator pipeline with DrummerAgent as data source.
        /// </summary>
        /// <param name="songContext">Song context with section, groove, and timing data.</param>
        /// <param name="drummerStyle">Optional style configuration for operator-based drum generation.
        /// When null, falls back to groove-only DrumTrackGenerator.</param>
        /// <param name="maxBars">Maximum number of bars to generate. When 0 (default), generates full song.
        /// When > 0, limits generation to first N bars.</param>
        /// <returns>Generated drum PartTrack.</returns>
        /// <remarks>
        /// <para>Architecture (Story RF-3):</para>
        /// <list type="bullet">
        ///   <item>When drummerStyle is provided: Creates DrummerAgent (data source) → passes to DrumGenerator (pipeline) → uses GrooveSelectionEngine for weighted selection</item>
        ///   <item>When drummerStyle is null: Falls back to existing DrumTrackGenerator (anchor patterns only)</item>
        /// </list>
        /// <para>Benefits of new architecture:</para>
        /// <list type="bullet">
        ///   <item>Enforces density targets from policy</item>
        ///   <item>Respects operator caps and weights</item>
        ///   <item>Uses GrooveSelectionEngine for proper weighted selection</item>
        ///   <item>Supports operator-based generation with physicality constraints</item>
        ///   <item>Memory system prevents robotic repetition</item>
        /// </list>
        /// </remarks>
        public static PartTrack Generate(SongContext songContext, StyleConfiguration? drummerStyle, int maxBars = 0)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GroovePresetDefinition);

            // When drummer style is provided, use custom style; otherwise use DrumTrackGenerator's default (PopRock)
            if (drummerStyle != null)
            {
                var agent = new DrummerAgent(drummerStyle);
                var generator = new DrumPhraseGenerator(agent, agent);
                return generator.Generate(songContext, maxBars);
            }

            return DrumTrackGenerator.Generate(songContext);
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

            var generator = new DrumGenerator(materialBank);
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
