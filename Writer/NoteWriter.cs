using System;
using System.Text.Json;
using MusicXml.Domain;
using Music.Designer;
using Music;

namespace Music.Writer
{
    /// <summary>
    /// Inserts a set of notes based on user parameters.
    /// Method assumes the score has already been initialized with parts, measure, tempo, time signature.
    /// All parameters are expected to be pre-validated.
    /// </summary>
    public static class NoteWriter
    {
        /// <summary>
        /// Adds notes to the specified score based on the provided configuration.
        /// All parameters are expected to be pre-validated.
        /// </summary>
        public static void Append(Score score, AppendNotesParams config)
        {
            var debugConfig = Helpers.DebugObject(config);

            if (score == null) throw new ArgumentNullException(nameof(score));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Notes == null || config.Notes.Count == 0) return;

            foreach (var scorePart in GetTargetParts(score, config.Parts))
            {
                ProcessPart(scorePart, config);
            }

            MessageBoxHelper.ShowMessage("Pattern has been applied to the score.", "Apply Pattern Set Notes");
        }

        private static IEnumerable<Part> GetTargetParts(Score score, List<string> partNames)
        {
            return (score.Parts ?? Enumerable.Empty<Part>())
                .Where(p => p?.Name != null && partNames.Contains(p.Name));
        }

        private static void ProcessPart(Part scorePart, AppendNotesParams config)
        {
            if (scorePart.Measures == null || scorePart.Measures.Count == 0)
                return;

            //============================================================
            // Check if this part allows two staves
            var twoStaffVoices = VoiceCatalog.GetTwoStaffVoices();
            bool allowsTwoStaves = twoStaffVoices.Contains(scorePart.Name, StringComparer.OrdinalIgnoreCase);

            // Validate staff selections
            var targetStaffs = config.Staffs.OrderBy(s => s).ToList();
            if (targetStaffs.Any(s => s == 2) && !allowsTwoStaves)
            {
                throw new InvalidOperationException(
                    $"Part '{scorePart.Name}' does not support a second staff. " +
                    $"Only these voices support two staves: {string.Join(", ", twoStaffVoices)}");
            }

            //============================================================
            // Process each target staff
            for (int staffIndex = 0; staffIndex < targetStaffs.Count; staffIndex++)
            {
                var staff = targetStaffs[staffIndex];

                // initial position
                int currentBar = config.StartBar;
                long currentBeatPosition = 0;

                // Track duration written per measure for backup calculation
                var durationPerMeasure = new Dictionary<int, long>();

                // Tuplet tracking for this staff pass: key = WriterNote.TupletNumber (string)
                var tupletStates = new Dictionary<string, TupletState>(StringComparer.OrdinalIgnoreCase);

                foreach (var writerNote in config.Notes)
                {
                    //============================================================
                    // Prepare current measure/info for this note

                    // When a measure fills up, the next note writes to the next measure

                    // Get Measure info for the current bar
                    var measure = scorePart.Measures[currentBar - 1];
                    var measureInfo = GetMeasureInfo(measure);

                    // Calculate divisions for the note's duration based on the current measure's divisions.
                    var noteDurationInMeasure = CalculateNoteDurationInMeasure(measureInfo.Divisions, writerNote.Duration);

                    // Adjust duration when the note is part of a tuplet:
                    // duration = baseDuration * (normal / actual) (rounded to nearest int)
                    if (noteDurationInMeasure > 0
                        && writerNote.TupletActualNotes > 0
                        && writerNote.TupletNormalNotes > 0)
                    {
                        noteDurationInMeasure = (int)Math.Round(
                            (double)noteDurationInMeasure * writerNote.TupletNormalNotes / writerNote.TupletActualNotes);
                    }

                    // current measure exactly full, advance to the start of the next measure
                    if (writerNote.IsChord)
                    {
                        // do nothing for measure advancement on chord tones
                    }
                    else if (currentBeatPosition == measureInfo.BarLengthDivisions)
                    {
                        currentBar++;
                        currentBeatPosition = 0;
                    }
                    else if (currentBeatPosition + noteDurationInMeasure > measureInfo.BarLengthDivisions)
                    {
                        // This needs a tie between measures - Future enhancement - TBD
                    }

                    // There is no next measure
                    if (currentBar > scorePart.Measures.Count)
                    {
                        MessageBoxHelper.ShowMessage(
                             $"Ran out of measures in part '{scorePart.Name}' at bar {currentBar}. Not all notes were placed.",
                             "Insufficient Measures");
                        return;
                    }

                    // Get the current measure info in case it advanced to next bar
                    measure = scorePart.Measures[currentBar - 1];
                    measureInfo = GetMeasureInfo(measure);

                    // NOTE: This drop thru logic is not the best


                    //============================================================

                    // Compose Note element (duration property always set to real duration)
                    var note = new Note
                    {
                        Type = DurationTypeString(writerNote.Duration),
                        Duration = noteDurationInMeasure,
                        Voice = staff == 1 ? 1 : 5,  // TODO this is probably bs
                        Staff = staff,
                        IsChordTone = writerNote.IsChord,
                        IsRest = writerNote.IsRest
                    };

                    if (!writerNote.IsRest)
                    {
                        note.Pitch = new Pitch
                        {
                            Step = char.ToUpper(writerNote.Step),
                            Octave = writerNote.Octave,
                            Alter = writerNote.Alter
                        };
                    }

                    // ===========================
                    // Tuplet handling: set TimeModification on every note in the tuplet,
                    // and apply TupletNotation 'start' on the first base note and 'stop' on the last base note.
                    // We treat TupletNumber (string) as the tuplet id within this staff pass.
                    if (!string.IsNullOrWhiteSpace(writerNote.TupletNumber)
                        && writerNote.TupletActualNotes > 0
                        && writerNote.TupletNormalNotes > 0)
                    {
                        var key = writerNote.TupletNumber!;
                        if (!tupletStates.TryGetValue(key, out var ts))
                        {
                            // initialize new tuplet state
                            int parsedNum = 1;
                            int.TryParse(key, out parsedNum);
                            ts = new TupletState
                            {
                                Actual = writerNote.TupletActualNotes,
                                Normal = writerNote.TupletNormalNotes,
                                Remaining = writerNote.TupletActualNotes,
                                Number = parsedNum,
                                IsStarted = false
                            };
                            tupletStates[key] = ts;
                        }

                        // Set time modification on the note (affects playback/timing)
                        note.TimeModification = new TimeModification
                        {
                            ActualNotes = ts.Actual,
                            NormalNotes = ts.Normal,
                            // set NormalType to the printed type (e.g., "eighth", "quarter")
                            NormalType = DurationTypeString(writerNote.Duration)
                        };

                        // If this is the first base note encountered for the tuplet, mark start
                        if (!ts.IsStarted)
                        {
                            note.TupletNotation = new TupletNotation
                            {
                                Type = "start",
                                Number = ts.Number
                            };
                            ts.IsStarted = true;
                        }

                        // Decrement tuplet remaining only when this is the base (non-chord) note
                        if (!writerNote.IsChord)
                        {
                            ts.Remaining--;
                            if (ts.Remaining <= 0)
                            {
                                // Last base note of the tuplet -> mark stop on this note
                                // (overwrites 'start' if the tuplet length was 1)
                                note.TupletNotation = new TupletNotation
                                {
                                    Type = "stop",
                                    Number = ts.Number
                                };

                                // remove the state so the same id may be reused later
                                tupletStates.Remove(key);
                            }
                        }
                    }

                    // Add note to the current measure (currentBar)
                    measure.MeasureElements.Add(new MeasureElement
                    {
                        Type = MeasureElementType.Note,
                        Element = note
                    });

                    // Update position tracking
                    if (!writerNote.IsChord)
                    {
                        currentBeatPosition += noteDurationInMeasure;
                        
                        // Track duration per measure
                        if (!durationPerMeasure.ContainsKey(currentBar))
                            durationPerMeasure[currentBar] = 0;
                        durationPerMeasure[currentBar] += noteDurationInMeasure;
                    }
                }

                // Update all of the stave 1 measures to include a backup tag if 2 staves targeted
                if (staffIndex < targetStaffs.Count - 1)
                {
                    foreach (var measureEntry in durationPerMeasure.OrderBy(kvp => kvp.Key))
                    {
                        int measureNumber = measureEntry.Key;
                        long durationWritten = measureEntry.Value;

                        if (durationWritten > 0 && measureNumber <= scorePart.Measures.Count)
                        {
                            var measure = scorePart.Measures[measureNumber - 1];
                            measure.MeasureElements ??= new List<MeasureElement>();
                            measure.MeasureElements.Add(new MeasureElement
                            {
                                Type = MeasureElementType.Backup,
                                Element = new Backup { Duration = (int)durationWritten }
                            });
                        }
                    }
                }
            }
        }

