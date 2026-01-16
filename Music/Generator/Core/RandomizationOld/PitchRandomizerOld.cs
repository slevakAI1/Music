// AI: THIS FILE IS DEPRECATED. Use Rng in Music.Generator.Core.Randomization instead. This class kept for backwards compatibility.
// AI: purpose=Controlled pitch selection per instrument; returns MIDI notes that respect key and chord constraints.
// AI: invariants=All returned pitch classes must be in the provided key scale; bass pcs MUST be root or fifth; guitar strong-beat pcs MUST be chord tones.
// AI: deps=RandomHelpers (rng, pick, scale helpers), PitchClassUtils, RandomizationSettings.Seed controls determinism.
// AI: perf=hotpath per note; avoid allocations; uses local per-call Rng for determinism.
// TODO? confirm PickMidiNearRange handles octave wrapping and chooses nearest octave of target pc.

using System.Diagnostics;

namespace Music.Generator
{
    // AI: class:stateless wrapper around RandomHelpers; do NOT change algorithmic choices or assertions.
    public sealed class PitchRandomizerOld
    {
        private readonly RandomizationSettingsOld _settings;

        // AI: registers: min/max/preferred center are absolute MIDI numbers; keep these when tuning ranges.
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

        public PitchRandomizerOld(RandomizationSettingsOld? settings = null)
        {
            _settings = settings ?? RandomizationSettingsOld.Default;
        }

