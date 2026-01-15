namespace Music.Generator
{
    /// <summary>
    /// Collection of patterns available for use in song generation.
    /// Can be populated from design, generated during composition, or loaded from storage.
    /// </summary>
    public sealed class ProposedPatternLibrary
    {
        /// <summary>
        /// Unique identifier for this library.
        /// </summary>
        public string LibraryId { get; init; }

        /// <summary>
        /// All patterns in the library.
        /// </summary>
        public List<ProposedPattern> Patterns { get; set; }

        /// <summary>
        /// Patterns designated as identity tokens (main hooks).
        /// </summary>
        public List<string> IdentityTokenIds { get; set; }

        public ProposedPatternLibrary()
        {
            LibraryId = Guid.NewGuid().ToString("N");
            Patterns = new List<ProposedPattern>();
            IdentityTokenIds = new List<string>();
        }
    }
}