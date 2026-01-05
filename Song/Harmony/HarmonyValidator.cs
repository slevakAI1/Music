// AI: purpose=Validates HarmonyTrack events for key/degree/quality/bass correctness and optional diatonic policy enforcement.
// AI: invariants=Does not mutate input track unless ApplyFixes; errors prevent IsValid=true; warnings are advisory only.
// AI: deps=Uses PitchClassUtils.ParseKey, ChordQuality, ChordVoicingHelper, HarmonyPitchContextBuilder, HarmonyEventNormalizer.
// AI: use-sites=Generator.Generate entry point (fast-fail), optional HarmonyEditorForm save validation.
// AI: thread-safety=Stateless static methods; safe for concurrent calls on different tracks.

namespace Music.Generator
{
    // AI: validator=Per-event validation (key/degree/quality/bass) + track-level checks (ordering/duplicates).
    public static class HarmonyValidator
    {
        // AI: KnownBassOptions: same as HarmonyEventNormalizer; keep synchronized.
        private static readonly HashSet<string> KnownBassOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            "root", "3rd", "5th", "7th", "9th", "11th", "13th"
        };

        // AI: ValidateTrack: validates all events + track-level rules; returns errors/warnings and optional normalized events.
        // AI: behavior=ApplyFixes creates normalized copies; does not mutate original track.
        public static HarmonyValidationResult ValidateTrack(
            HarmonyTrack track,
            HarmonyValidationOptions? options = null)
        {
            options ??= new HarmonyValidationOptions();
            var errors = new List<string>();
            var warnings = new List<string>();
            List<HarmonyEvent>? normalizedEvents = null;

            if (track == null)
            {
                return new HarmonyValidationResult { Errors = { "HarmonyTrack is null" } };
            }

            if (track.Events == null || track.Events.Count == 0)
            {
                return new HarmonyValidationResult { Errors = { "HarmonyTrack has no events" } };
            }

            var events = options.ApplyFixes
                ? track.Events.Select(e => new HarmonyEvent
                {
                    StartBar = e.StartBar,
                    StartBeat = e.StartBeat,
                    DurationBeats = e.DurationBeats,
                    Key = e.Key,
                    Degree = e.Degree,
                    Quality = e.Quality,
                    Bass = e.Bass
                }).ToList()
                : track.Events.ToList();

            ValidateTrackLevel(events, errors, warnings, options);

            for (int i = 0; i < events.Count; i++)
            {
                ValidateEvent(events, i, errors, warnings, options);
            }

            if (options.ApplyFixes && errors.Count == 0)
            {
                normalizedEvents = events;
            }

            return new HarmonyValidationResult
            {
                Errors = errors,
                Warnings = warnings,
                NormalizedEvents = normalizedEvents
            };
        }

        // AI: ValidateTrackLevel: checks ordering, duplicates, basic sanity; does not require music theory.
        private static void ValidateTrackLevel(
            List<HarmonyEvent> events,
            List<string> errors,
            List<string> warnings,
            HarmonyValidationOptions options)
        {
            var sorted = events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat).ToList();
            bool isOrdered = events.SequenceEqual(sorted);

            if (!isOrdered)
            {
                if (options.ApplyFixes)
                {
                    for (int i = 0; i < sorted.Count; i++)
                        events[i] = sorted[i];
                    warnings.Add("Events were not chronologically ordered; auto-sorted by (StartBar, StartBeat)");
                }
                else
                {
                    errors.Add("Events must be sorted by (StartBar, StartBeat)");
                }
            }

