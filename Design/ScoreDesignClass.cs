using System;
using System.Collections.Generic;

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
        public SectionSetClass Sections { get; } = new();

        // Actual Design - not started yet

        public ScoreDesignClass(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }
    }
}