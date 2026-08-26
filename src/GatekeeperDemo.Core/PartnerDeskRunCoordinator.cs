using AgentEval.PartnerDeskDemo.Demo;
using AgentEval.PartnerDeskDemo.Tools;
using System.Diagnostics;

namespace GatekeeperDemo.Core;

/// <summary>Serializes real runs and keeps Avalonia concerns outside the authoritative demo runner.</summary>
public sealed class PartnerDeskRunCoordinator : IAsyncDisposable
{
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private PartnerDeskRunner? _activeRunner;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    public async Task<ControlRoomRunResult> RunAsync(
        PartnerDeskRunConfiguration configuration,
        string question,
        ControlRoomEventStore events,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        await _runLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        IsRunning = true;
        var clock = Stopwatch.StartNew();
        try
        {
            var register = PartnerRegister.Load();
            var runDirectory = Path.Combine(Path.GetTempPath(), "GatekeeperDemo", events.RunId.ToString("N"));
            Directory.CreateDirectory(runDirectory);
            var output = DemoOutput.Create(new EventingTextWriter(events));
            _activeRunner = new PartnerDeskRunner(
                context => ScriptedPartnerDeskModel.Create(context, register),
                output,
                Path.Combine(runDirectory, "fake-outbox.jsonl"),
                register,
                events: events);

            var outcome = await _activeRunner.RunAsync(configuration, question, cancellationToken)
                .ConfigureAwait(false);
            var evidence = RunEvidence.From(outcome);
            var artifact = RunArtifact.Create(configuration, question, outcome, evidence, events.Snapshot());
            return new(configuration, question, outcome, evidence, artifact, clock.Elapsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            events.Emit(new(
                PartnerDeskRuntimeEventKind.Diagnostic,
                "runtime",
                "debug",
                "Run failed",
                exception.ToString(),
                PartnerDeskRuntimeDisposition.Failed));
            throw;
        }
        finally
        {
            if (_activeRunner is not null)
            {
                await _activeRunner.DisposeAsync().ConfigureAwait(false);
                _activeRunner = null;
            }

            IsRunning = false;
            _runLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        // Wait until RunAsync's finally block has disposed the active MCP child process and released the run lock.
        // This prevents window close from racing a second disposal against a still-running agent call.
        await _runLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_activeRunner is not null)
            {
                await _activeRunner.DisposeAsync().ConfigureAwait(false);
                _activeRunner = null;
            }

            _disposed = true;
        }
        finally
        {
            _runLock.Release();
            _runLock.Dispose();
        }
    }
}
