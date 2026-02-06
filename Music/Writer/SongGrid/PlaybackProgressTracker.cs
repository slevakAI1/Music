using Music.Generator;
using Music.MyMidi;

// AI: purpose=Track MIDI playback progress and raise measure change events to UI subscribers.
// AI: invariants=MeasureChanged fired only when measure increments; CurrentTick>=0; handler invoked on sync context if present.
// AI: deps=MidiPlaybackService for ticks, Timingtrack for measure mapping; poll interval default 50ms.
// AI: threading=RunAsync uses PeriodicTimer; cancellation token honors cooperative shutdown; not reentrant.
namespace Music.Writer;

internal sealed class PlaybackProgressTracker : IDisposable
{
    private readonly MidiPlaybackService _playbackService;
    private readonly Timingtrack _timeSignatureTrack;
    private readonly int _pollIntervalMs;
    private readonly SynchronizationContext? _syncContext;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private int _currentMeasure;

    public PlaybackProgressTracker(
        MidiPlaybackService playbackService,
        Timingtrack timeSignatureTrack,
        int pollIntervalMs = 50)
    {
        ArgumentNullException.ThrowIfNull(playbackService);
        ArgumentNullException.ThrowIfNull(timeSignatureTrack);

        _playbackService = playbackService;
        _timeSignatureTrack = timeSignatureTrack;
        _pollIntervalMs = pollIntervalMs <= 0 ? 50 : pollIntervalMs;
        _syncContext = SynchronizationContext.Current;
    }

    public event EventHandler<MeasureChangedEventArgs>? MeasureChanged;

    public void Start()
    {
        if (_cts != null)
            return;

        _currentMeasure = 0;
        _cts = new CancellationTokenSource();
        _loopTask = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        var cts = _cts;
        if (cts == null)
            return;

        _cts = null;
        try
        {
            cts.Cancel();
        }
        finally
        {
            cts.Dispose();
        }

        _loopTask = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_pollIntervalMs));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var isPlaying = _playbackService.IsPlaying;

                if (!isPlaying)
                    continue;

                var tick = _playbackService.CurrentTick;
                var measure = TickToMeasure(tick);

                if (measure <= 0 || measure == _currentMeasure)
                    continue;

                var previous = _currentMeasure;
                _currentMeasure = measure;

                RaiseMeasureChanged(previous, measure, tick);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected during shutdown; no diagnostics emitted.
        }
    }

    private void RaiseMeasureChanged(int previousMeasure, int currentMeasure, long currentTick)
    {
        var handler = MeasureChanged;
        if (handler == null)
            return;

        var args = new MeasureChangedEventArgs(previousMeasure, currentMeasure, currentTick);

        if (_syncContext != null)
        {
            _syncContext.Post(_ => handler(this, args), null);
        }
        else
        {
            handler(this, args);
        }
    }

    private int TickToMeasure(long tick)
    {
        if (tick < 0)
            return 0;

        if (_timeSignatureTrack.Events.Count == 0)
            return 0;

        long currentTick = 0;
        int bar = 1;

        for (; bar <= 10000; bar++)
        {
            var ts = _timeSignatureTrack.GetActiveTimeSignatureEvent(bar);
            if (ts == null)
                return 0;

            int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * ts.Numerator) / ts.Denominator;
            long nextTick = currentTick + ticksPerMeasure;

            if (tick < nextTick)
                return bar;

            currentTick = nextTick;
        }

        return 0;
    }

    public void Dispose()
    {
        Stop();
    }
}
