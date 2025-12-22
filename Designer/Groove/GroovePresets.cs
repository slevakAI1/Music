namespace Music.Generator
{
    // Factory for hardcoded groove presets
    public static class GroovePresets
    {
        /// <summary>
        /// Returns the "BossaNovaBasic" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: Bossa is often taught with steady 8th-note timekeeping; bass drum motion can be more syncopated.
        /// </summary>
        public static GroovePreset GetBossaNovaBasic()
        {
            return new GroovePreset
            {
                Name = "BossaNovaBasic",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Foundation pulse (simple, DAW-friendly approximation)
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Surdo-like / bass-drum motion (lightly syncopated base)
                    KickOnsets = new List<decimal> { 1m, 1.5m, 2.5m, 3m, 3.5m, 4.5m },

                    // Cross-stick / rim-click feel often sits on 2 & 4 in basic bossa teaching
                    SnareOnsets = new List<decimal> { 2m, 4m },

                    // Bass aligns with the main low-end motion
                    BassOnsets = new List<decimal> { 1m, 3m },

                    // Guitar/keys comp: light offbeat pushes
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Pads: sustained harmony on bar anchors
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "CountryTrain" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: Train beat is commonly taught with an ongoing driving subdivision (often snare/brushes).
        /// </summary>
        public static GroovePreset GetCountryTrain()
        {
            return new GroovePreset
            {
                Name = "CountryTrain",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Kick commonly anchors 1 & 3 in many train feels
                    KickOnsets = new List<decimal> { 1m, 3m },

                    // Train feel: continuous "chug" subdivision (base: 8ths on snare/brushes)
                    SnareOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Timekeeper (hat/shaker) also rides 8ths in a basic approximation
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Bass: simple lock to the big downbeats
                    BassOnsets = new List<decimal> { 1m, 3m },

                    // Acoustic guitar strum often feels like steady 8ths; use offbeats for "train" momentum
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Pads: light anchors
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "DanceEDMFourOnFloor" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// </summary>
        public static GroovePreset GetDanceEDMFourOnFloor()
        {
            return new GroovePreset
            {
                Name = "DanceEDMFourOnFloor",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Four-on-the-floor: kick on every quarter note
                    KickOnsets = new List<decimal> { 1m, 2m, 3m, 4m },

                    // Common clap/snare on 2 and 4
                    SnareOnsets = new List<decimal> { 2m, 4m },

                    // Offbeat hats are a common starting point (energy can be added later with 16ths)
                    HatOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Bass often reinforces the quarter-note pulse in simple EDM
                    BassOnsets = new List<decimal> { 1m, 2m, 3m, 4m },

                    // Comp/synth rhythm commonly pushes offbeats
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Pads: long harmonic anchors
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "FunkSyncopated" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: Funk often leans on steady 16th hats + backbeat + syncopated kick; ghost notes/accents come later.
        /// </summary>
        public static GroovePreset GetFunkSyncopated()
        {
            return new GroovePreset
            {
                Name = "FunkSyncopated",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Backbeat anchors
                    SnareOnsets = new List<decimal> { 2m, 4m },

                    // 16th-note hat grid (base funk timekeeping)
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.25m, 1.5m, 1.75m,
                        2m, 2.25m, 2.5m, 2.75m,
                        3m, 3.25m, 3.5m, 3.75m,
                        4m, 4.25m, 4.5m, 4.75m
                    },

                    // Syncopated kick (starter pattern; you'll permute heavily later)
                    KickOnsets = new List<decimal> { 1m, 1.5m, 1.75m, 3m, 3.5m },

                    // Bass follows the kick for a tight pocket
                    BassOnsets = new List<decimal> { 1m, 1.5m, 1.75m, 3m, 3.5m },

                    // Rhythm guitar comp often lives in 16th/offbeat space; keep it sparse initially
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Pads: minimal in classic funk; keep just the bar anchors
                    PadsOnsets = new List<decimal> { 1m, 3m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "HipHopBoomBap" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: Boom bap commonly uses a strong backbeat with kick anchoring downbeats; swing/humanization is essential later.
        /// </summary>
        public static GroovePreset GetHipHopBoomBap()
        {
            return new GroovePreset
            {
                Name = "HipHopBoomBap",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Core anchors (common starting point)
                    KickOnsets = new List<decimal> { 1m, 3m },

                    // Classic backbeat
                    SnareOnsets = new List<decimal> { 2m, 4m },

                    // Simple 8th hat scaffold (shuffle/swing can be applied later via timing variance)
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Bass typically reinforces kick anchors
                    BassOnsets = new List<decimal> { 1m, 3m },

                    // Sparse comp/chops on offbeats
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },

                    // Pads: slow bed
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "JazzSwing" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: Jazz swing is triplet-based; we approximate the ride pattern with triplet offsets.
        /// </summary>
        public static GroovePreset GetJazzSwing()
        {
            return new GroovePreset
            {
                Name = "JazzSwing",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Feathering concept: light kick on all quarters (feel > loudness; velocity later)
                    KickOnsets = new List<decimal> { 1m, 2m, 3m, 4m },

                    // Jazz comping snare is not a strict backbeat; keep empty as a base scaffold
                    SnareOnsets = new List<decimal>(),

                    // Using HatOnsets to represent the *ride* swing pattern (no separate ride property exists).
                    // "Ding (1), Ding (2), Da (2 + 2/3), Ding (3), Ding (4), Da (4 + 2/3)"
                    HatOnsets = new List<decimal> { 1m, 2m, 2.6667m, 3m, 4m, 4.6667m },

                    // Walking bass: quarters
                    BassOnsets = new List<decimal> { 1m, 2m, 3m, 4m },

                    // Comping (piano/guitar) tends to hit upbeats; keep a sparse baseline
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },

                    // Pads: typically minimal; leave empty
                    PadsOnsets = new List<decimal>()
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "MetalDoubleKick" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: This is a conservative "double-kick bed" (8ths) you can intensify later (16ths, blasts, etc.).
        /// </summary>
        public static GroovePreset GetMetalDoubleKick()
        {
            return new GroovePreset
            {
                Name = "MetalDoubleKick",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Double-kick bed (8ths)
                    KickOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Backbeat anchors (common in many metal grooves; variations later)
                    SnareOnsets = new List<decimal> { 2m, 4m },

                    // Timekeeper (hat/ride) 8ths as a base scaffold
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Bass locks to double-kick bed
                    BassOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Rhythm guitars often chug in 8ths/16ths; keep 8th offbeats for lock
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Pads: uncommon; leave empty
                    PadsOnsets = new List<decimal>()
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "PopRockBasic" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// </summary>
        public static GroovePreset GetPopRockBasic()
        {
            return new GroovePreset
            {
                Name = "PopRockBasic",

                AnchorLayer = new GrooveInstanceLayer
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

                TensionLayer = new GrooveInstanceLayer
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
        /// Returns the "RapBasic" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: Conservative “rap” scaffold (boom-bap-ish backbone) intended for later swing/humanize + permutation.
        /// </summary>
        public static GroovePreset GetRapBasic()
        {
            return new GroovePreset
            {
                Name = "RapBasic",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Solid downbeat + a couple common syncopation spots
                    KickOnsets = new List<decimal> { 1m, 1.75m, 3m },

                    // Classic backbeat (works for a lot of rap)
                    SnareOnsets = new List<decimal> { 2m, 4m },

                    // Simple timekeeper (add swing/rolls later via permutation)
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Bass/808 follows the kick as a safe default
                    BassOnsets = new List<decimal> { 1m, 1.75m, 3m },

                    // Sparse chops/stabs
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },

                    // Minimal pad anchor
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "ReggaeOneDrop" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: One-drop places kick + snare on beat 3; beat 1 is intentionally empty ("dropped").
        /// </summary>
        public static GroovePreset GetReggaeOneDrop()
        {
            return new GroovePreset
            {
                Name = "ReggaeOneDrop",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // One-drop: kick on 3
                    KickOnsets = new List<decimal> { 3m },

                    // One-drop: snare/rimshot also on 3
                    SnareOnsets = new List<decimal> { 3m },

                    // Hat commonly holds steady subdivisions; use 8ths as base
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Bass often anchors the song track strongly (simple: 1 & 3)
                    BassOnsets = new List<decimal> { 1m, 3m },

                    // Skank guitar/keys tends to hit the offbeats
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Pads: sustained harmony
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "ReggaetonDembow" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: This is a minimal "dembow-style" scaffold using 16th-grid placements for the backbeat elements.
        /// </summary>
        public static GroovePreset GetReggaetonDembow()
        {
            return new GroovePreset
            {
                Name = "ReggaetonDembow",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Foundation kick anchors
                    KickOnsets = new List<decimal> { 1m, 3m },

                    // Dembow-style backbeat elements:
                    // - "last 1/16th of beats 1 and 3" => 1.75, 3.75
                    // - "halfway through beats 2 and 4" => 2.5, 4.5
                    SnareOnsets = new List<decimal> { 1.75m, 2.5m, 3.75m, 4.5m },

                    // Hat: 8ths as a base (you'll add syncopation/rolls later)
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m
                    },

                    // Bass locks to kick anchors in a conservative base
                    BassOnsets = new List<decimal> { 1m, 3m },

                    // Comp often pushes offbeats; keep it simple
                    CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },

                    // Pads: bar anchor
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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
        /// Returns the "TrapModern" groove preset.
        /// Assumes 4/4 meter with 4 beats per bar.
        /// Note: Trap is commonly taught with a halftime backbone (snare on 3), with hats often running 16ths (plus rolls later).
        /// </summary>
        public static GroovePreset GetTrapModern()
        {
            return new GroovePreset
            {
                Name = "TrapModern",

                AnchorLayer = new GrooveInstanceLayer
                {
                    // Simplest halftime backbone: snare/clap on 3
                    SnareOnsets = new List<decimal> { 3m },

                    // Sparse kick anchors (starter pattern)
                    KickOnsets = new List<decimal> { 1m, 1.5m, 2.75m, 4m },

                    // 16th hat grid as a base (rolls/tuplets later via permutation)
                    HatOnsets = new List<decimal>
                    {
                        1m, 1.25m, 1.5m, 1.75m,
                        2m, 2.25m, 2.5m, 2.75m,
                        3m, 3.25m, 3.5m, 3.75m,
                        4m, 4.25m, 4.5m, 4.75m
                    },

                    // 808/bass commonly follows kick rhythm
                    BassOnsets = new List<decimal> { 1m, 1.5m, 2.75m, 4m },

                    // Minimal melodic chops; keep offbeat pushes
                    CompOnsets = new List<decimal> { 2.5m, 4.5m },

                    // Pads: optional, keep minimal
                    PadsOnsets = new List<decimal> { 1m }
                },

                TensionLayer = new GrooveInstanceLayer
                {
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

/*
Potential missing groove capabilities (intentionally NOT added as properties per your instruction):
- Swing/shuffle feel parameter (e.g., swing percentage) separate from onset positions (especially for BoomBap/Jazz/R&B/Neo-soul feels).
- Multi-bar pattern length support (needed for many genre-defining clave/dembow variations, 2-bar phrases, call/response).
- Explicit ride cymbal lane separate from hi-hat lane (Jazz especially).
- Accent/velocity lanes (genre-defining dynamics: ghost notes in funk, train-beat accents, reggae rimshot vs snare, etc.).
- Micro-timing/humanization constraints per genre (laid-back vs pushed, MPC swing, etc.).
- Probability / density controls per instrument for controlled permutation while staying in-genre.
*/

// AI TO DO:
//    If you want the next step to be immediately useful for permutation,
//    the most leverage is adding a genre-specific “allowed offsets grid”
//    (8ths only vs 16ths vs triplets) and density bounds per part—but that can
//    live outside these preset objects, exactly as you planned.

/*
 * 
 * is it common for a song to change the groove one or more times during the song? Like maybe the intro is a different groove than the verse?

Yes — it’s common, and in a lot of genres it’s basically expected. But most songs don’t “swap everything”; they usually keep the same tempo/meter and change the groove by density, subdivision, and accents so it still feels like the same track.
Common ways songs change groove (without changing tempo)
Intro vs verse: fewer elements (no kick, closed hat only, or half-time feel) → verse brings the full pocket.
Verse → chorus “lift”: same kick/snare grid, but hats go 8ths → 16ths, or add open-hat on offbeats, or add extra kick pickups.
Half-time / double-time illusion:
Half-time feel: snare moves to beat 3 (common in trap, metal breakdowns).
Double-time feel: hats/percussion subdivide faster while kick/snare anchors stay recognizable.
Pre-chorus: groove “climbs” (more syncopation, more percussion, rising hat density).
Bridge / breakdown: drop kick, change snare placement, or switch to toms/claps; sometimes a totally new pattern.
EDM drop: the groove often is the section identity (build vs drop are different grooves).
Hip-hop “beat switch”: sometimes a full groove change mid-song; super common in modern rap.

What’s less common
Hard groove changes that also change tempo and meter (happens, but it’s a bigger compositional move and can feel like two songs stitched together if not handled well).
If you’re implementing this in your system
A clean mental model is: one “section groove” per section (Intro/Verse/Chorus/Bridge), where each section groove is either:
a variation of the same base preset (same anchors, different density/subdivision), or
a deliberate alternate groove (beat switch), with transition help (fill, riser, break).
If you want, tell me your section list (Intro/Verse/Chorus/etc.) and genre, and I’ll map a realistic “groove plan” (what changes where) using only your existing onset properties.
 */
