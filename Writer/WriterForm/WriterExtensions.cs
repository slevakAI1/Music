using System;
using System.Collections.Generic;
using System.Linq;
using Music.Designer;
using Music.Writer;

/// <summary>
///   THIS APPLIES DESIGN TO Writer
/// 
namespace Music.Writer
{
    public static class WriterExtensions
    {
        // Applies design-driven defaults into an existing Writer instance.
        // Accept a nullable receiver so callers holding a `Writer?` (e.g. Globals.GenerationData)
        // can call this extension directly without a null-forgiving operator.
        public static void UpdateFromDesigner(this WriterData? data, Designer.Designer? design)
        {
            if (data == null) return;

            // ========== PARTS SYNCHRONIZATION ==========
            // Update the Writer parts to be in sync with the Designer parts if there are differences.
            // Preserve existing checked states

            var availableParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (design?.PartSet?.Parts != null)
            {
                foreach (var v in design.PartSet.Parts)
                {
                    var name = v?.PartName;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableParts.Add(name!);
                }
            }

            var existingParts = data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newPartsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingParts.Count == 0)
            {
                // No existing state: default all parts to checked
                foreach (var name in availableParts)
                    newPartsState[name] = true;
            }
            else
            {
                // Preserve existing checked states for parts that still exist
                foreach (var name in availableParts)
                {
                    if (existingParts.TryGetValue(name, out var isChecked))
                        newPartsState[name] = isChecked;
                    else
                        newPartsState[name] = false; // New parts default to unchecked
                }
            }

            data.PartsState = newPartsState;

            // ========== SECTIONS SYNCHRONIZATION ==========
            // Update the Writer sections to be in sync with the Designer sections if there are differences.
            // Preserve existing checked states

            var availableSections = new List<string>();
            if (design?.SectionSet?.Sections != null)
            {
                foreach (var section in design.SectionSet.Sections)
                {
                    var name = section?.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableSections.Add(name!);
                }
            }

            var existingSections = data.SectionsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newSectionsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingSections.Count == 0)
            {
                // No existing state: default all sections to unchecked (unlike parts, sections default to unchecked)
                foreach (var name in availableSections)
                    newSectionsState[name] = false;
            }
            else
            {
                // Preserve existing checked states for sections that still exist
                foreach (var name in availableSections)
                {
                    if (existingSections.TryGetValue(name, out var isChecked))
                        newSectionsState[name] = isChecked;
                    else
                        newSectionsState[name] = false; // New sections default to unchecked
                }
            }

            data.SectionsState = newSectionsState;

            // ========== END BAR ADJUSTMENT ==========
            // Ensure EndBar is within valid range based on design's total bars

            var total = design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
            {
                if (!data.EndBar.HasValue)
                    data.EndBar = total;
                else
                    data.EndBar = Math.Max(1, Math.Min(total, data.EndBar.Value));
            }
        }

        /// <summary>
        /// Converts Writer data to a AppendNotesParams for use with AppendNotes.
        /// </summary>
        public static AppendNotesParams ToAppendNotesParams(this WriterData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var parts = (data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase))
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            var selectedStaffs = (data.SelectedStaffs ?? new List<int>()).Any() 
                ? data.SelectedStaffs 
                : new List<int> { 1 }; // Default to staff 1 if none selected

            var notes = new List<WriterNote>();

            // Get tuplet settings from writer data
            // TupletNumber is now a string; check if it's populated
            bool hasTuplet = !string.IsNullOrWhiteSpace(data.TupletNumber);
            string? tupletNumber = hasTuplet ? data.TupletNumber : null;
            int tupletActualNotes = hasTuplet ? (data.TupletCount ?? 3) : 0;
            int tupletNormalNotes = hasTuplet ? (data.TupletOf ?? 2) : 0;

            if (data.IsChord ?? false)
            {
                // Convert chord to list of WriterNote
                var chordNotes = ChordConverter.Convert(
                    data.ChordKey,
                    (int)data.ChordDegree,
                    data.ChordQuality,
                    data.ChordBase,
                    baseOctave: data.Octave,
                    noteValue: GetNoteValue(data.NoteValue),
                    numberOfNotes: data.NumberOfNotes.GetValueOrDefault());
                
                // PER CHATGPT 5.0 Tuples start goes only on the Base Chord Note...guess will just use the first one for now.
                // Apply tuplet settings to the first chord note only if tuplet is specified
                if (hasTuplet && chordNotes.Count > 0)
                {
                    chordNotes[0].TupletNumber = tupletNumber;
                    chordNotes[0].TupletActualNotes = tupletActualNotes;
                    chordNotes[0].TupletNormalNotes = tupletNormalNotes;
                }
                
                notes.AddRange(chordNotes);
            }
            else
            {
                // Single note or rest
                // Handle case where NumberOfNotes > 1: create multiple WriterNote instances
                // This handles Case 1 (e.g., 3 identical eighth notes forming a triplet)
                int numberOfNotes = data.NumberOfNotes.GetValueOrDefault();
                
                for (int i = 0; i < numberOfNotes; i++)
                {
                    var writerNote = new WriterNote
                    {
                        Step = data.Step != '\0' ? data.Step : 'C',
                        Octave = data.Octave,
                        NoteValue = GetNoteValue(data.NoteValue),
                        NumberOfNotes = 1, // Each instance represents a single note
                        IsChord = false,
                        IsRest = data.IsRest ?? false,
                        Alter = GetAlter(data.Accidental),

                        // Apply tuplet settings to each note if tuplet is specified
                        TupletNumber = hasTuplet ? tupletNumber : null,
                        TupletActualNotes = tupletActualNotes,
                        TupletNormalNotes = tupletNormalNotes
                    };
                    
                    notes.Add(writerNote);
                }
            }

            var config = new AppendNotesParams
            {
                Parts = parts,
                Staffs = selectedStaffs!,
                StartBar = data.StartBar.GetValueOrDefault(),
                Notes = notes
            };

            return config;
        }

        private static int GetNoteValue(string? noteValueString)
        {
            if (noteValueString != null && Music.MusicConstants.NoteValueMap.TryGetValue(noteValueString, out var nv))
            {
                return nv;
            }
            return 4; // default quarter note
        }

        private static int GetAlter(string? accidental)
        {
            return (accidental ?? "Natural") switch
            {
                var s when s.Equals("Sharp", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.Equals("Flat", StringComparison.OrdinalIgnoreCase) => -1,
                _ => 0
            };
        }
    }

    /// <summary>
    /// Configuration extracted from Writer for easier processing by SetNotes.
    /// </summary>
    public sealed class AppendNotesParams
    {
        // TARGETS
        public List<string> Parts { get; set; } = new();
        public List<int> Staffs { get; set; } = new();
        public int StartBar { get; set; }
        public int StartBeat { get; set; }

        // List of notes to insert
        public List<WriterNote> Notes { get; set; } = new();
    }
}