        // AI: bass:choice mapping=0=root,1=fifth,2=octave; weights come from settings; octave biases center up 12.
        // AI: must preserve Debug.Assert that resulting pc equals root or fifth; used by downstream harmony consumers.
        public int SelectBassPitch(HarmonyPitchContext ctx, int bar, decimal onsetBeat)
        {
            var rng = RandomHelpersOld.CreateLocalRng(_settings.Seed, "bass", bar, onsetBeat);

            int rootPc = ctx.ChordRootPitchClass;
            int fifthPc = (rootPc + 7) % 12;

            // Weighted choice: 0=root, 1=fifth, 2=octave (root in higher register)
            int choice = RandomHelpersOld.WeightedChoice(rng, [
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

            int midi = RandomHelpersOld.PickMidiNearRange(targetPc, BassMinMidi, BassMaxMidi, center);

            // Validation: pitch class must be root or fifth, and in scale
            Debug.Assert(
                PitchClassUtils.ToPitchClass(midi) == rootPc || 
                PitchClassUtils.ToPitchClass(midi) == fifthPc,
                $"Bass pitch class {PitchClassUtils.ToPitchClass(midi)} is neither root {rootPc} nor fifth {fifthPc}");

            return midi;
        }

        // AI: guitar:strongBeat -> choose chord tones only; weak beat may use diatonic neighbors as passing tones.
        // AI: passing:true only when previousPitchClass present and probability check passes; prefer non-chord neighbors.
        public (int midi, int pitchClass) SelectGuitarPitch(
            HarmonyPitchContext ctx, 
            int bar, 
            decimal onsetBeat, 
            int? previousPitchClass)
        {
            var rng = RandomHelpersOld.CreateLocalRng(_settings.Seed, "guitar", bar, onsetBeat);
            bool isStrong = RandomHelpersOld.IsStrongBeat(onsetBeat);

            int pc;

            if (isStrong)
            {
                // Strong beat: chord tones only
                var chordTones = ctx.ChordPitchClasses.ToList();
                pc = RandomHelpersOld.ChooseRandom(rng, chordTones);
            }
            else
            {
                // Weak beat: possibly use a passing tone
                bool usePassingTone = previousPitchClass.HasValue &&
                                      rng.NextDouble() < _settings.GuitarPassingToneProbability;

                if (usePassingTone)
                {
                    var neighbors = RandomHelpersOld.GetDiatonicNeighbors(
                        previousPitchClass!.Value, 
                        ctx.KeyScalePitchClasses);

                    // Prefer neighbors that are NOT chord tones (true passing tones)
                    var nonChordNeighbors = neighbors
                        .Where(n => !RandomHelpersOld.IsChordTone(n, ctx))
                        .ToList();

                    if (nonChordNeighbors.Count > 0)
                    {
                        pc = RandomHelpersOld.ChooseRandom(rng, nonChordNeighbors);
                    }
                    else if (neighbors.Count > 0)
                    {
                        pc = RandomHelpersOld.ChooseRandom(rng, neighbors);
                    }
                    else
                    {
                        // Fallback to chord tone
                        pc = RandomHelpersOld.ChooseRandom(rng, ctx.ChordPitchClasses.ToList());
                    }
                }
                else
                {
                    // Use chord tone
                    pc = RandomHelpersOld.ChooseRandom(rng, ctx.ChordPitchClasses.ToList());
                }
            }

            int midi = RandomHelpersOld.PickMidiNearRange(pc, GuitarMinMidi, GuitarMaxMidi, GuitarPreferredCenter);

            // Validation
            Debug.Assert(
                RandomHelpersOld.IsInScale(pc, ctx),
                $"Guitar pitch class {pc} is not in scale");
            Debug.Assert(
                !isStrong || RandomHelpersOld.IsChordTone(pc, ctx),
                $"Guitar pitch class {pc} on strong beat is not a chord tone");

            return (midi, pc);
        }

        // AI: keys: returns ChordRealization instead of raw MIDI list; consumers use .MidiNotes to emit events.
        // AI: ninth selection uses next scale degree above root; ensure no duplicates; all returned pcs must be in scale.
        // AI: sectionProfile: if provided, uses ColorToneProbability instead of global KeysAdd9Probability.
        public ChordRealization SelectKeysVoicing(
            HarmonyPitchContext ctx, 
            int bar, 
            decimal onsetBeat, 
            bool isFirstOnsetOfHarmony,
            SectionProfile? sectionProfile = null)
        {
            var rng = RandomHelpersOld.CreateLocalRng(_settings.Seed, "keys", bar, onsetBeat);

            // Start with the base chord voicing
            var midiNotes = ctx.ChordMidiNotes.ToList();

            // Track if we added a color tone
            bool hasColorTone = false;
            string? colorToneTag = null;

            // Determine color tone probability (section profile overrides global setting)
            double colorToneProbability = sectionProfile?.ColorToneProbability ?? _settings.KeysAdd9Probability;

            // Optionally add a diatonic 9th on the first onset
            if (isFirstOnsetOfHarmony && rng.NextDouble() < colorToneProbability)
            {
                int rootPc = ctx.ChordRootPitchClass;
                int scaleIdx = RandomHelpersOld.FindScaleIndex(rootPc, ctx.KeyScalePitchClasses);

                if (scaleIdx >= 0)
                {
                    // 9th is scale degree 2 above root = (scaleIdx + 1) % 7
                    int ninthPc = ctx.KeyScalePitchClasses[(scaleIdx + 1) % 7];

                    // Place 9th above the chord (in the upper register)
                    int ninthMidi = RandomHelpersOld.PickMidiNearRange(
                        ninthPc, 
                        KeysMinMidi + 12,  // Above bass range of chord
                        KeysMaxMidi, 
                        KeysPreferredCenter + 12);

                    if (!midiNotes.Contains(ninthMidi))
                    {
                        midiNotes.Add(ninthMidi);
                        hasColorTone = true;
                        colorToneTag = "add9";
                    }
                }
            }

            // Apply density constraint from section profile
            if (sectionProfile != null && midiNotes.Count > sectionProfile.MaxDensity)
            {
                // Trim to max density by keeping lowest notes (bass/guide tones)
                midiNotes = midiNotes.Take(sectionProfile.MaxDensity).ToList();
            }

            // Calculate register center as median
            int registerCenter = midiNotes.Count > 0
                ? midiNotes[midiNotes.Count / 2]
                : KeysPreferredCenter;

            return new ChordRealization
            {
                MidiNotes = midiNotes,
                Inversion = ctx.Bass,
                RegisterCenterMidi = registerCenter,
                HasColorTone = hasColorTone,
                ColorToneTag = colorToneTag,
                Density = midiNotes.Count
            };
        }

        // AI: inversion: options depend on chord size; keep mapping stable as callers expect these string values.
        public string SelectChordInversion(HarmonyPitchContext ctx, int bar, decimal onsetBeat)
        {
            var rng = RandomHelpersOld.CreateLocalRng(_settings.Seed, "keys-inversion", bar, onsetBeat);

            // Determine available inversions based on chord size
            var options = ctx.ChordPitchClasses.Count >= 4
                ? new[] { "root", "3rd", "5th", "7th" }
                : new[] { "root", "3rd", "5th" };

            int idx = rng.NextInt(0, options.Length);
            return options[idx];
        }

        // AI: drums:humanize=±10% baseVelocity clamped to 1..127; rng key includes drum type for consistent variation.
        public int SelectDrumVelocity(int bar, decimal onsetBeat, string drumType, int baseVelocity)
        {
            var rng = RandomHelpersOld.CreateLocalRng(_settings.Seed, $"drum_{drumType}", bar, onsetBeat);
            
            // Apply ±10% variation for humanization
            int variation = (int)(baseVelocity * 0.1);
            int minVel = Math.Max(1, baseVelocity - variation);
            int maxVel = Math.Min(127, baseVelocity + variation);
            
            return rng.NextInt(minVel, maxVel + 1);
        }
    }
}