        private static MeasureInfo GetMeasureInfo(Measure measure)
        {
            var divisions = Math.Max(1, measure.Attributes?.Divisions ?? 1);
            var beatsPerBar = measure.Attributes?.Time?.Beats ?? 4;
            var existingDuration = CalculateExistingDuration(measure);

            return new MeasureInfo
            {
                Divisions = divisions,
                BeatsPerBar = beatsPerBar,
                BarLengthDivisions = divisions * beatsPerBar,
                ExistingDuration = existingDuration
            };
        }

        private static long CalculateExistingDuration(Measure measure)
        {
            long duration = 0;
            foreach (var me in measure.MeasureElements ?? Enumerable.Empty<MeasureElement>())
            {
                if (me?.Type == MeasureElementType.Note &&
                    me.Element is Note n &&
                    n.Duration > 0 &&
                    !n.IsChordTone)
                {
                    duration += n.Duration;
                }
            }
            return duration;
        }

        private static int CalculateNoteDurationInMeasure(int divisions, int noteValue)
        {
            int numerator = divisions * 4;
            if (numerator % noteValue != 0)
            {
                // Calculate the minimum divisions needed for this note value
                // For noteValue=32, need divisions≥8; for noteValue=64, need divisions≥16
                int minDivisions = (noteValue + 3) / 4; // Round up: (32+3)/4=8, (64+3)/4=16

                var msg = $"Cannot represent note value '{noteValue}' (1/{noteValue} note) with divisions={divisions}.\n" +
                          $"Minimum required divisions: {minDivisions}.\n\n" +
                          $"To fix this:\n" +
                          $"1. Create a new score (divisions will be set to 8)\n" +
                          $"2. Or manually edit the score's divisions value";
                MessageBoxHelper.ShowError(msg, "Invalid Duration");
                return 0;
            }
            return numerator / noteValue;
        }

        private static string DurationTypeString(int denom) => denom switch
        {
            1 => "whole",
            2 => "half",
            4 => "quarter",
            8 => "eighth",
            16 => "16th",
            32 => "32nd",
            64 => "64th",
            _ => "quarter"
        };

        public sealed class MeasureInfo
        {
            public int Divisions { get; set; }
            public int BeatsPerBar { get; set; }
            public int BarLengthDivisions { get; set; }
            public long ExistingDuration { get; set; }
        }

        // Local helper to track tuplet lifecycle for a given tuplet id within a staff pass.
        private sealed class TupletState
        {
            public int Actual { get; set; }
            public int Normal { get; set; }
            public int Remaining { get; set; }
            public int Number { get; set; }
            public bool IsStarted { get; set; }
        }
    }
}