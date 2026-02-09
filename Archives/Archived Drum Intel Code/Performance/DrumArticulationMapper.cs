// AI: purpose=Maps DrumArticulation enum to MIDI note numbers (GM2 standard); provides fallback for unsupported articulations.
// AI: invariants=Deterministic mapping; same articulation+role → same MIDI note; always returns valid note [0..127]; null-safe.
// AI: deps=Consumes DrumArticulation, DrumCandidate.Role; outputs MIDI note + metadata; used by DrumCandidateMapper and converters.
// AI: change=Story 6.3; extend mappings for future articulations or custom kits via configuration.

// NOTE: Per reasearch, will need VST to really implement some, maybe all articulations (e.g., flams, chokes).

using System.Collections.Generic;

namespace Music.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Maps drum articulation hints to MIDI note numbers using GM2 (General MIDI Level 2) standard.
    /// Provides graceful fallback to standard notes when articulations are unavailable.
    /// Story 6.3: Implement Articulation Mapping.
    /// </summary>
    public static class DrumArticulationMapper
    {
        // GM2 Standard Drum Note Mappings (MIDI channel 10)
        // Reference: GM2 specification for drum note assignments
        private static readonly IReadOnlyDictionary<string, int> StandardRoleNotes = new Dictionary<string, int>
        {
            { "Kick", 36 },            // Bass Drum 1 (GM2: Acoustic Bass Drum)
            { "Snare", 38 },           // Acoustic Snare (GM2: standard snare)
            { "ClosedHat", 42 },       // Closed Hi-Hat (GM2: standard closed hat)
            { "OpenHat", 46 },         // Open Hi-Hat (GM2: standard open hat)
            { "Crash", 49 },           // Crash Cymbal 1 (GM2: primary crash)
            { "Crash2", 57 },          // Crash Cymbal 2 (GM2: secondary crash)
            { "Ride", 51 },            // Ride Cymbal 1 (GM2: ride bow)
            { "Tom1", 48 },            // Hi Mid Tom (GM2: high tom)
            { "Tom2", 47 },            // Low Mid Tom (GM2: mid tom)
            { "FloorTom", 41 },        // Low Floor Tom (GM2: floor tom)
            { "RideBell", 53 }         // Ride Bell (GM2: ride bell)
        };

        // Articulation-specific MIDI note mappings (GM2)
        private static readonly IReadOnlyDictionary<DrumArticulation, int?> ArticulationNotes = new Dictionary<DrumArticulation, int?>
        {
            { DrumArticulation.None, null },             // Use standard role note
            { DrumArticulation.Rimshot, 40 },            // Electric Snare (GM2: snare rimshot approximation)
            { DrumArticulation.SideStick, 37 },          // Side Stick (GM2: cross stick)
            { DrumArticulation.OpenHat, 46 },            // Open Hi-Hat (GM2: explicit open)
            { DrumArticulation.Crash, 49 },              // Crash Cymbal 1 (GM2: standard crash)
            { DrumArticulation.Ride, 51 },               // Ride Cymbal 1 (GM2: ride bow)
            { DrumArticulation.RideBell, 53 },           // Ride Bell (GM2: bell tip)
            { DrumArticulation.CrashChoke, 49 },         // Crash Cymbal 1 (choke handled via duration/CC in future)
            { DrumArticulation.Flam, null }              // No specific GM2 note; handled via timing offset
        };

        /// <summary>
        /// Result of articulation mapping containing MIDI note and metadata.
        /// </summary>
        public sealed record ArticulationMappingResult
        {
            /// <summary>
            /// MIDI note number to play (0-127).
            /// </summary>
            public required int MidiNoteNumber { get; init; }

            /// <summary>
            /// Original articulation intent.
            /// </summary>
            public required DrumArticulation Articulation { get; init; }

            /// <summary>
            /// Role this articulation was mapped for.
            /// </summary>
            public required string Role { get; init; }

            /// <summary>
            /// Whether fallback to standard note was used.
            /// </summary>
            public required bool IsFallback { get; init; }

            /// <summary>
            /// Optional metadata string for advanced renderers (e.g., "Rimshot", "Flam+offset").
            /// </summary>
            public string? ArticulationMetadata { get; init; }
        }

        /// <summary>
        /// Maps a drum articulation for a given role to a MIDI note number.
        /// Always returns a valid, playable MIDI note with fallback to standard role note.
        /// Thread-safe, deterministic, null-safe.
        /// </summary>
        /// <param name="articulation">Articulation hint (None if no specific articulation).</param>
        /// <param name="role">Drum role (e.g., "Snare", "Kick", "ClosedHat").</param>
        /// <returns>Mapping result with MIDI note number and metadata.</returns>
        public static ArticulationMappingResult MapArticulation(DrumArticulation articulation, string role)
        {
            // Null safety: treat null role as unknown, return safe fallback
            if (string.IsNullOrWhiteSpace(role))
            {
                return new ArticulationMappingResult
                {
                    MidiNoteNumber = 38, // Default to snare as safe fallback
                    Articulation = articulation,
                    Role = role ?? "Unknown",
                    IsFallback = true,
                    ArticulationMetadata = "Fallback:NullRole"
                };
            }

            // Handle None articulation: use standard role note
            if (articulation == DrumArticulation.None)
            {
                return MapStandardRole(role, articulation);
            }

            // Attempt articulation-specific mapping
            if (ArticulationNotes.TryGetValue(articulation, out int? articulationNote) && articulationNote.HasValue)
            {
                return new ArticulationMappingResult
                {
                    MidiNoteNumber = ClampMidiNote(articulationNote.Value),
                    Articulation = articulation,
                    Role = role,
                    IsFallback = false,
                    ArticulationMetadata = articulation.ToString()
                };
            }

            // Fallback: use standard role note with articulation metadata
            var standardResult = MapStandardRole(role, articulation);
            return standardResult with
            {
                IsFallback = true,
                ArticulationMetadata = $"{articulation}:Fallback"
            };
        }

        /// <summary>
        /// Maps a role to its standard MIDI note without articulation variation.
        /// </summary>
        private static ArticulationMappingResult MapStandardRole(string role, DrumArticulation articulation)
        {
            if (StandardRoleNotes.TryGetValue(role, out int midiNote))
            {
                return new ArticulationMappingResult
                {
                    MidiNoteNumber = ClampMidiNote(midiNote),
                    Articulation = articulation,
                    Role = role,
                    IsFallback = articulation != DrumArticulation.None,
                    ArticulationMetadata = articulation == DrumArticulation.None ? null : $"{articulation}:StandardRole"
                };
            }

            // Unknown role: return safe fallback (snare)
            return new ArticulationMappingResult
            {
                MidiNoteNumber = 38, // Snare as universal fallback
                Articulation = articulation,
                Role = role,
                IsFallback = true,
                ArticulationMetadata = $"{articulation}:UnknownRole"
            };
        }

        /// <summary>
        /// Clamps MIDI note number to valid range [0..127].
        /// </summary>
        private static int ClampMidiNote(int note)
        {
            if (note < 0) return 0;
            if (note > 127) return 127;
            return note;
        }

        /// <summary>
        /// Gets the standard MIDI note number for a role without articulation.
        /// Returns null if role is unknown.
        /// </summary>
        public static int? GetStandardNoteForRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return null;

            return StandardRoleNotes.TryGetValue(role, out int note) ? note : null;
        }
    }
}
