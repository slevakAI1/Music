// AI: purpose=Immutable motif specification; defines WHAT a motif is (rhythm/contour/register/tones), not WHERE/WHEN placed
// AI: invariants=All fields immutable; MotifId unique per instance; RhythmShape ticks >=0; numeric fields clamped
// AI: deps=MaterialKind, PartTrackId from Story M1; converted to PartTrack via Story 8.2 helpers
// AI: change=Stage 9 placement logic consumes these; never mutate stored motifs (derive variants via provenance)
using Music.Generator;

namespace Music.Song.Material;

public record MotifSpec(
    PartTrack.PartTrackId MotifId,
    string Name,
    string IntendedRole,
    MaterialKind Kind,
    IReadOnlyList<int> RhythmShape,
    ContourIntent Contour,
    RegisterIntent Register,
    TonePolicy TonePolicy,
    IReadOnlySet<string> Tags)
{
    // AI: purpose=Factory with validation and defaults; clamps all numeric fields to safe ranges
    // AI: errors=Never throws; clamps invalid inputs to valid ranges; generates unique MotifId
    public static MotifSpec Create(
        string name,
        string intendedRole,
        MaterialKind kind,
        IReadOnlyList<int> rhythmShape,
        ContourIntent contour,
        int centerMidiNote,
        int rangeSemitones,
        double chordToneBias,
        bool allowPassingTones,
        IReadOnlySet<string>? tags = null)
    {
        var clampedRhythm = rhythmShape.Select(t => Math.Max(0, t)).ToList();

        return new MotifSpec(
            MotifId: PartTrack.PartTrackId.NewId(),
            Name: name,
            IntendedRole: intendedRole,
            Kind: kind,
            RhythmShape: clampedRhythm,
            Contour: contour,
            Register: RegisterIntent.Create(centerMidiNote, rangeSemitones),
            TonePolicy: TonePolicy.Create(chordToneBias, allowPassingTones),
            Tags: tags ?? new HashSet<string>());
    }
}
