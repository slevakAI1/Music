namespace Music.Generator
{
    /// <summary>
    /// Represents a single time-signature event in the song timeline.
    /// </summary>
    /// <remarks>
    /// Rationale / Constraints:
    /// - Models a discrete time-signature change that becomes effective at a specific bar index.
    /// - <see cref="StartBar"/> uses 1-based indexing; callers should treat bars as 1..N.
    /// - Instances are immutable after construction (<c>init</c> properties) so they can be safely shared.
    /// - No validation is performed by this type (e.g., denominator zero or non-power-of-two); callers are
    ///   responsible for ensuring domain-correct values when creating instances.
    /// </remarks>
    public sealed class TimingEvent
    {
        /// <summary>
        /// The bar where this time signature becomes active, using 1-based indexing.
        /// </summary>
        /// <remarks>
        /// Rationale / Constraints:
        /// - A value < 1 is semantically invalid for callers; methods that query by bar expect callers to
        ///   use 1-based bars and may throw if given invalid values.
        /// - This type does not enforce the invariant; validation should occur at construction site if required.
        /// </remarks>
        public int StartBar { get; init; }

        /// <summary>
        /// The time-signature numerator (beats per bar) that applies starting at <see cref="StartBar"/>.
        /// </summary>
        /// <remarks>
        /// Rationale / Constraints:
        /// - Typical values are positive integers (e.g., 3, 4); zero or negative values are considered invalid
        ///   by domain semantics but are not prevented by this type.
        /// </remarks>
        public int Numerator { get; init; }

        /// <summary>
        /// The time-signature denominator (note value that represents one beat) that applies starting at <see cref="StartBar"/>.
        /// </summary>
        /// <remarks>
        /// Rationale / Constraints:
        /// - By musical convention this is usually a power of two (e.g., 2, 4, 8). A denominator of zero is
        ///   invalid for arithmetic but not prevented here; callers must ensure a valid denominator when constructing events.
        /// </remarks>
        public int Denominator { get; init; }
    }
}
