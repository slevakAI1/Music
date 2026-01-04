// AI: purpose=Unit tests for HarmonyValidator covering all validation rules and edge cases.
// AI: invariants=Tests validate error/warning messages match spec; ApplyFixes behavior tested separately.
// AI: deps=Relies on HarmonyValidator, ChordQuality, HarmonyEventNormalizer for expected behavior.

namespace Music.Generator
{
    public static class HarmonyValidatorTests
    {
        // AI: RunAll: executes all test methods; returns true if all pass.
        public static bool RunAll()
        {
            bool allPassed = true;

            allPassed &= TestEmptyTrack();
            allPassed &= TestNullTrack();
            allPassed &= TestInvalidKey();
            allPassed &= TestInvalidDegree();
            allPassed &= TestInvalidQuality();
            allPassed &= TestUnknownBass();
            allPassed &= TestBassExceedsChordSize();
            allPassed &= TestNonDiatonicChordTones();
            allPassed &= TestDuplicateStartPositions();
            allPassed &= TestUnsortedEvents();
            allPassed &= TestApplyFixes();
            allPassed &= TestValidTrack();

            return allPassed;
        }

        private static bool TestEmptyTrack()
        {
            var track = new HarmonyTrack();
            var result = HarmonyValidator.ValidateTrack(track);

            if (result.IsValid)
                return Fail("Empty track should be invalid");
            if (!result.Errors.Any(e => e.Contains("no events")))
                return Fail("Expected 'no events' error");

            return Pass("TestEmptyTrack");
        }

        private static bool TestNullTrack()
        {
            var result = HarmonyValidator.ValidateTrack(null!);

            if (result.IsValid)
                return Fail("Null track should be invalid");
            if (!result.Errors.Any(e => e.Contains("null")))
                return Fail("Expected 'null' error");

            return Pass("TestNullTrack");
        }

