// AI: purpose=Augments per-bar protections with phrase-end protection rules (downbeat/backbeat NeverRemove).
// AI: deps=PhraseHookWindowResolver, BarContext, GroovePhraseHookPolicy; generator-agnostic.
// AI: invariants=Mutates protection sets in-place; adds NeverRemove for downbeat (1) and backbeats (2, 4) when in phrase-end window.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Augments per-bar protection sets with phrase-end protection rules.
    /// When a bar is in the phrase-end window, adds NeverRemove protections for downbeats and/or backbeats.
    /// Extracted from DrumTrackGenerator for cross-generator reuse (drums, comp, melody, motifs).
    /// </summary>
    public static class PhraseHookProtectionAugmenter
    {
        /// <summary>
        /// Augments protection sets with phrase-end protections based on policy.
        /// Adds NeverRemove for downbeat (beat 1) and backbeats (beats 2, 4) when in phrase-end window.
        /// </summary>
        /// <param name="mergedProtections">Per-bar role protection sets to augment (mutated in-place).</param>
        /// <param name="barContexts">Per-bar context for phrase/section position.</param>
        /// <param name="phraseHookPolicy">Policy controlling phrase-end protection behavior.</param>
        /// <param name="beatsPerBar">Number of beats per bar (for determining backbeat positions).</param>
        public static void Augment(
            Dictionary<int, Dictionary<string, RoleProtectionSet>> mergedProtections,
            IReadOnlyList<BarContext> barContexts,
            GroovePhraseHookPolicy? phraseHookPolicy,
            int beatsPerBar)
        {
            if (phraseHookPolicy == null || mergedProtections == null || barContexts == null)
                return;

            foreach (var ctx in barContexts)
            {
                if (!mergedProtections.TryGetValue(ctx.BarNumber, out var protectionsByRole))
                {
                    protectionsByRole = new Dictionary<string, RoleProtectionSet>(StringComparer.OrdinalIgnoreCase);
                    mergedProtections[ctx.BarNumber] = protectionsByRole;
                }

                var windowInfo = PhraseHookWindowResolver.Resolve(ctx, phraseHookPolicy);
                if (!windowInfo.InPhraseEndWindow)
                    continue;

                // Protect downbeat (beat 1) in phrase-end bars
                if (phraseHookPolicy.ProtectDownbeatOnPhraseEnd)
                {
                    AddNeverRemoveToAllRoles(protectionsByRole, 1m);
                }

                // Protect backbeats (beats 2, 4) in phrase-end bars
                if (phraseHookPolicy.ProtectBackbeatOnPhraseEnd)
                {
                    var backbeats = GetBackbeatPositions(beatsPerBar);
                    foreach (var beat in backbeats)
                    {
                        AddNeverRemoveToAllRoles(protectionsByRole, beat);
                    }
                }
            }
        }

        /// <summary>
        /// Returns standard backbeat positions for the given time signature.
        /// </summary>
        /// <param name="beatsPerBar">Number of beats per bar.</param>
        /// <returns>List of backbeat positions (typically beats 2 and 4 for 4/4).</returns>
        public static IReadOnlyList<decimal> GetBackbeatPositions(int beatsPerBar)
        {
            var backbeats = new List<decimal>();
            if (beatsPerBar >= 2) backbeats.Add(2m);
            if (beatsPerBar >= 4) backbeats.Add(4m);
            return backbeats;
        }

        private static void AddNeverRemoveToAllRoles(Dictionary<string, RoleProtectionSet> protectionsByRole, decimal beat)
        {
            foreach (var roleName in protectionsByRole.Keys.ToList())
            {
                var set = protectionsByRole[roleName] ?? new RoleProtectionSet();
                if (!set.NeverRemoveOnsets.Contains(beat))
                    set.NeverRemoveOnsets.Add(beat);
                protectionsByRole[roleName] = set;
            }
        }
    }
}
