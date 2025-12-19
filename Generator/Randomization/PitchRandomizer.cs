using Music.Generator;
using System.Diagnostics;

namespace Music.Generator
{
    /// <summary>
    /// Applies controlled randomness to pitch selection for different instrument parts.
    /// All generated pitches remain within key/chord constraints.
    /// </summary>
    public sealed class PitchRandomizer
    {
        private readonly RandomizationSettings _settings;

        // Bass register constants
        private const int BassMinMidi = 28;  // E1
        private const int BassMaxMidi = 48;  // C3
        private const int BassPreferredCenter = 36; // C2

        // Guitar register constants  
        private const int GuitarMinMidi = 40;  // E2
        private const int GuitarMaxMidi = 76;  // E5
        private const int GuitarPreferredCenter = 52; // E3

        // Keys register constants
        private const int KeysMinMidi = 48;  // C3
        private const int KeysMaxMidi = 84;  // C6
        private const int KeysPreferredCenter = 60; // C4

        public PitchRandomizer(RandomizationSettings? settings = null)
        {
            _settings = settings ?? RandomizationSettings.Default;
        }

        /// <summary>
        /// Selects a bass pitch for the given harmony context.
        /// Chooses from root, fifth, or octave with configurable weights.
        /// </summary>
        /// <param name="ctx">Current harmony context</param>
        /// <param name="bar">1-based bar number</param>
        /// <param name="onsetBeat">Beat position</param>
        /// <returns>MIDI note number for bass</returns>
        public int SelectBassPitch(HarmonyPitchContext ctx, int bar, decimal onsetBeat)
        {
            var rng = RandomHelpers.CreateLocalRng(_settings.Seed, "bass", bar, onsetBeat);

            int rootPc = ctx.ChordRootPitchClass;
            int fifthPc = (rootPc + 7) % 12;

            // Weighted choice: 0=root, 1=fifth, 2=octave (root in higher register)
            int choice = RandomHelpers.WeightedChoice(rng, [
                (0, _settings.BassRootWeight),
                (1, _settings.BassFifthWeight),
                (2, _settings.BassOctaveWeight)
            ]);

            int targetPc;
            int center = BassPreferredCenter;

            switch (choice)
            {
                case 1: // Fifth
                    targetPc = fifthPc;
                    break;
                case 2: // Octave (root, higher register)
                    targetPc = rootPc;
                    center = BassPreferredCenter + 12; // Bias upward
                    break;
                default: // Root
                    targetPc = rootPc;
                    break;
            }

            int midi = RandomHelpers.PickMidiNearRange(targetPc, BassMinMidi, BassMaxMidi, center);

            // Validation: pitch class must be root or fifth, and in scale
            Debug.Assert(
                PitchClassUtils.ToPitchClass(midi) == rootPc || 
                PitchClassUtils.ToPitchClass(midi) == fifthPc,
                $"Bass pitch class {PitchClassUtils.ToPitchClass(midi)} is neither root {rootPc} nor fifth {fifthPc}");

            return midi;
        }

        /// <summary>
        /// Selects a guitar pitch for the given harmony context.
        /// Strong beats use chord tones; weak beats may use passing tones.
        /// </summary>
        /// <param name="ctx">Current harmony context</param>
        /// <param name="bar">1-based bar number</param>
        /// <param name="onsetBeat">Beat position</param>
        /// <param name="previousPitchClass">Previous guitar pitch class, or null if none</param>
        /// <returns>Tuple of (MIDI note, new pitch class for tracking)</returns>
        public (int midi, int pitchClass) SelectGuitarPitch(
            HarmonyPitchContext ctx, 
            int bar, 
            decimal onsetBeat, 
            int? previousPitchClass)
        {
            var rng = RandomHelpers.CreateLocalRng(_settings.Seed, "guitar", bar, onsetBeat);
            bool isStrong = RandomHelpers.IsStrongBeat(onsetBeat);

            int pc;

            if (isStrong)
            {
                // Strong beat: chord tones only
                var chordTones = ctx.ChordPitchClasses.ToList();
                pc = RandomHelpers.ChooseRandom(rng, chordTones);
            }
            else
            {
                // Weak beat: possibly use a passing tone
                bool usePassingTone = previousPitchClass.HasValue &&
                                      rng.NextDouble() < _settings.GuitarPassingToneProbability;

                if (usePassingTone)
                {
                    var neighbors = RandomHelpers.GetDiatonicNeighbors(
                        previousPitchClass!.Value, 
                        ctx.KeyScalePitchClasses);

                    // Prefer neighbors that are NOT chord tones (true passing tones)
                    var nonChordNeighbors = neighbors
                        .Where(n => !RandomHelpers.IsChordTone(n, ctx))
                        .ToList();

                    if (nonChordNeighbors.Count > 0)
                    {
                        pc = RandomHelpers.ChooseRandom(rng, nonChordNeighbors);
                    }
                    else if (neighbors.Count > 0)
                    {
                        pc = RandomHelpers.ChooseRandom(rng, neighbors);
                    }
                    else
                    {
                        // Fallback to chord tone
                        pc = RandomHelpers.ChooseRandom(rng, ctx.ChordPitchClasses.ToList());
                    }
                }
                else
                {
                    // Use chord tone
                    pc = RandomHelpers.ChooseRandom(rng, ctx.ChordPitchClasses.ToList());
                }
            }

            int midi = RandomHelpers.PickMidiNearRange(pc, GuitarMinMidi, GuitarMaxMidi, GuitarPreferredCenter);

            // Validation
            Debug.Assert(
                RandomHelpers.IsInScale(pc, ctx),
                $"Guitar pitch class {pc} is not in scale");
            Debug.Assert(
                !isStrong || RandomHelpers.IsChordTone(pc, ctx),
                $"Guitar pitch class {pc} on strong beat is not a chord tone");

            return (midi, pc);
        }

