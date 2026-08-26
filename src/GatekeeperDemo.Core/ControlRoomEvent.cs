using AgentEval.PartnerDeskDemo.Demo;
using System.Diagnostics;

namespace GatekeeperDemo.Core;

/// <summary>An ordered, replayable projection of one semantic PartnerDesk runtime event.</summary>
public sealed record ControlRoomEvent(
    Guid RunId,
    long Sequence,
    TimeSpan Elapsed,
    DateTimeOffset TimestampUtc,
    PartnerDeskRuntimeEventKind Kind,
    string Source,
    string Target,
    string Title,
    string Detail,
    PartnerDeskRuntimeDisposition Disposition,
    string? PayloadPreview,
    int? Turn,
    string? OperationId);

/// <summary>
/// Thread-safe run-local event store. Runtime observation is append-only and event handlers are isolated from the
/// protected execution path.
/// </summary>
public sealed class ControlRoomEventStore : IPartnerDeskRuntimeEventSink
{
    private readonly object _sync = new();
    private readonly List<ControlRoomEvent> _events = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private long _sequence;

    public ControlRoomEventStore(Guid? runId = null) => RunId = runId ?? Guid.NewGuid();

    public Guid RunId { get; }

    public event EventHandler<ControlRoomEvent>? EventRecorded;

    public IReadOnlyList<ControlRoomEvent> Snapshot()
    {
        lock (_sync)
        {
            return _events.ToArray();
        }
    }

    public void Emit(PartnerDeskRuntimeEvent runtimeEvent)
    {
        ArgumentNullException.ThrowIfNull(runtimeEvent);
        var recorded = new ControlRoomEvent(
            RunId,
            Interlocked.Increment(ref _sequence),
            _clock.Elapsed,
            runtimeEvent.TimestampUtc,
            runtimeEvent.Kind,
            runtimeEvent.Source,
            runtimeEvent.Target,
            runtimeEvent.Title,
            runtimeEvent.Detail,
            runtimeEvent.Disposition,
            runtimeEvent.PayloadPreview,
            runtimeEvent.Turn,
            runtimeEvent.OperationId);

        lock (_sync)
        {
            _events.Add(recorded);
        }

        try
        {
            EventRecorded?.Invoke(this, recorded);
        }
        catch
        {
            // A projector is presentation only. It must never change the protected run.
        }
    }
}
