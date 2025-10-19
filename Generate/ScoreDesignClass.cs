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

        // Design Space
        public VoiceSetClass VoiceSet { get; } = new();
        public ChordSetClass ChordSet { get; } = new();
        public SectionSetClass Sections { get; } = new();

        // Actual Design - not started yet

        public ScoreDesignClass(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }


        // TODO  - get rid of these!!!
        //public sealed record Voice(string Id, string Value);
        public sealed record Chord(string Id, Step RootStep, int RootAlter, ChordKind Kind, Step? BassStep, int? BassAlter, string Name);

        public enum Step { A, B, C, D, E, F, G }
        public enum ChordKind
        {
            Major, Minor, Augmented, Diminished, DominantSeventh, MajorSeventh, MinorSeventh,
            SuspendedFourth, SuspendedSecond, Power, HalfDiminishedSeventh, DiminishedSeventh
        }
    }
}