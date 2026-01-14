// AI: purpose=Conversion helpers between MotifSpec and PartTrack for MaterialBank storage; enables round-trip serialization
// AI: invariants=ToPartTrack creates MaterialLocal domain events; FromPartTrack validates domain and reconstructs spec
// AI: deps=Uses PartTrack.Meta, PartTrackEvent for storage; pitch placeholders use MIDI 60 (middle C) as default
// AI: change=Stage 9 renderer replaces placeholder pitches with actual harmony-aware notes
using Music.Generator;
using Music.MyMidi;

namespace Music.Song.Material;

public static class MotifConversion
{
    // AI: purpose=Convert MotifSpec to PartTrack for storage in MaterialBank
    // AI: invariants=Creates MaterialLocal domain events; pitch placeholders = MIDI 60; ticks from RhythmShape
    public static PartTrack ToPartTrack(this MotifSpec spec)
    {
        var events = new List<PartTrackEvent>();
        
        // AI: Create note events for each rhythm onset with placeholder pitch (MIDI 60 = middle C)
        // AI: Duration = 240 ticks (half of quarter note at 480 PPQN) as safe default
        const int placeholderPitch = 60;
        const int defaultDuration = 240;
        
        foreach (var tick in spec.RhythmShape)
        {
            events.Add(new PartTrackEvent(
                noteNumber: placeholderPitch,
                absoluteTimeTicks: tick,
                noteDurationTicks: defaultDuration,
                noteOnVelocity: 100));
        }

        return new PartTrack(events)
        {
            Meta = new PartTrackMeta
            {
                TrackId = spec.MotifId,
                Name = spec.Name,
                IntendedRole = spec.IntendedRole,
                Domain = PartTrackDomain.MaterialLocal,
                Kind = PartTrackKind.MaterialFragment,
                MaterialKind = spec.Kind,
                Tags = spec.Tags
            },
            MidiProgramName = spec.IntendedRole,
            MidiProgramNumber = 0
        };
    }

    // AI: purpose=Reconstruct MotifSpec from PartTrack for round-trip validation
    // AI: invariants=Validates MaterialLocal domain; extracts rhythm from event ticks
    // AI: errors=Returns null if validation fails (domain mismatch or invalid materialKind)
    public static MotifSpec? FromPartTrack(PartTrack track)
    {
        // AI: Validate domain and kind for motif tracks
        if (track.Meta.Domain != PartTrackDomain.MaterialLocal)
            return null;

        if (track.Meta.Kind != PartTrackKind.MaterialFragment)
            return null;

        // AI: Extract rhythm shape from note event ticks
        var rhythmShape = track.PartTrackNoteEvents
            .Select(e => (int)e.AbsoluteTimeTicks)
            .ToList();

        // AI: Reconstruct spec with defaults for fields not stored in PartTrack
        // AI: Note: Contour, Register, TonePolicy not stored in PartTrack; use safe defaults
        return new MotifSpec(
            MotifId: track.Meta.TrackId,
            Name: track.Meta.Name ?? "Unnamed",
            IntendedRole: track.Meta.IntendedRole ?? "Lead",
            Kind: track.Meta.MaterialKind,
            RhythmShape: rhythmShape,
            Contour: ContourIntent.Flat, // Default
            Register: RegisterIntent.Create(60, 12), // Default: middle C ± octave
            TonePolicy: TonePolicy.Create(0.7, true), // Default: 70% chord tone bias, passing tones allowed
            Tags: track.Meta.Tags ?? new HashSet<string>());
    }
}
