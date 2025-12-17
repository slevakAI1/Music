namespace Music.Designer
{
    // Factory for hardcoded groove presets
    public static class GroovePresets
    {
        /// <summary>
        /// Returns the "PopRockBasic" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// </summary>
        public static GroovePreset GetPopRockBasic()
        {
            return new GroovePreset
            {
                Name = "PopRockBasic",
                
                AnchorLayer = new GrooveLayer
                {
                    // Kick on beats 1 and 3 (driving the downbeats)
                    KickOnsets = new List<decimal> { 1m, 3m },
                    
                    // Snare on beats 2 and 4 (classic backbeat)
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    
                    // Hi-hat on 8th notes (1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5)
                    HatOnsets = new List<decimal> 
                    { 
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m 
                    },
                    
                    // Bass locks to kick on 1 and 3, adds root movement
                    BassOnsets = new List<decimal> { 1m, 3m },
                    
                    // Rhythm guitar on offbeat 8ths (typical pop/rock strumming)
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    
                    // Pads on downbeats or half notes (sustained chords)
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },
                
                TensionLayer = new GrooveLayer
                {
                    // Empty for now (disabled)
                    KickOnsets = new List<decimal>(),
                    SnareOnsets = new List<decimal>(),
                    HatOnsets = new List<decimal>(),
                    BassOnsets = new List<decimal>(),
                    CompOnsets = new List<decimal>(),
                    PadsOnsets = new List<decimal>()
                }
            };
        }

        /// <summary>
        /// Gets a groove preset by name.
        /// </summary>
        public static GroovePreset? GetByName(string name)
        {
            return name.Trim() switch
            {
                "PopRockBasic" => GetPopRockBasic(),
                _ => null
            };
        }
    }
}