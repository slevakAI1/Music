// AI: purpose=Target register and range constraints for motif pitch realization
// AI: invariants=CenterMidiNote [21..108] valid MIDI range; RangeSemitones [0..24] max 2 octaves
// AI: change=When rendering motif pitches, respect both center and range; clamp to instrument limits
namespace Music.Song.Material;

public readonly record struct RegisterIntent(
    int CenterMidiNote,
    int RangeSemitones)
{
    public static RegisterIntent Create(int centerMidiNote, int rangeSemitones) =>
        new(
            Math.Clamp(centerMidiNote, 21, 108),
            Math.Clamp(rangeSemitones, 0, 24));
}