        /// <summary>
        /// Selects a chord voicing for keys/pads.
        /// May add a diatonic 9th on the first onset of a harmony event.
        /// </summary>
        /// <param name="ctx">Current harmony context</param>
        /// <param name="bar">1-based bar number</param>
        /// <param name="onsetBeat">Beat position</param>
        /// <param name="isFirstOnsetOfHarmony">True if this is the first onset of this harmony event</param>
        /// <returns>List of MIDI notes for the chord voicing</returns>
        public List<int> SelectKeysVoicing(
            HarmonyPitchContext ctx, 
            int bar, 
            decimal onsetBeat, 
            bool isFirstOnsetOfHarmony)
        {
            var rng = RandomHelpers.CreateLocalRng(_settings.Seed, "keys", bar, onsetBeat);

            // Start with the base chord voicing
            var midiNotes = ctx.ChordMidiNotes.ToList();

            // Optionally add a diatonic 9th on the first onset
            if (isFirstOnsetOfHarmony && rng.NextDouble() < _settings.KeysAdd9Probability)
            {
                int rootPc = ctx.ChordRootPitchClass;
                int scaleIdx = RandomHelpers.FindScaleIndex(rootPc, ctx.KeyScalePitchClasses);

                if (scaleIdx >= 0)
                {
                    // 9th is scale degree 2 above root = (scaleIdx + 1) % 7
                    int ninthPc = ctx.KeyScalePitchClasses[(scaleIdx + 1) % 7];

                    // Place 9th above the chord (in the upper register)
                    int ninthMidi = RandomHelpers.PickMidiNearRange(
                        ninthPc, 
                        KeysMinMidi + 12,  // Above bass range of chord
                        KeysMaxMidi, 
                        KeysPreferredCenter + 12);

                    if (!midiNotes.Contains(ninthMidi))
                    {
                        midiNotes.Add(ninthMidi);
                    }
                }
            }

            // Sort and ensure uniqueness
            midiNotes = midiNotes.Distinct().OrderBy(n => n).ToList();

            // Validation: all pitch classes must be in scale
            foreach (var midi in midiNotes)
            {
                int pc = PitchClassUtils.ToPitchClass(midi);
                Debug.Assert(
                    RandomHelpers.IsInScale(pc, ctx),
                    $"Keys pitch class {pc} (MIDI {midi}) is not in scale");
            }

            return midiNotes;
        }

        /// <summary>
        /// Selects an inversion/bass option for chord voicing.
        /// </summary>
        /// <param name="ctx">Current harmony context</param>
        /// <param name="bar">1-based bar number</param>
        /// <param name="onsetBeat">Beat position</param>
        /// <returns>Bass option string ("root", "3rd", "5th", or "7th")</returns>
        public string SelectChordInversion(HarmonyPitchContext ctx, int bar, decimal onsetBeat)
        {
            var rng = RandomHelpers.CreateLocalRng(_settings.Seed, "keys-inversion", bar, onsetBeat);

            // Determine available inversions based on chord size
            var options = ctx.ChordPitchClasses.Count >= 4
                ? new[] { "root", "3rd", "5th", "7th" }
                : new[] { "root", "3rd", "5th" };

            int idx = rng.NextInt(0, options.Length);
            return options[idx];
        }

        /// <summary>
        /// Selects a drum velocity with subtle humanization randomness.
        /// </summary>
        public int SelectDrumVelocity(int bar, decimal onsetBeat, string drumType, int baseVelocity)
        {
            var rng = RandomHelpers.CreateLocalRng(_settings.Seed, $"drum_{drumType}", bar, onsetBeat);
            
            // Apply ±10% variation for humanization
            int variation = (int)(baseVelocity * 0.1);
            int minVel = Math.Max(1, baseVelocity - variation);
            int maxVel = Math.Min(127, baseVelocity + variation);
            
            return rng.NextInt(minVel, maxVel + 1);
        }
    }
}