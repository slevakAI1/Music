// AI: purpose=Maps GM2 drum MIDI notes (36-81) to normalized role names for feature extraction (Story 7.2a).
// AI: invariants=Deterministic mapping; same MIDI note → same role; groups articulations to base role (e.g., 38+40 → "Snare").
// AI: deps=Consumes MIDI note numbers from PartTrackEvent; outputs role strings; used by DrumTrackEventExtractor.
// AI: change=Story 7.2a; extend mappings for additional GM2 notes or custom kits as needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Maps GM2 drum MIDI note numbers to normalized role names.
/// Groups articulation variants to their base role (e.g., 38 Acoustic Snare + 40 Electric Snare → "Snare").
/// Story 7.2a: Raw Event Extraction.
/// </summary>
public static class DrumRoleMapper
{
    // GM2 Drum Note to Role Mapping
    // Reference: General MIDI Level 2 specification for drum note assignments
    // Notes grouped by instrument family with articulation variants consolidated
    private static readonly IReadOnlyDictionary<int, string> MidiNoteToRole = new Dictionary<int, string>
    {
        // Bass Drums (35-36)
        { 35, "Kick" },           // Acoustic Bass Drum
        { 36, "Kick" },           // Bass Drum 1

        // Snares (37-40)
        { 37, "SideStick" },      // Side Stick / Rim Click
        { 38, "Snare" },          // Acoustic Snare
        { 39, "Snare" },          // Hand Clap (grouped with snare for analysis)
        { 40, "Snare" },          // Electric Snare / Rimshot

        // Toms (41-48, 50)
        { 41, "FloorTom" },       // Low Floor Tom
        { 43, "FloorTom" },       // High Floor Tom
        { 45, "Tom2" },           // Low Tom
        { 47, "Tom2" },           // Low-Mid Tom
        { 48, "Tom1" },           // Hi-Mid Tom
        { 50, "Tom1" },           // High Tom

        // Hi-Hats (42, 44, 46)
        { 42, "ClosedHat" },      // Closed Hi-Hat
        { 44, "PedalHat" },       // Pedal Hi-Hat
        { 46, "OpenHat" },        // Open Hi-Hat

        // Cymbals (49, 51-59)
        { 49, "Crash" },          // Crash Cymbal 1
        { 51, "Ride" },           // Ride Cymbal 1
        { 52, "China" },          // Chinese Cymbal
        { 53, "RideBell" },       // Ride Bell
        { 54, "Tambourine" },     // Tambourine
        { 55, "Splash" },         // Splash Cymbal
        { 56, "Cowbell" },        // Cowbell
        { 57, "Crash" },          // Crash Cymbal 2 (grouped with Crash)
        { 59, "Ride" },           // Ride Cymbal 2 (grouped with Ride)

        // Percussion (60-81)
        { 60, "Bongo" },          // Hi Bongo
        { 61, "Bongo" },          // Low Bongo
        { 62, "Conga" },          // Mute Hi Conga
        { 63, "Conga" },          // Open Hi Conga
        { 64, "Conga" },          // Low Conga
        { 65, "Timbale" },        // High Timbale
        { 66, "Timbale" },        // Low Timbale
        { 67, "Agogo" },          // High Agogo
        { 68, "Agogo" },          // Low Agogo
        { 69, "Cabasa" },         // Cabasa
        { 70, "Maracas" },        // Maracas
        { 71, "Whistle" },        // Short Whistle
        { 72, "Whistle" },        // Long Whistle
        { 73, "Guiro" },          // Short Guiro
        { 74, "Guiro" },          // Long Guiro
        { 75, "Claves" },         // Claves
        { 76, "WoodBlock" },      // Hi Wood Block
        { 77, "WoodBlock" },      // Low Wood Block
        { 78, "Cuica" },          // Mute Cuica
        { 79, "Cuica" },          // Open Cuica
        { 80, "Triangle" },       // Mute Triangle
        { 81, "Triangle" }        // Open Triangle
    };

    // Primary drum kit roles (most common in standard drum patterns)
    private static readonly IReadOnlySet<string> PrimaryRoles = new HashSet<string>
    {
        "Kick", "Snare", "ClosedHat", "OpenHat", "Crash", "Ride", "Tom1", "Tom2", "FloorTom"
    };

    /// <summary>
    /// Maps a GM2 MIDI drum note to a normalized role name.
    /// Returns "Unknown:{midiNote}" for unmapped notes.
    /// Thread-safe, deterministic.
    /// </summary>
    /// <param name="midiNote">MIDI note number (typically 35-81 for drums).</param>
    /// <returns>Normalized role name or "Unknown:{midiNote}" if unmapped.</returns>
    public static string MapNoteToRole(int midiNote)
    {
        return MidiNoteToRole.TryGetValue(midiNote, out string? role)
            ? role
            : $"Unknown:{midiNote}";
    }

    /// <summary>
    /// Checks if a MIDI note maps to a primary drum kit role.
    /// Primary roles: Kick, Snare, ClosedHat, OpenHat, Crash, Ride, Tom1, Tom2, FloorTom.
    /// </summary>
    /// <param name="midiNote">MIDI note number.</param>
    /// <returns>True if the note maps to a primary drum kit role.</returns>
    public static bool IsPrimaryRole(int midiNote)
    {
        if (!MidiNoteToRole.TryGetValue(midiNote, out string? role))
            return false;

        return PrimaryRoles.Contains(role);
    }

    /// <summary>
    /// Gets all known MIDI notes for a given role.
    /// Useful for validating that articulation variants are grouped correctly.
    /// </summary>
    /// <param name="role">Role name (e.g., "Snare").</param>
    /// <returns>Collection of MIDI notes that map to this role.</returns>
    public static IReadOnlyList<int> GetNotesForRole(string role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return MidiNoteToRole
            .Where(kvp => kvp.Value == role)
            .Select(kvp => kvp.Key)
            .OrderBy(n => n)
            .ToList();
    }

    /// <summary>
    /// Gets all mapped roles.
    /// </summary>
    /// <returns>Collection of all role names that have mappings.</returns>
    public static IReadOnlySet<string> GetAllRoles()
    {
        return MidiNoteToRole.Values.ToHashSet();
    }

    /// <summary>
    /// Gets the set of primary drum kit roles.
    /// </summary>
    /// <returns>Set of primary role names.</returns>
    public static IReadOnlySet<string> GetPrimaryRoles() => PrimaryRoles;

    /// <summary>
    /// Checks if a MIDI note is in the standard GM2 drum range (35-81).
    /// </summary>
    /// <param name="midiNote">MIDI note number.</param>
    /// <returns>True if in GM2 drum range.</returns>
    public static bool IsInDrumRange(int midiNote)
    {
        return midiNote >= 35 && midiNote <= 81;
    }
}