        private static bool TestInvalidKey()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "InvalidKey",
                Degree = 1,
                Quality = "",
                Bass = "root"
            });

            var result = HarmonyValidator.ValidateTrack(track);

            if (result.IsValid)
                return Fail("Invalid key should fail validation");
            if (!result.Errors.Any(e => e.Contains("Invalid Key")))
                return Fail("Expected 'Invalid Key' error");

            return Pass("TestInvalidKey");
        }

        private static bool TestInvalidDegree()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "C major",
                Degree = 8,
                Quality = "",
                Bass = "root"
            });

            var result = HarmonyValidator.ValidateTrack(track);

            if (result.IsValid)
                return Fail("Degree 8 should fail validation");
            if (!result.Errors.Any(e => e.Contains("Degree must be 1..7")))
                return Fail("Expected degree range error");

            return Pass("TestInvalidDegree");
        }

        private static bool TestInvalidQuality()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "C major",
                Degree = 1,
                Quality = "invalid-quality",
                Bass = "root"
            });

            var result = HarmonyValidator.ValidateTrack(track);

            if (result.IsValid)
                return Fail("Invalid quality should fail validation");
            if (!result.Errors.Any(e => e.Contains("Unsupported Quality")))
                return Fail("Expected 'Unsupported Quality' error");

            return Pass("TestInvalidQuality");
        }

        private static bool TestUnknownBass()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "C major",
                Degree = 1,
                Quality = "",
                Bass = "invalid-bass"
            });

            var options = new HarmonyValidationOptions
            {
                ApplyFixes = false,
                ClampInvalidBassToRoot = false
            };

            var result = HarmonyValidator.ValidateTrack(track, options);

            if (result.IsValid)
                return Fail("Unknown bass should fail validation");
            if (!result.Errors.Any(e => e.Contains("Unknown Bass")))
                return Fail("Expected 'Unknown Bass' error");

            return Pass("TestUnknownBass");
        }

        private static bool TestBassExceedsChordSize()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "C major",
                Degree = 1,
                Quality = "",
                Bass = "7th"
            });

            var options = new HarmonyValidationOptions
            {
                ApplyFixes = false,
                ClampInvalidBassToRoot = false
            };

            var result = HarmonyValidator.ValidateTrack(track, options);

            if (result.IsValid)
                return Fail("7th bass on triad should fail validation");
            if (!result.Errors.Any(e => e.Contains("exceeds chord size")))
                return Fail("Expected chord size error");

            return Pass("TestBassExceedsChordSize");
        }

        private static bool TestNonDiatonicChordTones()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "C major",
                Degree = 5,
                Quality = "m",
                Bass = "root"
            });

            var options = new HarmonyValidationOptions
            {
                StrictDiatonicChordTones = true
            };

            var result = HarmonyValidator.ValidateTrack(track, options);

            if (result.IsValid)
                return Fail("V minor in C major should fail strict diatonic check");
            if (!result.Errors.Any(e => e.Contains("not fully diatonic")))
                return Fail("Expected diatonic error");

            return Pass("TestNonDiatonicChordTones");
        }

        private static bool TestDuplicateStartPositions()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent { StartBar = 1, StartBeat = 1, Key = "C major", Degree = 1, Quality = "" });
            track.Add(new HarmonyEvent { StartBar = 1, StartBeat = 1, Key = "C major", Degree = 5, Quality = "7" });

            var result = HarmonyValidator.ValidateTrack(track);

            if (result.IsValid)
                return Fail("Duplicate start positions should fail validation");
            if (!result.Errors.Any(e => e.Contains("Duplicate events")))
                return Fail("Expected duplicate error");

            return Pass("TestDuplicateStartPositions");
        }

        private static bool TestUnsortedEvents()
        {
            var track = new HarmonyTrack();
            track.Events.Clear();
            track.Events.Add(new HarmonyEvent { StartBar = 3, Key = "C major", Degree = 1, Quality = "" });
            track.Events.Add(new HarmonyEvent { StartBar = 1, Key = "C major", Degree = 1, Quality = "" });

            var options = new HarmonyValidationOptions { ApplyFixes = false };
            var result = HarmonyValidator.ValidateTrack(track, options);

            if (result.IsValid)
                return Fail("Unsorted events should fail validation when ApplyFixes=false");
            if (!result.Errors.Any(e => e.Contains("sorted")))
                return Fail("Expected sorting error");

            return Pass("TestUnsortedEvents");
        }

        private static bool TestApplyFixes()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "C MAJOR",
                Degree = 1,
                Quality = "Major",
                Bass = "ROOT"
            });

            var options = new HarmonyValidationOptions
            {
                ApplyFixes = true,
                StrictDiatonicChordTones = false
            };

            var result = HarmonyValidator.ValidateTrack(track, options);

            if (!result.IsValid)
                return Fail($"Valid track with unnormalized fields should pass with ApplyFixes: {string.Join("; ", result.Errors)}");

            if (result.NormalizedEvents == null)
                return Fail("ApplyFixes should produce NormalizedEvents");

            var normalized = result.NormalizedEvents[0];
            if (normalized.Key != "C major")
                return Fail($"Expected key 'C major', got '{normalized.Key}'");
            if (normalized.Quality != "")
                return Fail($"Expected quality '', got '{normalized.Quality}'");
            if (normalized.Bass != "root")
                return Fail($"Expected bass 'root', got '{normalized.Bass}'");

            if (result.Warnings.Count == 0)
                return Fail("Expected warnings for normalization changes");

            return Pass("TestApplyFixes");
        }

        private static bool TestValidTrack()
        {
            var track = new HarmonyTrack();
            track.Add(new HarmonyEvent
            {
                StartBar = 1,
                Key = "C major",
                Degree = 1,
                Quality = "",
                Bass = "root"
            });
            track.Add(new HarmonyEvent
            {
                StartBar = 3,
                Key = "C major",
                Degree = 5,
                Quality = "7",
                Bass = "root"
            });

            var result = HarmonyValidator.ValidateTrack(track);

            if (!result.IsValid)
                return Fail($"Valid track should pass validation: {string.Join("; ", result.Errors)}");

            if (result.Errors.Count > 0)
                return Fail("Valid track should have no errors");

            return Pass("TestValidTrack");
        }

        private static bool Pass(string testName)
        {
            Console.WriteLine($"? {testName} passed");
            return true;
        }

        private static bool Fail(string message)
        {
            Console.WriteLine($"? FAIL: {message}");
            return false;
        }
    }
}
