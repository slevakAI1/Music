using System;
using System.Collections.Generic;
using Music.Generate;

namespace Music.Design
{
    /// <summary>
    /// Minimal, score-wide structure (top-level only). No voice/staff/part targeting.
    /// </summary>
    public sealed class ScoreDesignClass
    {
        public string DesignId { get; }

        // Design Space
        public VoiceSetClass VoiceSet { get; } = new();

        public SectionSetClass SectionSet { get; } = new();

        // Harmonic timeline persisted with the design
        public HarmonicTimeline? HarmonicTimeline { get; set; }

        public ScoreDesignClass(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }
    }
}