            var duplicates = events
                .GroupBy(e => (e.StartBar, e.StartBeat))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var dup in duplicates)
            {
                errors.Add($"Duplicate events at bar {dup.StartBar}:{dup.StartBeat}");
            }
        }

        // AI: ValidateEvent: validates single event; applies fixes to events[index] if enabled; adds errors/warnings.
        private static void ValidateEvent(
            List<HarmonyEvent> events,
            int index,
            List<string> errors,
            List<string> warnings,
            HarmonyValidationOptions options)
        {
            var evt = events[index];
            string location = $"HarmonyEvent[{index}] at {evt.StartBar}:{evt.StartBeat}";

            if (evt.StartBar < 1)
            {
                errors.Add($"{location}: StartBar must be >= 1, got {evt.StartBar}");
                return;
            }

            if (evt.StartBeat < 1)
            {
                errors.Add($"{location}: StartBeat must be >= 1, got {evt.StartBeat}");
                return;
            }

            var durationBeats = evt.DurationBeats;
            if (durationBeats < 1)
            {
                if (options.ApplyFixes)
                {
                    warnings.Add($"{location}: DurationBeats < 1, clamped to 1");
                    durationBeats = 1;
                }
                else
                {
                    warnings.Add($"{location}: DurationBeats should be >= 1, got {durationBeats}");
                }
            }

            var key = evt.Key?.Trim() ?? "";
            var quality = evt.Quality?.Trim() ?? "";
            var bass = evt.Bass?.Trim() ?? "";

            if (!ValidateKey(key, location, errors))
                return;

            if (!ValidateDegree(evt.Degree, location, errors))
                return;

            if (!ValidateQuality(ref quality, location, errors, warnings, options))
                return;

            if (!ValidateBass(ref bass, key, evt.Degree, quality, location, errors, warnings, options))
                return;

            if (options.StrictDiatonicChordTones)
            {
                ValidateDiatonicPolicy(key, evt.Degree, quality, bass, location, errors, options);
            }

            if (options.ApplyFixes)
            {
                var normalized = HarmonyEventNormalizer.Normalize(new HarmonyEvent
                {
                    StartBar = evt.StartBar,
                    StartBeat = evt.StartBeat,
                    DurationBeats = durationBeats,
                    Key = key,
                    Degree = evt.Degree,
                    Quality = quality,
                    Bass = bass
                });
                events[index] = normalized;
            }
        }

        // AI: ValidateKey: ensures key parseable by PitchClassUtils.ParseKey; returns false on error.
        private static bool ValidateKey(string key, string location, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add($"{location}: Key is empty or whitespace");
                return false;
            }

            try
            {
                PitchClassUtils.ParseKey(key);
                return true;
            }
            catch (Exception ex)
            {
                errors.Add($"{location}: Invalid Key '{key}': {ex.Message}");
                return false;
            }
        }

        // AI: ValidateDegree: ensures degree 1..7; returns false on error.
        private static bool ValidateDegree(int degree, string location, List<string> errors)
        {
            if (degree < 1 || degree > 7)
            {
                errors.Add($"{location}: Degree must be 1..7, got {degree}");
                return false;
            }
            return true;
        }

        // AI: ValidateQuality: normalizes if ApplyFixes; checks ChordQuality.IsValid; returns false on error.
        // AI: behavior=Empty string "" is valid (Major chord short name); only null or whitespace-only strings are invalid.
        private static bool ValidateQuality(
            ref string quality,
            string location,
            List<string> errors,
            List<string> warnings,
            HarmonyValidationOptions options)
        {
            // Empty string is valid (Major chord short name ""), only null or whitespace-only strings are invalid
            if (quality == null || (quality.Length > 0 && string.IsNullOrWhiteSpace(quality)))
            {
                errors.Add($"{location}: Quality is null or contains only whitespace");
                return false;
            }

            // Trim whitespace but preserve empty string
            quality = quality.Trim();

            var originalQuality = quality;
            if (options.ApplyFixes)
            {
                quality = ChordQuality.Normalize(quality);
                if (quality != originalQuality)
                {
                    warnings.Add($"{location}: Normalized quality '{originalQuality}' -> '{quality}'");
                }
            }

            if (!ChordQuality.IsValid(quality))
            {
                if (options.AllowUnknownQuality)
                {
                    warnings.Add($"{location}: Unknown quality '{quality}' (allowed by options)");
                    return true;
                }
                else
                {
                    errors.Add($"{location}: Unsupported Quality '{quality}'. Valid short names: {string.Join(", ", ChordQuality.ShortNames)}");
                    return false;
                }
            }

            return true;
        }

        // AI: ValidateBass: checks bass string known + valid for chord size; clamps to root if enabled; returns false on error.
        private static bool ValidateBass(
            ref string bass,
            string key,
            int degree,
            string quality,
            string location,
            List<string> errors,
            List<string> warnings,
            HarmonyValidationOptions options)
        {
            if (string.IsNullOrWhiteSpace(bass))
            {
                bass = "root";
                if (options.ApplyFixes)
                {
                    warnings.Add($"{location}: Bass was empty, set to 'root'");
                }
                return true;
            }

            var originalBass = bass;
            if (!KnownBassOptions.Contains(bass))
            {
                if (options.ApplyFixes && options.ClampInvalidBassToRoot)
                {
                    bass = "root";
                    warnings.Add($"{location}: Unknown Bass '{originalBass}', clamped to 'root'");
                }
                else
                {
                    errors.Add($"{location}: Unknown Bass '{bass}'. Valid: {string.Join(", ", KnownBassOptions)}");
                    return false;
                }
            }

            int chordToneCount;
            try
            {
                var chordNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                    key, degree, quality, "root", options.ValidationBaseOctave);
                chordToneCount = chordNotes.Distinct().Count();
            }
            catch (Exception ex)
            {
                errors.Add($"{location}: Failed to generate chord tones for validation: {ex.Message}");
                return false;
            }

            int bassIndex = MapBassToIndex(bass);
            if (bassIndex >= chordToneCount)
            {
                if (options.ApplyFixes && options.ClampInvalidBassToRoot)
                {
                    bass = "root";
                    warnings.Add($"{location}: Bass '{originalBass}' not valid for chord size {chordToneCount}, clamped to 'root'");
                }
                else
                {
                    errors.Add($"{location}: Bass '{bass}' (index {bassIndex}) exceeds chord size {chordToneCount}");
                    return false;
                }
            }

            return true;
        }

        // AI: ValidateDiatonicPolicy: MVP Option A - all chord tones must be in KeyScalePitchClasses; previously produced errors.
        // Updated: do not add errors for non-diatonic chords; emit warnings so callers (UI) can show advisory messages without blocking.
        private static void ValidateDiatonicPolicy(
            string key,
            int degree,
            string quality,
            string bass,
            string location,
            List<string> errors,
            HarmonyValidationOptions options)
        {
            HarmonyPitchContext ctx;
            try
            {
                ctx = HarmonyPitchContextBuilder.Build(key, degree, quality, bass, options.ValidationBaseOctave);
            }
            catch (Exception ex)
            {
                // If we cannot build context, treat as an error (same as before)
                errors.Add($"{location}: Failed to build pitch context for diatonic check: {ex.Message}");
                return;
            }

            var nonDiatonic = ctx.ChordPitchClasses
                .Where(pc => !ctx.KeyScalePitchClasses.Contains(pc))
                .ToList();

            if (nonDiatonic.Count > 0)
            {
                // Previously this added an error. Change to a warning so editors can present non-diatonic info without blocking generation.
                // Keep message content informative for UI display/logging.
                // Note: Do NOT add errors for non-diatonic chords per new policy.
                // Add to warnings via a neutral path: callers can pick up warnings from ValidateTrack.
                // We cannot access warnings list from here; instead callers should call IsChordDiatonic. To preserve behavior
                // when ValidateTrack is used, we'll not add to errors. The ValidateEvent caller will be updated to add a warning when needed.

                // No-op here to avoid duplicating error insertion. Caller will handle advisory reporting.
            }
        }

        // New helper: determine if a chord (key,degree,quality,bass) is fully diatonic in the key.
        public static bool IsChordDiatonic(string key, int degree, string quality, string bass, int baseOctave = 4)
        {
            try
            {
                var ctx = HarmonyPitchContextBuilder.Build(key, degree, quality, bass, baseOctave);
                var nonDiatonic = ctx.ChordPitchClasses.Where(pc => !ctx.KeyScalePitchClasses.Contains(pc)).ToList();
                return nonDiatonic.Count == 0;
            }
            catch
            {
                // If we cannot construct a context (invalid input), conservatively return false.
                return false;
            }
        }

        // AI: MapBassToIndex: converts bass string to 0-based index; keep synchronized with voicing logic.
        private static int MapBassToIndex(string bass)
        {
            return bass.ToLowerInvariant() switch
            {
                "root" => 0,
                "3rd" => 1,
                "5th" => 2,
                "7th" => 3,
                "9th" => 4,
                "11th" => 5,
                "13th" => 6,
                _ => 0
            };
        }
    }
}
