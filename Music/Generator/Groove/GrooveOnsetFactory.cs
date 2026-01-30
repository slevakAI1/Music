// AI: purpose=Factory for creating GrooveOnset from anchors; variation handled by DrumGrooveOnsetFactory.
// AI: invariants=Anchor onsets get Source=Anchor provenance; variation onsets created by instrument-specific factories.
// AI: deps=GrooveOnset, GrooveOnsetProvenance.
// AI: change=FromVariation removed (GC-3); variation creation delegated to instrument-specific factories (Drums namespace).

namespace Music.Generator.Groove;

// AI: purpose=Factory for creating GrooveOnset from anchors; variation handled by DrumGrooveOnsetFactory.
public static class GrooveOnsetFactory
{
    // AI: purpose=Creates GrooveOnset from anchor with Source=Anchor provenance.
    public static GrooveOnset FromAnchor(
        string role,
        int barNumber,
        decimal beat,
        bool isMustHit = false,
        bool isNeverRemove = false,
        bool isProtected = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return new GrooveOnset
        {
            Role = role,
            BarNumber = barNumber,
            Beat = beat,
            Provenance = GrooveOnsetProvenance.ForAnchor(),
            IsMustHit = isMustHit,
            IsNeverRemove = isNeverRemove,
            IsProtected = isProtected
        };
    }

    // AI: purpose=Creates copy with updated properties; preserves provenance via 'with' expression.
    public static GrooveOnset WithUpdatedProperties(
        GrooveOnset onset,
        OnsetStrength? strength = null,
        int? velocity = null,
        int? timingOffsetTicks = null)
    {
        ArgumentNullException.ThrowIfNull(onset);

        return onset with
        {
            Strength = strength ?? onset.Strength,
            Velocity = velocity ?? onset.Velocity,
            TimingOffsetTicks = timingOffsetTicks ?? onset.TimingOffsetTicks
        };
    }
}
