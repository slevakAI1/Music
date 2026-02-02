// AI: purpose=Applies bounded evolution to drum phrases for purposeful variation in phrase placement.
// AI: invariants=Never mutate original phrase/events; deterministic from (seed, phraseId, evolution) inputs.
// AI: deps=MaterialPhrase events are phrase-relative ticks; uses DrumPhraseEvolutionParams and BarTrack bounds.

using Music.Generator;
using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator.Agents.Drums;

public sealed class DrumPhraseEvolver
{
    private const int KickNote = 36;
    private const int SnareNote = 38;
    private const int ClosedHatNote = 42;
    private const int OpenHatNote = 46;
    private const int GhostOffsetTicks = 60;
    private const int GhostDurationTicks = 30;

    private readonly int _seed;

    public DrumPhraseEvolver(int seed)
    {
        _seed = seed;
    }

    // AI: purpose=Applies evolution operators in order; returns original when evolution is null or all zeros.
    public MaterialPhrase Evolve(MaterialPhrase original, DrumPhraseEvolutionParams? evolution, BarTrack barTrack)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(barTrack);

        if (evolution is null || IsNoEvolution(evolution))
            return original;

        List<PartTrackEvent> events = original.Events.Select(e => CloneEvent(e)).ToList();
        var rng = new Random(_seed ^ original.PhraseId.GetHashCode());

        if (evolution.Simplification > 0)
            events = ApplySimplification(events, evolution.Simplification, rng);

        if (evolution.GhostIntensity > 0)
            events = AddGhostNotes(events, evolution.GhostIntensity, barTrack, rng);

        if (evolution.HatVariation > 0)
            events = ApplyHatVariation(events, evolution.HatVariation, rng);

        if (evolution.RandomVariation > 0)
            events = ApplyRandomVariation(events, evolution.RandomVariation, rng);

        events = events.OrderBy(e => e.AbsoluteTimeTicks).ToList();

