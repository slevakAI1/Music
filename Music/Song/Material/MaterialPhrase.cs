// AI: purpose=Phrase data for placement; agnostic to instrument; events use phrase-relative ticks.
// AI: invariants=BarCount>=1; SectionTypes may be empty but must be stable for placement decisions.
// AI: deps=MaterialBank storage and Generator placement; ToPartTrack requires instrument program number.

using Music.Generator;
using Music.MyMidi;

namespace Music.Song.Material;

public sealed record MaterialPhrase
{
    public required int PhraseNumber { get; init; }
    public required string PhraseId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int BarCount { get; init; }
    public required int MidiProgramNumber { get; init; }
    public required int Seed { get; init; }
    public required IReadOnlyList<PartTrackEvent> Events { get; init; }
    public IReadOnlyList<MusicConstants.eSectionType> SectionTypes { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public double EnergyHint { get; init; } = 0.5;

    public static MaterialPhrase FromPartTrack(
        PartTrack partTrack,
        int phraseNumber,
        string phraseId,
        string name,
        string description,
        int barCount,
        int seed,
        IReadOnlyList<MusicConstants.eSectionType>? sectionTypes = null,
        IReadOnlyList<string>? tags = null,
        double energyHint = 0.5)
    {
        ArgumentNullException.ThrowIfNull(partTrack);
        if (phraseNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(phraseNumber), "PhraseNumber must be >= 1");
        if (string.IsNullOrWhiteSpace(phraseId))
            throw new ArgumentException("PhraseId must be provided", nameof(phraseId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be provided", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description must be provided", nameof(description));
        if (barCount < 1)
            throw new ArgumentOutOfRangeException(nameof(barCount), "BarCount must be >= 1");
        return new MaterialPhrase
        {
            PhraseNumber = phraseNumber,
            PhraseId = phraseId,
            Name = name,
            Description = description,
            BarCount = barCount,
            MidiProgramNumber = partTrack.MidiProgramNumber,
            Seed = seed,
            Events = partTrack.PartTrackNoteEvents.ToList(),
            SectionTypes = sectionTypes ?? [],
            Tags = tags ?? [],
            EnergyHint = energyHint
        };
    }

    public PartTrack ToPartTrack(BarTrack barTrack, int startBar, int midiProgramNumber)
    {
        ArgumentNullException.ThrowIfNull(barTrack);
        if (startBar < 1)
            throw new ArgumentOutOfRangeException(nameof(startBar), "StartBar must be >= 1");
        if (!barTrack.TryGetBar(1, out var bar1))
            throw new ArgumentException("BarTrack must include bar 1", nameof(barTrack));
        if (!barTrack.TryGetBar(startBar, out var targetBar))
            throw new ArgumentOutOfRangeException(nameof(startBar), "StartBar not found in BarTrack");

        long tickOffset = targetBar.StartTick - bar1.StartTick;

        var offsetEvents = Events
            .Select(e => new PartTrackEvent
            {
                AbsoluteTimeTicks = e.AbsoluteTimeTicks + tickOffset,
                Type = e.Type,
                NoteNumber = e.NoteNumber,
                NoteDurationTicks = e.NoteDurationTicks,
                NoteOnVelocity = e.NoteOnVelocity
            })
            .OrderBy(e => e.AbsoluteTimeTicks)
            .ToList();

        return new PartTrack(offsetEvents)
        {
            MidiProgramName = $"{Name} @bar{startBar}",
            MidiProgramNumber = midiProgramNumber
        };
    }
}
