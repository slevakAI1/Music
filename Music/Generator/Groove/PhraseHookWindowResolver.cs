// AI: purpose=Resolve phrase/section-end "fill window" info for a bar; shared helper for generators.
// AI: invariants=Pure function; returns same booleans as previous inline logic; no side-effects.
// AI: deps=BarContext.BarsUntilSectionEnd; GroovePhraseHookPolicy fields; returns EnabledFillTags for consumers.

namespace Music.Generator.Groove
{
    /// <summary>
    /// DTO containing phrase/section-end window flags and enabled fill tags for a bar.
    /// Returned by PhraseHookWindowResolver.Resolve().
    /// </summary>
    // AI: immutable DTO; consumers check flags to gate fill candidates or protect anchors.
    public sealed record PhraseHookWindowInfo(
        bool InPhraseEndWindow,
        bool InSectionEndWindow,
        IReadOnlyList<string> EnabledFillTags);

    /// <summary>
    /// Resolves whether a bar is in phrase-end or section-end fill windows using policy and bar context.
    /// Extracted from DrumTrackGenerator.ApplyPhraseHookPolicyToProtections for cross-generator reuse.
    /// </summary>
    // AI: stateless resolver; deterministic; logic identical to DrumTrackGenerator phrase window computation.
    public static class PhraseHookWindowResolver
    {
        /// <summary>
        /// Determines fill window status for a bar based on phrase hook policy and bar context.
        /// </summary>
        /// <param name="ctx">Bar context containing section position info.</param>
        /// <param name="policy">Phrase hook policy with window sizes and fill tag configuration.</param>
        /// <returns>Window info with flags and enabled fill tags; empty tags if policy null.</returns>
        // AI: null policy returns all-false flags and empty tag list; non-null policy computes windows via BarsUntilSectionEnd.
        public static PhraseHookWindowInfo Resolve(BarContext ctx, GroovePhraseHookPolicy? policy)
        {
            if (policy == null)
                return new PhraseHookWindowInfo(
                    InPhraseEndWindow: false,
                    InSectionEndWindow: false,
                    EnabledFillTags: new List<string>());

            // Compute phrase-end window: true when AllowFillsAtPhraseEnd=false and bar within window size
            bool inPhraseEndWindow = policy.AllowFillsAtPhraseEnd == false
                && policy.PhraseEndBarsWindow > 0
                && ctx.BarsUntilSectionEnd >= 0
                && ctx.BarsUntilSectionEnd < policy.PhraseEndBarsWindow;

            // Compute section-end window: true when AllowFillsAtSectionEnd=false and bar within window size
            bool inSectionEndWindow = policy.AllowFillsAtSectionEnd == false
                && policy.SectionEndBarsWindow > 0
                && ctx.BarsUntilSectionEnd >= 0
                && ctx.BarsUntilSectionEnd < policy.SectionEndBarsWindow;

            // Return enabled fill tags from policy (or empty list if null)
            var enabledTags = (IReadOnlyList<string>)(policy.EnabledFillTags ?? new List<string>());

            return new PhraseHookWindowInfo(inPhraseEndWindow, inSectionEndWindow, enabledTags);
        }
    }
}
