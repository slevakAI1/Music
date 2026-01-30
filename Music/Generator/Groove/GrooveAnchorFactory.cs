namespace Music.Generator.Groove
{
    // AI: purpose=Centralized factory for retrieving groove anchor patterns by genre.
    // AI: invariants=Anchor patterns are hardcoded, deterministic; same genre always returns same pattern.
    // AI: errors=Throws ArgumentException for unknown genre; throws ArgumentNullException for null genre.
    // AI: change=Generate method removed (GC-4); variation now handled by Drummer Agent, not groove layer.
    public static class GrooveAnchorFactory
    {
        // AI: purpose=Returns anchor pattern for specified genre; anchors define base rhythm without variation.
        public static GrooveInstanceLayer GetAnchor(string genre)
        {
            ArgumentNullException.ThrowIfNull(genre);

            return genre switch
            {
                "PopRock" => GetPopRockAnchor(),
                _ => throw new ArgumentException($"Unknown genre: {genre}", nameof(genre))
            };
        }

        // AI: purpose=Returns list of all supported genre names for UI/validation.
        public static IReadOnlyList<string> GetAvailableGenres()
        {
            return new List<string> { "PopRock" };
        }

        // AI: purpose=PopRock anchor pattern - standard backbeat with 8th note hats.
        // AI: invariants=Kick [1,3], Snare [2,4] backbeat never changes; hats on all 8ths; bass/comp/pads mirror kick rhythm.
        private static GrooveInstanceLayer GetPopRockAnchor()
        {
            return new GrooveInstanceLayer
            {
                KickOnsets = new List<decimal> { 1m, 3m },
                SnareOnsets = new List<decimal> { 2m, 4m },
                HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                BassOnsets = new List<decimal> { 1m, 3m },
                CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                PadsOnsets = new List<decimal> { 1m, 3m }
            };
        }
    }
}
