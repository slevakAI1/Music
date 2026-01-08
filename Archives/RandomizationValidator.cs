//// AI: purpose=Runtime validator for pitch randomization used in tests/debug; verifies constraints, determinism, and variation.
//// AI: invariants=Validators treat Warnings as non-fatal; IsValid == (Errors.Count==0); do not change that semantics.
//// AI: deps=relies on PitchClassUtils, RandomHelpers, and PitchRandomizer seeding; changing RNG/seed breaks determinism tests.
//// AI: perf=not hotpath; used in test/debug only; keep error strings stable for test assertions.
//// TODO? confirm bass/guitar range thresholds mirror production ranges in PitchRandomizer constants.

//using System.Text;

//namespace Music.Generator
//{
//    // AI: ValidationResult: IsValid derives from Errors only; Warnings are advisory and preserved for diagnostics.
//    public static class RandomizationValidator
//    {
//        public sealed class ValidationResult
//        {
//            public bool IsValid { get; init; }
//            public List<string> Errors { get; init; } = new();
//            public List<string> Warnings { get; init; } = new();

//            public override string ToString()
//            {
//                var sb = new StringBuilder();
//                sb.AppendLine($"Valid: {IsValid}");
                
//                if (Errors.Count > 0)
//                {
//                    sb.AppendLine("Errors:");
//                    foreach (var e in Errors)
//                        sb.AppendLine($"  - {e}");
//                }
                
//                if (Warnings.Count > 0)
//                {
//                    sb.AppendLine("Warnings:");
//                    foreach (var w in Warnings)
//                        sb.AppendLine($"  - {w}");
//                }
                
//                return sb.ToString();
//            }
//        }

//        // AI: ValidateBassPitch: error if not root/fifth or not in key; range warning is advisory and may differ from generator consts.
//        public static ValidationResult ValidateBassPitch(int midiNote, HarmonyPitchContext ctx)
//        {
//            var errors = new List<string>();
//            var warnings = new List<string>();

//            int pc = PitchClassUtils.ToPitchClass(midiNote);
//            int rootPc = ctx.ChordRootPitchClass;
//            int fifthPc = (rootPc + 7) % 12;

//            // Bass must be root or fifth
//            if (pc != rootPc && pc != fifthPc)
//            {
//                errors.Add($"Bass pitch class {pc} is neither root ({rootPc}) nor fifth ({fifthPc})");
//            }

//            // Must be in scale
//            if (!RandomHelpers.IsInScale(pc, ctx))
//            {
//                errors.Add($"Bass pitch class {pc} is not in the key scale");
//            }

//            // Range check (warning only)
//            if (midiNote < 28 || midiNote > 55)
//            {
//                warnings.Add($"Bass MIDI {midiNote} is outside typical bass range (28-55)");
//            }

//            return new ValidationResult
//            {
//                IsValid = errors.Count == 0,
//                Errors = errors,
//                Warnings = warnings
//            };
//        }

//        // AI: ValidateGuitarPitch: errors for out-of-scale or non-chord on strong beats; range is advisory.
//        public static ValidationResult ValidateGuitarPitch(
//            int midiNote, 
//            HarmonyPitchContext ctx, 
//            bool isStrongBeat)
//        {
//            var errors = new List<string>();
//            var warnings = new List<string>();

//            int pc = PitchClassUtils.ToPitchClass(midiNote);

//            // Must always be in scale
//            if (!RandomHelpers.IsInScale(pc, ctx))
//            {
//                errors.Add($"Guitar pitch class {pc} is not in the key scale");
//            }

//            // Strong beats must be chord tones
//            if (isStrongBeat && !RandomHelpers.IsChordTone(pc, ctx))
//            {
//                errors.Add($"Guitar pitch class {pc} on strong beat is not a chord tone");
//            }

//            // Range check (warning only)
//            if (midiNote < 40 || midiNote > 88)
//            {
//                warnings.Add($"Guitar MIDI {midiNote} is outside typical guitar range (40-88)");
//            }

//            return new ValidationResult
//            {
//                IsValid = errors.Count == 0,
//                Errors = errors,
//                Warnings = warnings
//            };
//        }

//        // AI: ValidateKeysVoicing: errors if any note outside key; warns if root missing (voicing may still be valid).
//        public static ValidationResult ValidateKeysVoicing(
//            IReadOnlyList<int> midiNotes, 
//            HarmonyPitchContext ctx)
//        {
//            var errors = new List<string>();
//            var warnings = new List<string>();

