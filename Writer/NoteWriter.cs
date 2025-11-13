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

                // Sum of durations (divisions) written for non-chord notes on this staff (used for backup)
                long totalDurationWritten = 0;


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

                    // Add note to the current measure (currentBar)
                    measure.MeasureElements.Add(new MeasureElement
                    {
                        Type = MeasureElementType.Note,
                        Element = note
                    });

                    // Update position tracking for non-chord notes
                    if (!writerNote.IsChord)
                    {
                        currentBeatPosition += noteDurationInMeasure;
                        totalDurationWritten += noteDurationInMeasure;
                    }
                }

                // Insert backup (if there is a following staff) using totalDurationWritten.
                if (staffIndex < targetStaffs.Count - 1 && totalDurationWritten > 0)
                {
                    // Determine which measure to insert the backup into:
                    // If currentBeatPosition == 0 then the next insertion point is at the start of currentBar,
                    // so the last written content is in currentBar - 1. Otherwise it's currentBar.
                    int backupBar = currentBeatPosition == 0 ? currentBar - 1 : currentBar;
                    backupBar = Math.Clamp(backupBar, 1, scorePart.Measures.Count);

                    var backupMeasure = scorePart.Measures[backupBar - 1];
                    backupMeasure.MeasureElements ??= new List<MeasureElement>();
                    backupMeasure.MeasureElements.Add(new MeasureElement
                    {
                        Type = MeasureElementType.Backup,
                        Element = new Backup { Duration = (int)totalDurationWritten }
                    });
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
    }
}