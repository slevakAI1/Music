using Music.Designer;
using MusicXml.Domain;

namespace Music.Writer
{
    internal static class AppendNotesHelper
    {
        public static void AddBackupElementsIfNeeded(
            Part scorePart, 
            int staffIndex, 
            int totalStaffs, 
            MeasureMeta usedDivisionsPerMeasure)
        {
            if (staffIndex >= totalStaffs - 1)
                return;

            var staffNumber = scorePart != null ? staffIndex + 1 : 1; // Convert index to staff number

            // Get entries for this specific part and staff
            var relevantEntries = usedDivisionsPerMeasure.GetDivisionsUsedForPartAndStaff(scorePart.Name, staffNumber);

            foreach (var (measureNumber, durationWritten) in relevantEntries)
            {
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

        private static void ApplyTupletNotation(
            MusicXml.Domain.Note note, 
            PitchEvent pitchEvent, 
            AppendNotes.TupletState ts, 
            Dictionary<string, AppendNotes.TupletState> tupletStates, 
            string key)
        {
            // Mark start on first base note
            if (!ts.IsStarted)
            {
                note.TupletNotation = new TupletNotation
                {
                    Type = "start",
                    Number = ts.Number
                };
                ts.IsStarted = true;
            }

            // Decrement and mark stop on last base note
            if (!pitchEvent.IsChord)
            {
                ts.Remaining--;
                if (ts.Remaining <= 0)
                {
                    note.TupletNotation = new TupletNotation
                    {
                        Type = "stop",
                        Number = ts.Number
                    };
                    tupletStates.Remove(key);
                }
            }
        }

        public static void ApplyTupletSettings(
            MusicXml.Domain.Note note, 
            PitchEvent pitchEvent, 
            Dictionary<string, AppendNotes.TupletState> tupletStates)
        {
            if (string.IsNullOrWhiteSpace(pitchEvent.TupletNumber)
                || pitchEvent.TupletActualNotes <= 0
                || pitchEvent.TupletNormalNotes <= 0)
            {
                return;
            }

            var key = pitchEvent.TupletNumber!;
            if (!tupletStates.TryGetValue(key, out var ts))
            {
                ts = InitializeTupletState(key, pitchEvent);
                tupletStates[key] = ts;
            }

            // Set time modification on the note
            note.TimeModification = new TimeModification
            {
                ActualNotes = ts.Actual,
                NormalNotes = ts.Normal,
                NormalType = DurationTypeString(pitchEvent.Duration)
            };

            // Handle tuplet notation (start/stop markers)
            ApplyTupletNotation(note, pitchEvent, ts, tupletStates, key);
        }

        private static int CalculateNoteDurationInMeasure(int divisions, int noteValue)
        {
            int numerator = divisions * 4;  // TO DO   this is the 4 beats (quarter note = 1 beat) per measure
            if (numerator % noteValue != 0)
            {
                // Calculate the minimum divisions needed for this note value
                // For noteValue=32, need divisions?8; for noteValue=64, need divisions?16
                int minDivisions = (noteValue + 3) / 4; // Round up: (32+3)/4=8, (64+3)/4=16

                var msg = $"Cannot represent note value '{noteValue}' (1/{noteValue} note) with divisions={divisions}.\n" +
                          $"Minimum required divisions: {minDivisions}.\n\n" +
                          $"To fix this:\n" +
                          $"1. Create a new score (divisions will be set to {MusicConstants.DefaultDivisions})\n" +
                          $"2. Or manually edit the score's divisions value";
                MessageBoxHelper.ShowError(msg, "Invalid Duration");
                return 0;
            }
            return numerator / noteValue;
        }

        public static int CalculateTotalNoteDuration(int divisions, PitchEvent pitchEvent)
        {
            var baseDuration = CalculateNoteDurationInMeasure(divisions, pitchEvent.Duration);
            
            // Calculate total duration including dots
            var noteDurationInMeasure = baseDuration;
            if (pitchEvent.Dots == 1)
            {
                noteDurationInMeasure = baseDuration + (baseDuration / 2);
            }
            else if (pitchEvent.Dots == 2)
            {
                noteDurationInMeasure = baseDuration + (baseDuration / 2) + (baseDuration / 4);
            }

            // Adjust duration when the note is part of a tuplet
            if (noteDurationInMeasure > 0
                && pitchEvent.TupletActualNotes > 0
                && pitchEvent.TupletNormalNotes > 0)
            {
                noteDurationInMeasure = (int)Math.Round(
                    (double)noteDurationInMeasure * pitchEvent.TupletNormalNotes / pitchEvent.TupletActualNotes);
            }

            return noteDurationInMeasure;
        }

        public static MusicXml.Domain.Note ComposeNote(PitchEvent pitchEvent, int duration, int staff)
        {
            var note = new MusicXml.Domain.Note
            {
                Type = DurationTypeString(pitchEvent.Duration),
                Duration = duration,
                Voice = staff == 1 ? 1 : 5,
                Staff = staff,
                IsChordTone = pitchEvent.IsChord,
                IsRest = pitchEvent.IsRest,
                Dots = pitchEvent.Dots
            };

            if (!pitchEvent.IsRest)
            {
                note.Pitch = new Pitch
                {
                    Step = char.ToUpper(pitchEvent.Step),
                    Octave = pitchEvent.Octave,
                    Alter = pitchEvent.Alter
                };
            }

            return note;
        }

        public static MusicXml.Domain.Note CreateTiedNote(
            PitchEvent pitchEvent, 
            int duration, 
            int divisions, 
            int staff, 
            bool isFirstPart)
        {
            var note = new MusicXml.Domain.Note
            {
                Type = DurationTypeForTiedNote(duration, divisions),
                Duration = duration,
                Voice = staff == 1 ? 1 : 5,
                Staff = staff,
                IsChordTone = pitchEvent.IsChord,
                IsRest = pitchEvent.IsRest,
                Tie = pitchEvent.IsRest ? Tie.NotTied : (isFirstPart ? Tie.Start : Tie.Stop),
                Dots = 0  // Tied notes across measures typically don't use dots
            };

            if (!pitchEvent.IsRest)
            {
                note.Pitch = new Pitch
                {
                    Step = char.ToUpper(pitchEvent.Step),
                    Octave = pitchEvent.Octave,
                    Alter = pitchEvent.Alter
                };
            }

            return note;
        }

        private static string DurationTypeForTiedNote(long duration, int divisions)
        {
            // Common durations in terms of divisions
            var wholeDuration = divisions * 4;
            var halfDuration = divisions * 2;
            var quarterDuration = divisions;
            var eighthDuration = divisions / 2;
            var sixteenthDuration = divisions / 4;

            if (duration >= wholeDuration) return "whole";
            if (duration >= halfDuration) return "half";
            if (duration >= quarterDuration) return "quarter";
            if (duration >= eighthDuration) return "eighth";
            if (duration >= sixteenthDuration) return "16th";
            
            return "quarter"; // fallback
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

        public static bool EnsureMeasureAvailable(Part scorePart, int currentBar, string partName)
        {
            if (currentBar > scorePart.Measures.Count)
            {
                MessageBoxHelper.ShowMessage(
                     $"Ran out of measures in part '{partName}' at bar {currentBar}. Not all notes were placed.",
                     "Insufficient Measures");
                return false;
            }
            return true;
        }

        public static AppendNotes.MeasureInfo GetMeasureInfo(Measure measure)
        {
            var divisions = Math.Max(1, measure.Attributes?.Divisions ?? MusicConstants.DefaultDivisions);
            var beatsPerBar = measure.Attributes?.Time?.Beats ?? 4;

            return new AppendNotes.MeasureInfo
            {
                Divisions = divisions,
                BeatsPerBar = beatsPerBar,
                BarLengthDivisions = divisions * beatsPerBar
            };
        }

        public static bool HandleTiedChordAcrossMeasures(
            Part scorePart,
            List<PitchEvent> pitchEvents,
            AppendNotes.StaffProcessingContext context,
            AppendNotes.MeasureInfo measureInfo,
            int noteDuration,
            MeasureMeta usedDivisionsPerMeasure)
        {
            long durationInCurrentMeasure = measureInfo.BarLengthDivisions - context.CurrentBeatPosition;
            long durationInNextMeasure = noteDuration - durationInCurrentMeasure;

            // Place first part of tied chord notes in current measure
            var measure = scorePart.Measures[context.CurrentBar - 1];

            foreach (var chordNote in pitchEvents)
            {
                var firstChordNote = CreateTiedNote(
                    chordNote,
                    (int)durationInCurrentMeasure,
                    measureInfo.Divisions,
                    context.Staff,
                    isFirstPart: true);

                measure.MeasureElements.Add(new MeasureElement
                {
                    Type = MeasureElementType.Note,
                    Element = firstChordNote
                });
            }

            usedDivisionsPerMeasure.AddDivisionsUsed(scorePart.Name, context.Staff, context.CurrentBar, durationInCurrentMeasure);

            // Advance to next measure
            context.CurrentBar++;
            context.CurrentBeatPosition = 0;

            // Check if next measure exists
            if (context.CurrentBar > scorePart.Measures.Count)
            {
                MessageBoxHelper.ShowMessage(
                     $"Ran out of measures in part '{scorePart.Name}' at bar {context.CurrentBar}. Not all notes were placed.",
                     "Insufficient Measures");
                return false;
            }

            // Place second part of tied chord notes in next measure
            var nextMeasure = scorePart.Measures[context.CurrentBar - 1];

            foreach (var chordNote in pitchEvents)
            {
                var secondChordNote = CreateTiedNote(
                    chordNote,
                    (int)durationInNextMeasure,
                    measureInfo.Divisions,
                    context.Staff,
                    isFirstPart: false);

                nextMeasure.MeasureElements.Add(new MeasureElement
                {
                    Type = MeasureElementType.Note,
                    Element = secondChordNote
                });
            }

            context.CurrentBeatPosition = durationInNextMeasure;
            usedDivisionsPerMeasure.AddDivisionsUsed(scorePart.Name, context.Staff, context.CurrentBar, durationInNextMeasure);

            return true;
        }

        public static bool HandleTiedNoteAcrossMeasures(
            Part scorePart, 
            PitchEvent pitchEvent,
            AppendNotes.StaffProcessingContext context,
            AppendNotes.MeasureInfo measureInfo,
            int noteDuration,
            MeasureMeta usedDivisionsPerMeasure)
        {
            long durationInCurrentMeasure = measureInfo.BarLengthDivisions - context.CurrentBeatPosition;
            long durationInNextMeasure = noteDuration - durationInCurrentMeasure;

            // Place first part of tied note in current measure
            var measure = scorePart.Measures[context.CurrentBar - 1];
            var firstNote = CreateTiedNote(
                pitchEvent, 
                (int)durationInCurrentMeasure, 
                measureInfo.Divisions, 
                context.Staff, 
                isFirstPart: true);

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = firstNote
            });

            usedDivisionsPerMeasure.AddDivisionsUsed(scorePart.Name, context.Staff, context.CurrentBar, durationInCurrentMeasure);

            // Advance to next measure
            context.CurrentBar++;
            context.CurrentBeatPosition = 0;

            // Check if next measure exists
            if (context.CurrentBar > scorePart.Measures.Count)
            {
                MessageBoxHelper.ShowMessage(
                     $"Ran out of measures in part '{scorePart.Name}' at bar {context.CurrentBar}. Not all notes were placed.",
                     "Insufficient Measures");
                return false;
            }

            // Place second part of tied note in next measure
            var nextMeasure = scorePart.Measures[context.CurrentBar - 1];
            var secondNote = CreateTiedNote(
                pitchEvent, 
                (int)durationInNextMeasure, 
                measureInfo.Divisions, 
                context.Staff, 
                isFirstPart: false);

            nextMeasure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = secondNote
            });

