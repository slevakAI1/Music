// AI: purpose=Generate PartTrack for drums using Section+Bar timing only.
// AI: invariants=BarTrack is read-only; do not rebuild or reorder SectionTrack here.
// AI: deps=Uses DrumTrackGenerator and MusicConstants.TicksPerQuarterNote; keep seed for determinism.
// AI: perf=Single-run generation; avoid large allocations in inner loops; preserve ordering.

using Music.Generator.Drums.Generation;
using Music.Generator.Groove;

namespace Music.Generator
{
    public static class SongGenerator
    {
        // AI: entry=Facade generate method: validate context then run default drum pipeline; fast-fail on invalid input
        public static PartTrack Generate(SongContext songContext)
        {
            return Generate(songContext, maxBars: 0);
        }

        // AI: entry=Validate context then generate drum PartTrack; maxBars limits phrase generation scope
        public static PartTrack Generate(SongContext songContext, int maxBars = 0)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GroovePresetDefinition);

            int drumProgramNumber = VoiceSet.GetDrumProgramNumber(songContext.Voices);
            var generator = new DrumPhraseGenerator();
            return generator.Generate(songContext, drumProgramNumber, maxBars);
        }

        // AI: entry=Generate PartTrack from material phrases; materialBank required
        public static PartTrack GenerateFromPhrases(
            SongContext songContext,
            int maxBars = 0)
            => GenerateFromPhrases(songContext, seed: 0, maxBars);

        // AI: entry=Phrase-based generation with explicit seed for deterministic mapping across sections
        public static PartTrack GenerateFromPhrases(
            SongContext songContext,
            int seed,
            int maxBars)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);

            var materialBank = songContext.MaterialBank
                ?? throw new ArgumentException("MaterialBank must be provided", nameof(songContext));

            var generator = new DrumTrackGenerator();
            return generator.Generate(songContext, seed, maxBars);
        }

        // AI: entry=Generate a simple groove preview PartTrack for audition; deterministic for same inputs
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


        // AI: validation=Throw ArgumentException/ArgumentNullException when required tracks or contexts missing
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

        // AI: helper=Ensure SongContext is non-null; callers depend on thrown exception for invalid input
        private static void ValidateSongContext(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);
        }

        // AI: validation=Ensure Groove preset and AnchorLayer present
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
