// AI: purpose=Generator-agnostic rhythm vocabulary filter for syncopation/anticipation rules.
// AI: deps=GrooveRoleConstraintPolicy, RoleRhythmVocabulary; used by drums, comp, melody, motifs.
// AI: invariants=Deterministic position classification; v1 straight-grid heuristics (upgraded in Story 18/20).

namespace Music.Generator
{
    /// <summary>
    /// Filters events based on rhythm vocabulary rules (syncopation, anticipation).
    /// Story G3 extraction from DrumTrackGenerator.ApplySyncopationAnticipationFilter.
    /// Generator-agnostic: works with any event type via delegates.
    /// </summary>
    public static class RhythmVocabularyFilter
    {
        /// <summary>
        /// Checks if a beat position is allowed for a specific role based on rhythm vocabulary rules.
        /// </summary>
        /// <param name="roleName">Role name (e.g., "Kick", "Snare", "Lead")</param>
        /// <param name="beat">1-based beat position within bar</param>
        /// <param name="beatsPerBar">Number of beats per bar (e.g., 4 for 4/4 time)</param>
        /// <param name="roleConstraintPolicy">Policy containing role vocabulary rules</param>
        /// <returns>True if beat position is allowed by vocabulary rules</returns>
        public static bool IsAllowed(
            string roleName,
            decimal beat,
            int beatsPerBar,
            GrooveRoleConstraintPolicy? roleConstraintPolicy)
        {
            if (roleConstraintPolicy == null || roleConstraintPolicy.RoleVocabulary == null)
                return true; // No policy means no filtering

            // Look up role vocabulary
            if (!roleConstraintPolicy.RoleVocabulary.TryGetValue(roleName, out var vocab))
                return true; // No vocabulary for this role means allow by default

            // Check syncopation rule
            if (!vocab.AllowSyncopation && IsOffbeatPosition(beat, beatsPerBar))
                return false;

            // Check anticipation rule
            if (!vocab.AllowAnticipation && IsPickupPosition(beat, beatsPerBar))
                return false;

            return true;
        }

        /// <summary>
        /// Filters a list of events based on rhythm vocabulary rules.
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="events">Events to filter</param>
        /// <param name="getRoleName">Delegate to extract role name from event</param>
        /// <param name="getBeat">Delegate to extract beat position from event</param>
        /// <param name="beatsPerBar">Number of beats per bar</param>
        /// <param name="roleConstraintPolicy">Policy containing role vocabulary rules</param>
        /// <returns>Filtered list of events</returns>
        public static List<T> Filter<T>(
            IEnumerable<T> events,
            Func<T, string> getRoleName,
            Func<T, decimal> getBeat,
            int beatsPerBar,
            GrooveRoleConstraintPolicy? roleConstraintPolicy)
        {
            if (events == null)
                return new List<T>();

            if (roleConstraintPolicy == null || roleConstraintPolicy.RoleVocabulary == null)
                return events.ToList(); // No filtering needed

            var filtered = new List<T>();

            foreach (var evt in events)
            {
                string roleName = getRoleName(evt);
                decimal beat = getBeat(evt);

                if (IsAllowed(roleName, beat, beatsPerBar, roleConstraintPolicy))
                {
                    filtered.Add(evt);
                }
            }

            return filtered;
        }

        /// <summary>
        /// Checks if beat position is an offbeat (.5 positions between main beats).
        /// Story G3: v1 straight-grid heuristic; will be upgraded in Story 18/20.
        /// </summary>
        /// <param name="beat">1-based beat position</param>
        /// <param name="beatsPerBar">Number of beats per bar</param>
        /// <returns>True if position is an eighth note offbeat</returns>
        public static bool IsOffbeatPosition(decimal beat, int beatsPerBar)
        {
            // Offbeats are at .5 positions (eighth note offbeats)
            // Examples in 4/4: 1.5, 2.5, 3.5, 4.5
            decimal fractionalPart = beat - Math.Floor(beat);
            return Math.Abs(fractionalPart - 0.5m) < 0.01m;
        }

        /// <summary>
        /// Checks if beat position is a pickup/anticipation (.75 or last 16th before strong beat).
        /// Story G3: v1 straight-grid heuristic; will be upgraded in Story 18/20.
        /// </summary>
        /// <param name="beat">1-based beat position</param>
        /// <param name="beatsPerBar">Number of beats per bar</param>
        /// <returns>True if position is a pickup/anticipation</returns>
        public static bool IsPickupPosition(decimal beat, int beatsPerBar)
        {
            // Pickups are typically at .75 positions (16th note anticipations)
            decimal fractionalPart = beat - Math.Floor(beat);

            // .75 positions (e.g., 1.75, 2.75, 3.75, 4.75)
            if (Math.Abs(fractionalPart - 0.75m) < 0.01m)
                return true;

            return false;
        }
    }
}
