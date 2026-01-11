// AI: purpose=Create read-only HarmonyPitchContext from event/params; used by generators to constrain pitch choices.
// AI: invariants=Produces ChordPitchClasses and KeyScalePitchClasses as sorted unique pitch classes.
// AI: deps=Relies on PitchClassUtils, ChordVoicingHelper.GenerateChordMidiNotes; changing those breaks this builder.
// AI: errors=Throws ArgumentOutOfRange/ArgumentNull/InvalidOperation when inputs or voicing are invalid; callers should handle.

namespace Music.Generator
{
    public static class HarmonyPitchContextBuilder
    {
        // AI: Build(harmonyEvent): null-checks then forwards to param Build; preserves SourceEvent for debugging.
        public static HarmonyPitchContext Build(HarmonyEvent harmonyEvent, int baseOctave = 4, HarmonyPolicy? policy = null)
        {
            if (harmonyEvent == null)
                throw new ArgumentNullException(nameof(harmonyEvent));

            return Build(
                harmonyEvent.Key,
                harmonyEvent.Degree,
                harmonyEvent.Quality,
                harmonyEvent.Bass,
                baseOctave,
                policy,
                harmonyEvent);
        }

        // AI: Build(params): key steps: parse key->pitchclass, get scale pcs, validate degree, get chord MIDI notes, dedupe/sort, map to pcs.
        // AI: invariants=Degree must be 1..7; ChordMidiNotes are deduped/sorted; ChordPitchClasses are unique sorted 0-11 values.
        // AI: policy=When AllowNonDiatonicChordTones=false (default), filters chord tones to only include diatonic pitch classes.
        public static HarmonyPitchContext Build(
            string key,
            int degree,
            string quality,
            string bass,
            int baseOctave = 4,
            HarmonyPolicy? policy = null,
            HarmonyEvent? sourceEvent = null)
        {
            // Use default policy if not provided
            policy ??= HarmonyPolicy.Default;

            // Step 1: Get the key root pitch class
            int keyRootPitchClass = PitchClassUtils.ParseKeyToPitchClass(key);

            // Step 2: Get the scale pitch classes for the key
            var keyScalePitchClasses = PitchClassUtils.GetScalePitchClassesForKey(key);

            // Step 3: Calculate the chord root pitch class from the scale degree
            if (degree < 1 || degree > 7)
                throw new ArgumentOutOfRangeException(nameof(degree), degree, "Degree must be 1-7");

            int chordRootPitchClass = keyScalePitchClasses[degree - 1];

            // Step 4: Get chord tones as MIDI note numbers using the shared helper
            var chordMidiNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                key: key,
                degree: degree,
                quality: quality,
                bass: bass,
                baseOctave: baseOctave);

            // Step 5: Deduplicate, sort chord MIDI notes for predictable generator behavior
            var sortedChordMidiNotes = chordMidiNotes
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            // Step 6: Convert to pitch classes (0-11), unique and sorted
            var chordPitchClasses = sortedChordMidiNotes
                .Select(PitchClassUtils.ToPitchClass)
                .Distinct()
                .OrderBy(pc => pc)
                .ToList();

            // Step 7: Apply policy-based filtering
            if (!policy.AllowNonDiatonicChordTones)
            {
                // Filter chord tones to only include diatonic pitch classes
                var diatonicChordPitchClasses = chordPitchClasses
                    .Where(pc => keyScalePitchClasses.Contains(pc))
                    .ToList();

                var nonDiatonicPitchClasses = chordPitchClasses
                    .Where(pc => !keyScalePitchClasses.Contains(pc))
                    .ToList();

                // Filter MIDI notes to match the diatonic chord pitch classes
                var diatonicChordMidiNotes = sortedChordMidiNotes
                    .Where(midi => diatonicChordPitchClasses.Contains(PitchClassUtils.ToPitchClass(midi)))
                    .ToList();

                chordPitchClasses = diatonicChordPitchClasses;
                sortedChordMidiNotes = diatonicChordMidiNotes;
            }

            // Step 8: Build and return the context
            return new HarmonyPitchContext
            {
                SourceEvent = sourceEvent,
                KeyRootPitchClass = keyRootPitchClass,
                ChordRootPitchClass = chordRootPitchClass,
                ChordPitchClasses = chordPitchClasses,
                KeyScalePitchClasses = keyScalePitchClasses,
                ChordMidiNotes = sortedChordMidiNotes,
                BaseOctaveUsed = baseOctave,
                Key = key,
                Degree = degree,
                Quality = quality,
                Bass = bass
            };
        }

        // Minimal convenience: convert built context into a ChordRealization for voicing-aware consumers
        public static ChordRealization ToChordRealization(this HarmonyPitchContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var midiNotes = ctx.ChordMidiNotes ?? Array.Empty<int>();

            // Determine register center as median MIDI note if available
            int registerCenter = midiNotes.Count > 0
                ? midiNotes[midiNotes.Count / 2]
                : 60; // default C4

            // Heuristic: detect color tone by checking quality string for common add/alter tokens
            bool hasColor = !string.IsNullOrWhiteSpace(ctx.Quality) &&
                            (ctx.Quality.Contains("add", StringComparison.OrdinalIgnoreCase) ||
                             ctx.Quality.Contains("#", StringComparison.OrdinalIgnoreCase) ||
                             ctx.Quality.Contains("b", StringComparison.OrdinalIgnoreCase));

            string? colorTag = null;
            if (hasColor)
            {
                // crude extract of addN tag
                var idx = ctx.Quality.IndexOf("add", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var token = ctx.Quality.Substring(idx);
                    colorTag = token;
                }
            }

            return new ChordRealization
            {
                MidiNotes = midiNotes,
                Inversion = ctx.Bass ?? string.Empty,
                RegisterCenterMidi = registerCenter,
                HasColorTone = hasColor,
                ColorToneTag = colorTag,
                Density = midiNotes.Count
            };
        }
    }
}