            context.CurrentBeatPosition = durationInNextMeasure;
            usedDivisionsPerMeasure.AddDivisionsUsed(scorePart.Name, context.Staff, context.CurrentBar, durationInNextMeasure);

            return true;
        }

        private static AppendNotes.TupletState InitializeTupletState(string key, PitchEvent pitchEvent)
        {
            int parsedNum = 1;
            int.TryParse(key, out parsedNum);
            
            return new AppendNotes.TupletState
            {
                Actual = pitchEvent.TupletActualNotes,
                Normal = pitchEvent.TupletNormalNotes,
                Remaining = pitchEvent.TupletActualNotes,
                Number = parsedNum,
                IsStarted = false
            };
        }

        public static void UpdateUsedDivisionsPerMeasure(
            ref Dictionary<string, long> usedDivisionsPerMeasure, 
            string partName, 
            int staff, 
            int currentBar, 
            long duration)
        {
            // Create composite key: "PartName|Staff|Measure"
            var key = $"{partName}|{staff}|{currentBar}";
            
            if (!usedDivisionsPerMeasure.ContainsKey(key))
                usedDivisionsPerMeasure[key] = 0;
            usedDivisionsPerMeasure[key] += duration;
        }

        public static void UpdatePositionTracking(
            AppendNotes.StaffProcessingContext context, 
            PitchEvent pitchEvent, 
            int noteDuration,
            string partName,
            MeasureMeta usedDivisionsPerMeasure)
        {
            if (!pitchEvent.IsChord)
            {
                context.CurrentBeatPosition += noteDuration;
                usedDivisionsPerMeasure.AddDivisionsUsed(partName, context.Staff, context.CurrentBar, noteDuration);
            }
        }

