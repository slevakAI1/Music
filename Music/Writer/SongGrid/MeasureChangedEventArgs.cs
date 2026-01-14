using Music.Generator;

namespace Music.Writer;

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
