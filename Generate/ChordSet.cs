using System;
using System.Collections.Generic;

namespace Music.Generate
{
    // Holds the collection of chords (MusicXML harmony-compatible)
    public sealed class ChordSet
    {
        private readonly List<ScoreDesign.Chord> _chords = new();
        public IReadOnlyList<ScoreDesign.Chord> Chords => _chords;

        public void Reset() => _chords.Clear();

        public ScoreDesign.Chord AddChord(
            ScoreDesign.Step rootStep,
            int rootAlter,
            ScoreDesign.ChordKind kind,
            ScoreDesign.Step? bassStep = null,
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

            var chord = new ScoreDesign.Chord(
                Id: Guid.NewGuid().ToString("N"),
                RootStep: rootStep,
                RootAlter: rootAlter,
                Kind: kind,
                BassStep: bassStep,
                BassAlter: bassAlter,
                Name: string.IsNullOrWhiteSpace(name)
                    ? BuildChordDisplayName(rootStep, rootAlter, kind, bassStep, bassAlter)
                    : name!);

            _chords.Add(chord);
            return chord;
        }

        public IReadOnlyList<ScoreDesign.Chord> AddDefaultChords()
        {
            AddChord(ScoreDesign.Step.C, 0, ScoreDesign.ChordKind.Major, name: "C");
            return Chords;
        }

        private static string BuildChordDisplayName(ScoreDesign.Step rootStep, int rootAlter, ScoreDesign.ChordKind chordKind, ScoreDesign.Step? bassStep, int? bassAlter)
        {
            static string StepToText(ScoreDesign.Step s) => s switch
            {
                ScoreDesign.Step.A => "A",
                ScoreDesign.Step.B => "B",
                ScoreDesign.Step.C => "C",
                ScoreDesign.Step.D => "D",
                ScoreDesign.Step.E => "E",
                ScoreDesign.Step.F => "F",
                ScoreDesign.Step.G => "G",
                _ => "?"
            };

            static string AlterToText(int alter) => alter switch
            {
                < 0 => new string('b', -alter),
                > 0 => new string('#', alter),
                _ => ""
            };

            static string KindToSuffix(ScoreDesign.ChordKind k) => k switch
            {
                ScoreDesign.ChordKind.Major => "",
                ScoreDesign.ChordKind.Minor => "m",
                ScoreDesign.ChordKind.Augmented => "aug",
                ScoreDesign.ChordKind.Diminished => "dim",
                ScoreDesign.ChordKind.DominantSeventh => "7",
                ScoreDesign.ChordKind.MajorSeventh => "maj7",
                ScoreDesign.ChordKind.MinorSeventh => "m7",
                ScoreDesign.ChordKind.SuspendedFourth => "sus4",
                ScoreDesign.ChordKind.SuspendedSecond => "sus2",
                ScoreDesign.ChordKind.Power => "5",
                ScoreDesign.ChordKind.HalfDiminishedSeventh => "m7b5",
                ScoreDesign.ChordKind.DiminishedSeventh => "dim7",
                _ => ""
            };

            var root = StepToText(rootStep) + AlterToText(rootAlter);
            var kindSuffix = KindToSuffix(chordKind);
            var bass = bassStep is null ? "" : "/" + StepToText(bassStep.Value) + (bassAlter.HasValue ? AlterToText(bassAlter.Value) : "");
            return root + kindSuffix + bass;
        }
    }
}