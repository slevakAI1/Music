using System;
using System.Collections.Generic;

namespace Music.Generate
{
    /// <summary>
    /// Minimal, score-wide structure (top-level only). No voice/staff/part targeting.
    /// </summary>
    public sealed class ScoreDesignClass
    {
        public string DesignId { get; }

        // Persist sets on the design (form should not own these)
        public VoiceSetClass VoiceSet { get; } = new();
        public ChordSetClass ChordSet { get; } = new();
        public SectionsClass Sections { get; } = new();

        // Legacy collections kept for compatibility with existing APIs
        private readonly List<Section> _sectionsLegacy = new();
        public IReadOnlyList<Section> SectionsLegacy => _sectionsLegacy;

        private readonly List<Voice> _voices = new();
        public IReadOnlyList<Voice> Voices => _voices;

        private readonly List<Chord> _chords = new();
        public IReadOnlyList<Chord> Chords => _chords;

        public ScoreDesignClass(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }

        // Legacy helpers (can be removed once fully migrated to *Set/SectionsClass)
        public void ResetSections() => _sectionsLegacy.Clear();

        public Section AddSection(SectionType type, MeasureRange span, string? name = null, IEnumerable<string>? tags = null)
        {
            var sec = new Section(
                Id: Guid.NewGuid().ToString("N"),
                Type: type,
                Span: span,
                Name: string.IsNullOrWhiteSpace(name) ? type.ToString() : name!,
                Tags: tags is null ? Array.Empty<string>() : new List<string>(tags).ToArray());

            _sectionsLegacy.Add(sec);
            return sec;
        }

        public Voice AddVoice(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Voice value must not be null or empty.", nameof(value));

            foreach (var v in _voices)
            {
                if (string.Equals(v.Value, value, StringComparison.Ordinal))
                    return v;
            }

            var voice = new Voice(
                Id: Guid.NewGuid().ToString("N"),
                Value: value);

            _voices.Add(voice);
            return voice;
        }

        public IReadOnlyList<Voice> AddVoices()
        {
            AddVoice("Guitar");
            AddVoice("Drum Set");
            AddVoice("Keyboard");
            AddVoice("Base Guitar");
            return Voices;
        }

        public Chord AddChord(
            Step rootStep,
            int rootAlter,
            ChordKind kind,
            Step? bassStep = null,
            int? bassAlter = null,
            string? name = null)
        {
            foreach (var c in _chords)
            {
                if (c.RootStep == rootStep &&
                    c.RootAlter == rootAlter &&
                    c.Kind == kind &&
                    c.BassStep == bassStep &&
                    c.BassAlter == bassAlter)
                {
                    return c;
                }
            }

            var chord = new Chord(
                Id: Guid.NewGuid().ToString("N"),
                RootStep: rootStep,
                RootAlter: rootAlter,
                Kind: kind,
                BassStep: bassStep,
                BassAlter: bassAlter,
                Name: string.IsNullOrWhiteSpace(name) ? BuildChordDisplayName(rootStep, rootAlter, kind, bassStep, bassAlter) : name!);

            _chords.Add(chord);
            return chord;
        }

        public IReadOnlyList<Chord> CreateChordSet()
        {
            AddChord(Step.C, 0, ChordKind.Major, name: "C");
            return Chords;
        }

        private static string BuildChordDisplayName(Step rootStep, int rootAlter, ChordKind chordKind, Step? bassStep, int? bassAlter)
        {
            static string StepToText(Step s) => s switch
            {
                Step.A => "A",
                Step.B => "B",
                Step.C => "C",
                Step.D => "D",
                Step.E => "E",
                Step.F => "F",
                Step.G => "G",
                _ => "?"
            };

            static string AlterToText(int alter) => alter switch
            {
                < 0 => new string('b', -alter),
                > 0 => new string('#', alter),
                _ => ""
            };

            static string KindToSuffix(ChordKind k) => k switch
            {
                ChordKind.Major => "",
                ChordKind.Minor => "m",
                ChordKind.Augmented => "aug",
                ChordKind.Diminished => "dim",
                ChordKind.DominantSeventh => "7",
                ChordKind.MajorSeventh => "maj7",
                ChordKind.MinorSeventh => "m7",
                ChordKind.SuspendedFourth => "sus4",
                ChordKind.SuspendedSecond => "sus2",
                ChordKind.Power => "5",
                ChordKind.HalfDiminishedSeventh => "m7b5",
                ChordKind.DiminishedSeventh => "dim7",
                _ => ""
            };

            var root = StepToText(rootStep) + AlterToText(rootAlter);
            var kindSuffix = KindToSuffix(chordKind);
            var bass = bassStep is null ? "" : "/" + StepToText(bassStep.Value) + (bassAlter.HasValue ? AlterToText(bassAlter.Value) : "");
            return root + kindSuffix + bass;
        }

        public readonly record struct MeasureRange(int StartMeasure, int? EndMeasure, bool InclusiveEnd = true)
        {
            public bool IsOpenEnded => EndMeasure is null;
            public static MeasureRange Single(int measure) => new(measure, measure, true);
        }

        public sealed record Section(string Id, SectionType Type, MeasureRange Span, string Name, string[] Tags);
        public sealed record Voice(string Id, string Value);
        public sealed record Chord(string Id, Step RootStep, int RootAlter, ChordKind Kind, Step? BassStep, int? BassAlter, string Name);

        public enum SectionType { Intro, Verse, Chorus, Solo, Bridge, Outro, Custom }
        public enum Step { A, B, C, D, E, F, G }
        public enum ChordKind
        {
            Major, Minor, Augmented, Diminished, DominantSeventh, MajorSeventh, MinorSeventh,
            SuspendedFourth, SuspendedSecond, Power, HalfDiminishedSeventh, DiminishedSeventh
        }
    }
}