//            foreach (var midi in midiNotes)
//            {
//                int pc = PitchClassUtils.ToPitchClass(midi);

//                // All notes must be in scale
//                if (!RandomHelpers.IsInScale(pc, ctx))
//                {
//                    errors.Add($"Keys pitch class {pc} (MIDI {midi}) is not in the key scale");
//                }
//            }

//            // Should contain at least the root
//            bool hasRoot = midiNotes.Any(m => 
//                PitchClassUtils.ToPitchClass(m) == ctx.ChordRootPitchClass);
            
//            if (!hasRoot)
//            {
//                warnings.Add("Keys voicing does not contain the chord root");
//            }

//            return new ValidationResult
//            {
//                IsValid = errors.Count == 0,
//                Errors = errors,
//                Warnings = warnings
//            };
//        }

//        // AI: TestDeterminism: uses two independent randomizers with same settings; any RNG/seed change breaks this.
//        public static ValidationResult TestDeterminism(
//            RandomizationSettings settings, 
//            HarmonyPitchContext ctx,
//            int testIterations = 10)
//        {
//            var errors = new List<string>();
//            var randomizer1 = new PitchRandomizer(settings);
//            var randomizer2 = new PitchRandomizer(settings);

//            for (int bar = 1; bar <= testIterations; bar++)
//            {
//                for (decimal beat = 1m; beat <= 4m; beat += 0.5m)
//                {
//                    // Test bass
//                    int bass1 = randomizer1.SelectBassPitch(ctx, bar, beat);
//                    int bass2 = randomizer2.SelectBassPitch(ctx, bar, beat);
//                    if (bass1 != bass2)
//                    {
//                        errors.Add($"Bass non-deterministic at bar {bar}, beat {beat}: {bass1} vs {bass2}");
//                    }

//                    // Test guitar
//                    var guitar1 = randomizer1.SelectGuitarPitch(ctx, bar, beat, null);
//                    var guitar2 = randomizer2.SelectGuitarPitch(ctx, bar, beat, null);
//                    if (guitar1.midi != guitar2.midi)
//                    {
//                        errors.Add($"Guitar non-deterministic at bar {bar}, beat {beat}: {guitar1.midi} vs {guitar2.midi}");
//                    }

//                    // Test keys (first beat only for add9 test)
//                    bool isFirst = bar == 1 && beat == 1m;
//                    var keys1 = randomizer1.SelectKeysVoicing(ctx, bar, beat, isFirst);
//                    var keys2 = randomizer2.SelectKeysVoicing(ctx, bar, beat, isFirst);
//                    if (!keys1.MidiNotes.SequenceEqual(keys2.MidiNotes))
//                    {
//                        errors.Add($"Keys non-deterministic at bar {bar}, beat {beat}");
//                    }
//                }
//            }

//            return new ValidationResult
//            {
//                IsValid = errors.Count == 0,
//                Errors = errors
//            };
//        }

//        // AI: TestVariation: checks that different seeds produce some difference; it's heuristic and reports warning if none found.
//        public static ValidationResult TestVariation(HarmonyPitchContext ctx)
//        {
//            var errors = new List<string>();
//            var warnings = new List<string>();

//            var settings1 = new RandomizationSettings { Seed = 12345 };
//            var settings2 = new RandomizationSettings { Seed = 54321 };

//            var randomizer1 = new PitchRandomizer(settings1);
//            var randomizer2 = new PitchRandomizer(settings2);

//            bool anyDifferent = false;

//            for (int bar = 1; bar <= 5; bar++)
//            {
//                for (decimal beat = 1m; beat <= 4m; beat += 1m)
//                {
//                    int bass1 = randomizer1.SelectBassPitch(ctx, bar, beat);
//                    int bass2 = randomizer2.SelectBassPitch(ctx, bar, beat);
//                    if (bass1 != bass2) anyDifferent = true;

//                    var guitar1 = randomizer1.SelectGuitarPitch(ctx, bar, beat, null);
//                    var guitar2 = randomizer2.SelectGuitarPitch(ctx, bar, beat, null);
//                    if (guitar1.midi != guitar2.midi) anyDifferent = true;
//                }
//            }

//            if (!anyDifferent)
//            {
//                warnings.Add("Different seeds produced identical results - randomization may not be working");
//            }

//            return new ValidationResult
//            {
//                IsValid = true, // Not an error, just a warning
//                Warnings = warnings
//            };
//        }
//    }
//}