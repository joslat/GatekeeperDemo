// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AgentEval Contributors

namespace AgentEval.PartnerDeskDemo.Demo;

/// <summary>Semantic runtime facts emitted by the PartnerDesk scenario for observers such as the Avalonia UI.</summary>
public enum PartnerDeskRuntimeEventKind
{
    RunConfigured,
    RunStarted,
    UserMessageSubmitted,
    McpSessionStarting,
    McpSessionReady,
    AgentWorking,
    ModelRequestStarted,
    ModelResponseReceived,
    ToolProposed,
    ToolExecutionStarted,
    ToolCompleted,
    GateFindingRecorded,
    ResultWithheld,
    ContainmentActivated,
    RetryStarted,
    AgentAnswerProduced,
    RunCompleted,
    Diagnostic,
}

/// <summary>Security meaning of an event, independent from whether the operation completed.</summary>
public enum PartnerDeskRuntimeDisposition
{
    Neutral,
    Untrusted,
    Safe,
    Risky,
    Blocked,
    Withheld,
    SimulatedEffect,
    Failed,
}

/// <summary>
/// A bounded, presentation-neutral runtime fact. The event deliberately carries semantic actor IDs instead of
/// Avalonia node IDs; presentation maps those actors onto a particular graph.
/// </summary>
public sealed record PartnerDeskRuntimeEvent(
    PartnerDeskRuntimeEventKind Kind,
    string Source,
    string Target,
    string Title,
    string Detail,
    PartnerDeskRuntimeDisposition Disposition = PartnerDeskRuntimeDisposition.Neutral,
    string? PayloadPreview = null,
    int? Turn = null,
    string? OperationId = null,
    DateTimeOffset? OccurredAtUtc = null)
{
    /// <summary>The event timestamp, defaulting to UTC now when the producer did not supply one.</summary>
    public DateTimeOffset TimestampUtc { get; } = OccurredAtUtc ?? DateTimeOffset.UtcNow;
}

/// <summary>Receives semantic runtime facts. Implementations must not throw back into the protected run.</summary>
public interface IPartnerDeskRuntimeEventSink
{
    void Emit(PartnerDeskRuntimeEvent runtimeEvent);
}

/// <summary>No-op sink used by the console and tests when no observer is attached.</summary>
public sealed class NullPartnerDeskRuntimeEventSink : IPartnerDeskRuntimeEventSink
{
    private NullPartnerDeskRuntimeEventSink() { }

    public static NullPartnerDeskRuntimeEventSink Instance { get; } = new();

    public void Emit(PartnerDeskRuntimeEvent runtimeEvent) { }
}

internal static class PartnerDeskRuntimeEventSinkExtensions
{
    public static void EmitSafely(this IPartnerDeskRuntimeEventSink sink, PartnerDeskRuntimeEvent runtimeEvent)
    {
        try
        {
            sink.Emit(runtimeEvent);
        }
        catch
        {
            // Observation is explicitly non-authoritative and may never alter Gatekeeper or tool behaviour.
        }
    }
}
