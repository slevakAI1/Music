namespace Music.Generator.Groove
{
    using Music.Generator;
    using Music.MyMidi;

    // AI: purpose=Instance layer holding onset lists per role; used for anchor onsets in GroovePresetDefinition.
    // AI: invariants=Onset values in domain units (beats or fractional bar offsets); callers normalize/sort when needed.
    // AI: change=Add role onset list when introducing new role; decoupled from old groove code (migration needed).
    public sealed class GrooveInstanceLayer
    {
        public List<decimal> KickOnsets { get; set; } = new();
        public List<decimal> SnareOnsets { get; set; } = new();
        public List<decimal> HatOnsets { get; set; } = new();
        public List<decimal> BassOnsets { get; set; } = new();
        public List<decimal> CompOnsets { get; set; } = new();
        public List<decimal> PadsOnsets { get; set; } = new();

        // AI: purpose=Query methods for part generators to access onset data by role string.
        // AI: errors=GetOnsets returns empty list for unknown roles (never null, never throws).
        // AI: invariants=Role name matching is case-sensitive; must match GrooveRoles constants.
        public IReadOnlyList<decimal> GetOnsets(string role)
        {
            ArgumentNullException.ThrowIfNull(role);

            return role switch
            {
                GrooveRoles.Kick => KickOnsets,
                GrooveRoles.Snare => SnareOnsets,
                GrooveRoles.ClosedHat or GrooveRoles.OpenHat => HatOnsets,
                GrooveRoles.Bass => BassOnsets,
                GrooveRoles.Comp => CompOnsets,
                GrooveRoles.Pads => PadsOnsets,
                _ => Array.Empty<decimal>()
            };
        }

        public IReadOnlySet<string> GetActiveRoles()
        {
            HashSet<string> activeRoles = new();

            if (KickOnsets.Count > 0) activeRoles.Add(GrooveRoles.Kick);
            if (SnareOnsets.Count > 0) activeRoles.Add(GrooveRoles.Snare);
            if (HatOnsets.Count > 0)
            {
                activeRoles.Add(GrooveRoles.ClosedHat);
                activeRoles.Add(GrooveRoles.OpenHat);
            }
            if (BassOnsets.Count > 0) activeRoles.Add(GrooveRoles.Bass);
            if (CompOnsets.Count > 0) activeRoles.Add(GrooveRoles.Comp);
            if (PadsOnsets.Count > 0) activeRoles.Add(GrooveRoles.Pads);

            return activeRoles;
        }

        public bool HasRole(string role)
        {
            ArgumentNullException.ThrowIfNull(role);

            return role switch
            {
                GrooveRoles.Kick => KickOnsets.Count > 0,
                GrooveRoles.Snare => SnareOnsets.Count > 0,
                GrooveRoles.ClosedHat or GrooveRoles.OpenHat => HatOnsets.Count > 0,
                GrooveRoles.Bass => BassOnsets.Count > 0,
                GrooveRoles.Comp => CompOnsets.Count > 0,
                GrooveRoles.Pads => PadsOnsets.Count > 0,
                _ => false
            };
        }

        // AI: purpose=Creates varied groove from anchor by seed-based probabilistic onset additions.
        // AI: invariants=Same seed+anchor always produces identical output; snare backbeat (2,4) never modified; deterministic.
        // AI: deps=Rng must be initialized before calling; uses GrooveVariationGroupPick purpose for all decisions.
        // AI: change=To add variation types, add new decision blocks; keep probabilities tunable via seed-derived thresholds.
        public static GrooveInstanceLayer CreateVariation(GrooveInstanceLayer anchor, int seed)
        {
            ArgumentNullException.ThrowIfNull(anchor);

            Rng.Initialize(seed);

            GrooveInstanceLayer variation = new()
            {
                KickOnsets = new List<decimal>(anchor.KickOnsets),
                SnareOnsets = new List<decimal>(anchor.SnareOnsets),
                HatOnsets = new List<decimal>(anchor.HatOnsets),
                BassOnsets = new List<decimal>(anchor.BassOnsets),
                CompOnsets = new List<decimal>(anchor.CompOnsets),
                PadsOnsets = new List<decimal>(anchor.PadsOnsets)
            };

            ApplyKickDoubles(variation);
            ApplyHatSubdivision(variation);
            ApplySyncopation(variation);

            return variation;
        }

        // AI: purpose=Adds kick doubles at 1.5 or 3.5 with 50% probability for each position.
        // AI: invariants=Only adds if position not already present; avoids duplicates.
        private static void ApplyKickDoubles(GrooveInstanceLayer variation)
        {
            if (Rng.NextDouble(RandomPurpose.GrooveVariationGroupPick) < 0.5)
            {
                decimal kickDouble = 1.5m;
                if (!variation.KickOnsets.Contains(kickDouble))
                {
                    variation.KickOnsets.Add(kickDouble);
                }
            }

            if (Rng.NextDouble(RandomPurpose.GrooveVariationGroupPick) < 0.5)
            {
                decimal kickDouble = 3.5m;
                if (!variation.KickOnsets.Contains(kickDouble))
                {
                    variation.KickOnsets.Add(kickDouble);
                }
            }
        }

        // AI: purpose=Upgrades 8th note hats to 16ths with 30% probability; adds .25 offsets between existing 8ths.
        // AI: invariants=Only adds 16th positions that don't already exist; common pattern: 1.25, 2.25, 3.25, 4.25.
        private static void ApplyHatSubdivision(GrooveInstanceLayer variation)
        {
            if (Rng.NextDouble(RandomPurpose.GrooveVariationGroupPick) < 0.3)
            {
                decimal[] sixteenthPositions = { 1.25m, 2.25m, 3.25m, 4.25m };
                foreach (decimal position in sixteenthPositions)
                {
                    if (!variation.HatOnsets.Contains(position))
                    {
                        variation.HatOnsets.Add(position);
                    }
                }
            }
        }

        // AI: purpose=Adds anticipation/syncopation onsets at .75 positions with 20% probability.
        // AI: invariants=Adds to kick; avoids snare backbeat modification; typical anticipations: 1.75, 3.75 (anticipate beats 2,4).
        private static void ApplySyncopation(GrooveInstanceLayer variation)
        {
            if (Rng.NextDouble(RandomPurpose.GrooveVariationGroupPick) < 0.2)
            {
                decimal[] anticipationPositions = { 1.75m, 3.75m };
                foreach (decimal position in anticipationPositions)
                {
                    if (!variation.KickOnsets.Contains(position))
                    {
                        variation.KickOnsets.Add(position);
                    }
                }
            }
        }

        // AI: purpose=Converts groove to playable drum PartTrack for audition; maps roles to GM MIDI drum notes.
        // AI: invariants=All events use same velocity (no shaping); events sorted by AbsoluteTimeTicks; each onset repeated per bar.
        // AI: deps=BarTrack for tick conversion; uses GM MIDI drum note numbers (Kick=36, Snare=38, ClosedHat=42, OpenHat=46).
        // AI: change=To add roles, add case to role switch with appropriate MIDI note number.
        public PartTrack ToPartTrack(BarTrack barTrack, int totalBars, int velocity = 100)
        {
            ArgumentNullException.ThrowIfNull(barTrack);
            if (totalBars < 1)
                throw new ArgumentOutOfRangeException(nameof(totalBars), "Must be at least 1");
            if (velocity < 1 || velocity > 127)
                throw new ArgumentOutOfRangeException(nameof(velocity), "Must be 1-127");

            List<PartTrackEvent> events = new();

            // Process each bar
            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Add kick onsets
                foreach (decimal beat in KickOnsets)
                {
                    events.Add(CreateDrumEvent(barTrack, bar, beat, 36, velocity));
                }

                // Add snare onsets
                foreach (decimal beat in SnareOnsets)
                {
                    events.Add(CreateDrumEvent(barTrack, bar, beat, 38, velocity));
                }

                // Add hat onsets (use ClosedHat MIDI note)
                foreach (decimal beat in HatOnsets)
                {
                    events.Add(CreateDrumEvent(barTrack, bar, beat, 42, velocity));
                }
            }

            // Sort events by absolute time
            events.Sort((a, b) => a.AbsoluteTimeTicks.CompareTo(b.AbsoluteTimeTicks));

            PartTrack partTrack = new(events)
            {
                MidiProgramName = "Standard Kit",
                MidiProgramNumber = 0
            };

            return partTrack;
        }

        // AI: purpose=Helper to create single drum event from bar+beat position with fixed duration.
        // AI: invariants=Duration fixed at 120 ticks (short drum hit); uses BarTrack.ToTick for timing conversion.
        private static PartTrackEvent CreateDrumEvent(
            BarTrack barTrack,
            int barNumber,
            decimal beat,
            int midiNote,
            int velocity)
        {
            long absoluteTicks = barTrack.ToTick(barNumber, beat);
            const int drumNoteDuration = 120;

            return new PartTrackEvent(
                noteNumber: midiNote,
                absoluteTimeTicks: (int)absoluteTicks,
                noteDurationTicks: drumNoteDuration,
                noteOnVelocity: velocity);
        }
    }
}
