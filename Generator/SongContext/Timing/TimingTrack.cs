namespace Music.Generator
{
    /// <summary>
    /// A global, bar/beat-aligned design track that records time signature changes for a song.
    /// </summary>
    /// <remarks>
    /// Rationale / Constraints:
    /// - This class models the temporal placement of time signature events used during song generation.
    /// - Bars are treated as 1-based indices; callers should use 1..N semantics when querying.
    /// - The implementation assumes events are appended in chronological order (ascending <c>StartBar</c>).
    ///   Methods that search rely on that order and do not sort or normalize the list.
    /// - No validation is performed when adding events; callers are responsible for preventing duplicates or
    ///   overlapping events if that is a domain requirement.
    /// </remarks>
    public class Timingtrack
    {
        /// <summary>
        /// Mutable sequence of time signature events in this track.
        /// </summary>
        /// <remarks>
        /// Rationale / Constraints:
        /// - The list is intentionally exposed as a mutable collection to allow callers to perform batch updates
        ///   (insertions, removals, reordering) without additional APIs.
        /// - The class does not enforce ordering or uniqueness; keep the list ordered by <c>StartBar</c> for
        ///   correct results from query methods.
        /// </remarks>
        public List<TimingEvent> Events { get; set; } = new();

        /// <summary>
        /// Appends a time signature event to the end of the track.
        /// </summary>
        /// <param name="evt">The time signature event to add. Its <c>StartBar</c> is expected to use 1-based indexing.</param>
        /// <remarks>
        /// Rationale / Constraints:
        /// - This method does not validate or reorder the list; callers must ensure the appended event fits the
        ///   desired ordering and domain constraints.
        /// - Use this method when you intend to maintain chronological order by appending events in sequence.
        /// </remarks>
        public void Add(TimingEvent evt)
        {
            Events.Add(evt);
        }

        /// <summary>
        /// Returns the most recent time signature event that begins on or before the specified bar.
        /// </summary>
        /// <param name="bar">1-based bar index for which to find the active time signature.</param>
        /// <returns>
        /// The active <see cref="TimingEvent"/> whose <c>StartBar</c> is the latest one &lt;= <paramref name="bar"/>,

        /// or <c>null</c> if no event starts on or before <paramref name="bar"/>.
        /// </returns>
        /// <remarks>
        /// - Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="bar"/> &lt; 1 because callers
        ///   should use 1-based bar numbering.
        /// - The method iterates from the end of <see cref="Events"/> toward the start and therefore expects
        ///   that events are in ascending <c>StartBar</c> order. If the list is unsorted, the result may be incorrect.
        /// - An event that starts exactly on <paramref name="bar"/> is considered active for that bar.
        /// </remarks>
        public TimingEvent? GetActiveTimeSignatureEvent(int bar)
        {
            if (bar < 1) throw new ArgumentOutOfRangeException(nameof(bar));

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                var evt = Events[i];
                var eventStartBar = evt.StartBar;

                if (eventStartBar <= bar)
                {
                    return evt;
                }
            }

            return null;
        }
    }
}
