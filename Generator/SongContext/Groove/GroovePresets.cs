// AI: purpose=Factory of declarative groove presets used as reusable templates for song generation.
// AI: invariants=BeatsPerBar is declarative; onset lists are raw, unsorted, may contain duplicates; callers must normalize.
// AI: deps=Returns new GroovePreset instance per call; changing property names/types breaks consumers and persistence.
// AI: change=When adding/removing presets, update GetByName switch to keep parity; do not encode velocity/humanization here.

namespace Music.Generator
{
    // AI: presets are templates only; consumers decide merge/normalization and mapping to MIDI/velocity.
    public static class GroovePresets
    {
        public static GroovePreset GetBossaNovaBasic()
        {
            return new GroovePreset
            {
                Name = "BossaNovaBasic",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    KickOnsets = new List<decimal> { 1m, 1.5m, 2.5m, 3m, 3.5m, 4.5m },
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    BassOnsets = new List<decimal> { 1m, 3m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetCountryTrain()
        {
            return new GroovePreset
            {
                Name = "CountryTrain",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 3m },
                    SnareOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 3m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetDanceEDMFourOnFloor()
        {
            return new GroovePreset
            {
                Name = "DanceEDMFourOnFloor",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 2m, 3m, 4m },
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    HatOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 2m, 3m, 4m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetFunkSyncopated()
        {
            return new GroovePreset
            {
                Name = "FunkSyncopated",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.25m, 1.5m, 1.75m,
                        2m, 2.25m, 2.5m, 2.75m,
                        3m, 3.25m, 3.5m, 3.75m,
                        4m, 4.25m, 4.5m, 4.75m
                    },
                    KickOnsets = new List<decimal> { 1m, 1.5m, 1.75m, 3m, 3.5m },
                    BassOnsets = new List<decimal> { 1m, 1.5m, 1.75m, 3m, 3.5m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetHipHopBoomBap()
        {
            return new GroovePreset
            {
                Name = "HipHopBoomBap",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 3m },
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 3m },
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetJazzSwing()
        {
            return new GroovePreset
            {
                Name = "JazzSwing",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 2m, 3m, 4m },
                    SnareOnsets = new List<decimal>(),
                    HatOnsets = new List<decimal> { 1m, 2m, 2.6667m, 3m, 4m, 4.6667m },
                    BassOnsets = new List<decimal> { 1m, 2m, 3m, 4m },
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },
                    PadsOnsets = new List<decimal>()
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetMetalDoubleKick()
        {
            return new GroovePreset
            {
                Name = "MetalDoubleKick",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal>()
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetPopRockBasic()
        {
            return new GroovePreset
            {
                Name = "PopRockBasic",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 3m },
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 3m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetRapBasic()
        {
            return new GroovePreset
            {
                Name = "RapBasic",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 1.75m, 3m },
                    SnareOnsets = new List<decimal> { 2m, 4m },
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 1.75m, 3m },
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetReggaeOneDrop()
        {
            return new GroovePreset
            {
                Name = "ReggaeOneDrop",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 3m },
                    SnareOnsets = new List<decimal> { 3m },
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 3m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetReggaetonDembow()
        {
            return new GroovePreset
            {
                Name = "ReggaetonDembow",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    KickOnsets = new List<decimal> { 1m, 3m },
                    SnareOnsets = new List<decimal> { 1.75m, 2.5m, 3.75m, 4.5m },
                    HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                    BassOnsets = new List<decimal> { 1m, 3m },
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        public static GroovePreset GetTrapModern()
        {
            return new GroovePreset
            {
                Name = "TrapModern",
                BeatsPerBar = 4,

                AnchorLayer = new GrooveInstanceLayer
                {
                    SnareOnsets = new List<decimal> { 3m },
                    KickOnsets = new List<decimal> { 1m, 1.5m, 2.75m, 4m },
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.25m, 1.5m, 1.75m,
                        2m, 2.25m, 2.5m, 2.75m,
                        3m, 3.25m, 3.5m, 3.75m,
                        4m, 4.25m, 4.5m, 4.75m
                    },
                    BassOnsets = new List<decimal> { 1m, 1.5m, 2.75m, 4m },
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer()
            };
        }

        // AI: contract=GetByName trims input; matching is case-sensitive exact name; returns new instance or null.
        public static GroovePreset? GetByName(string name)
        {
            return name.Trim() switch
            {
                "BossaNovaBasic" => GetBossaNovaBasic(),
                "CountryTrain" => GetCountryTrain(),
                "DanceEDMFourOnFloor" => GetDanceEDMFourOnFloor(),
                "FunkSyncopated" => GetFunkSyncopated(),
                "HipHopBoomBap" => GetHipHopBoomBap(),
                "JazzSwing" => GetJazzSwing(),
                "MetalDoubleKick" => GetMetalDoubleKick(),
                "PopRockBasic" => GetPopRockBasic(),
                "RapBasic" => GetRapBasic(),
                "ReggaeOneDrop" => GetReggaeOneDrop(),
                "ReggaetonDembow" => GetReggaetonDembow(),
                "TrapModern" => GetTrapModern(),
                _ => null
            };
        }
    }
}
