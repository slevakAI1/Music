// AI: purpose=Event args for measure-change notifications in SongGrid UI.
// AI: invariants=Properties set at construction and immutable; measure indices are integers; CurrentTick >= 0.
// AI: deps=Used by GridControlLinesManager and UI handlers; do not change ctor signature or property names.
namespace Music.Writer;

// AI: contract=Immutable EventArgs carrying previous/current measure and current absolute tick
internal sealed class MeasureChangedEventArgs : EventArgs
{
    public MeasureChangedEventArgs(int previousMeasure, int currentMeasure, long currentTick)
    {
        PreviousMeasure = previousMeasure;
        CurrentMeasure = currentMeasure;
        CurrentTick = currentTick;
    }

    public int PreviousMeasure { get; }
    public int CurrentMeasure { get; }

    public long CurrentTick { get; }
}