        return original with
        {
            PhraseId = $"{original.PhraseId}_ev{_seed % 1000}",
            Name = $"{original.Name} (evolved)",
            Events = events
        };
    }

    private static bool IsNoEvolution(DrumPhraseEvolutionParams evolution)
        => evolution.GhostIntensity == 0
           && evolution.HatVariation == 0
           && evolution.Simplification == 0
           && evolution.RandomVariation == 0;

    private static List<PartTrackEvent> ApplySimplification(
        List<PartTrackEvent> events,
        double intensity,
        Random rng)
    {
        var simplified = events
            .Where(e => IsEssentialHit(e) || rng.NextDouble() > intensity * 0.5)
            .ToList();

        if (intensity > 0 && simplified.Count == events.Count)
        {
            var removable = simplified.Where(e => !IsEssentialHit(e)).ToList();
            if (removable.Count > 0)
            {
                var toRemove = removable[rng.Next(removable.Count)];
                simplified.Remove(toRemove);
            }
        }

        return simplified;
    }

    private static List<PartTrackEvent> AddGhostNotes(
        List<PartTrackEvent> events,
        double intensity,
        BarTrack barTrack,
        Random rng)
    {
        if (!barTrack.TryGetBar(1, out var bar1))
            throw new ArgumentException("BarTrack must include bar 1", nameof(barTrack));

        var result = new List<PartTrackEvent>(events.Select(e => CloneEvent(e)));
        var snareHits = result
            .Where(e => e.NoteNumber == SnareNote && e.NoteOnVelocity > 80)
            .OrderBy(e => e.AbsoluteTimeTicks)
            .ToList();

        int addedCount = 0;
        foreach (var snare in snareHits)
        {
            if (rng.NextDouble() >= intensity * 0.3)
                continue;

            long ghostTick = snare.AbsoluteTimeTicks - GhostOffsetTicks;
            if (ghostTick <= bar1.StartTick)
                continue;

            if (result.Any(e => e.NoteNumber == SnareNote && e.AbsoluteTimeTicks == ghostTick))
                continue;

            result.Add(new PartTrackEvent
            {
                AbsoluteTimeTicks = ghostTick,
                Type = PartTrackEventType.NoteOn,
                NoteNumber = SnareNote,
                NoteDurationTicks = GhostDurationTicks,
                NoteOnVelocity = 40 + rng.Next(20)
            });
            addedCount++;
        }

        if (intensity > 0 && addedCount == 0 && snareHits.Count > 0)
        {
            var snare = snareHits[0];
            long ghostTick = snare.AbsoluteTimeTicks - GhostOffsetTicks;
            if (ghostTick > bar1.StartTick)
            {
                result.Add(new PartTrackEvent
                {
                    AbsoluteTimeTicks = ghostTick,
                    Type = PartTrackEventType.NoteOn,
                    NoteNumber = SnareNote,
                    NoteDurationTicks = GhostDurationTicks,
                    NoteOnVelocity = 50
                });
            }
        }

        return result;
    }

    private static List<PartTrackEvent> ApplyHatVariation(
        List<PartTrackEvent> events,
        double intensity,
        Random rng)
    {
        var updated = new List<PartTrackEvent>(events.Count);
        bool hatChanged = false;

        foreach (var e in events)
        {
            if (e.NoteNumber == ClosedHatNote && rng.NextDouble() < intensity * 0.2)
            {
                updated.Add(CloneEvent(e, noteNumber: OpenHatNote));
                hatChanged = true;
            }
            else
            {
                updated.Add(CloneEvent(e));
            }
        }

        if (intensity > 0 && !hatChanged)
        {
            var index = updated.FindIndex(e => e.NoteNumber == ClosedHatNote);
            if (index >= 0)
            {
                updated[index] = CloneEvent(updated[index], noteNumber: OpenHatNote);
            }
        }

        return updated;
    }

    private static List<PartTrackEvent> ApplyRandomVariation(
        List<PartTrackEvent> events,
        double intensity,
        Random rng)
    {
        var varied = new List<PartTrackEvent>(events.Count);
        bool changed = false;

        foreach (var e in events)
        {
            int velocityDelta = (int)Math.Round(intensity * 15 * (rng.NextDouble() - 0.5));
            int newVelocity = Math.Clamp(e.NoteOnVelocity + velocityDelta, 1, 127);
            long timingDelta = 0;

            if (!IsEssentialHit(e))
                timingDelta = (long)Math.Round(intensity * 10 * (rng.NextDouble() - 0.5));

            long newTick = Math.Max(0, e.AbsoluteTimeTicks + timingDelta);

            if (newVelocity != e.NoteOnVelocity || newTick != e.AbsoluteTimeTicks)
                changed = true;

            varied.Add(CloneEvent(e, absoluteTicks: newTick, noteOnVelocity: newVelocity));
        }

        if (intensity > 0 && !changed && varied.Count > 0)
        {
            var targetIndex = varied.FindIndex(e => !IsEssentialHit(e));
            if (targetIndex < 0)
                targetIndex = 0;

            var target = varied[targetIndex];
            int boosted = Math.Clamp(target.NoteOnVelocity + 1, 1, 127);
            varied[targetIndex] = CloneEvent(target, noteOnVelocity: boosted);
        }

        return varied;
    }

    private static bool IsEssentialHit(PartTrackEvent e)
        => e.NoteNumber == KickNote || (e.NoteNumber == SnareNote && e.NoteOnVelocity > 80);

    private static PartTrackEvent CloneEvent(
        PartTrackEvent source,
        long? absoluteTicks = null,
        int? noteNumber = null,
        int? noteDurationTicks = null,
        int? noteOnVelocity = null)
    {
        return new PartTrackEvent
        {
            AbsoluteTimeTicks = absoluteTicks ?? source.AbsoluteTimeTicks,
            Type = source.Type,
            NoteNumber = noteNumber ?? source.NoteNumber,
            NoteDurationTicks = noteDurationTicks ?? source.NoteDurationTicks,
            NoteOnVelocity = noteOnVelocity ?? source.NoteOnVelocity,
            Parameters = source.Parameters.Count == 0
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(source.Parameters)
        };
    }
}