        /// <summary>
        /// Finds the first measure and beat position where notes can be appended for a given part and staff.
        /// Returns (measureNumber, beatPosition) based on MeasureMeta tracking.
        /// </summary>
        public static (int measureNumber, long beatPosition) FindAppendStartPosition(
            Part scorePart,
            int staff,
            MeasureMeta usedDivisionsPerMeasure)
        {
            if (scorePart?.Measures == null || scorePart.Measures.Count == 0)
                return (1, 0);

            // Query all divisions used for this part and staff
            var allDivisions = usedDivisionsPerMeasure.GetDivisionsUsedForPartAndStaff(scorePart.Name, staff);

            // If no data exists, start at measure 1, position 0
            if (allDivisions.Count == 0)
                return (1, 0);

            // Find the highest measure number with divisions > 0
            var lastUsedMeasure = allDivisions
                .Where(x => x.duration > 0)
                .OrderByDescending(x => x.measureNumber)
                .FirstOrDefault();

            // If no measure has any divisions used, start at measure 1
            if (lastUsedMeasure.measureNumber == 0)
                return (1, 0);

            // Get the measure to check its capacity
            int measureIndex = lastUsedMeasure.measureNumber - 1;
            if (measureIndex < 0 || measureIndex >= scorePart.Measures.Count)
                return (1, 0);

            var measure = scorePart.Measures[measureIndex];
            var measureInfo = GetMeasureInfo(measure);

            // If measure is full, start at the next measure
            if (lastUsedMeasure.duration >= measureInfo.BarLengthDivisions)
            {
                int nextMeasure = lastUsedMeasure.measureNumber + 1;
                return (nextMeasure, 0);
            }

            // Measure has space; return current measure with used divisions as position
            return (lastUsedMeasure.measureNumber, lastUsedMeasure.duration);
        }
    }
}
