// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AgentEval Contributors

using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace AgentEval.PartnerDeskDemo.Demo;

/// <summary>
/// Sits directly under the agent and records every tool call the model asks for, before any Gatekeeper middleware
/// sees it.
/// </summary>
/// <remarks>
/// This is what makes "the agent still tried" provable rather than rhetorical. A pre-execution block returns a
/// refusal to the model and leaves no trace in the tool ledger; the attempt only exists in the model's own output,
/// which is exactly what this client captures.
/// </remarks>
public sealed class RecordingChatClient : DelegatingChatClient
{
    private const int MaximumPayloadCharacters = 6000;
    private static readonly JsonSerializerOptions PayloadJsonOptions = new() { WriteIndented = true };
    private readonly ToolCallJournal _journal;
    private readonly IPartnerDeskRuntimeEventSink _events;

    /// <summary>Wraps <paramref name="inner"/>, recording proposals into <paramref name="journal"/>.</summary>
    public RecordingChatClient(
        IChatClient inner,
        ToolCallJournal journal,
        IPartnerDeskRuntimeEventSink? events = null)
        : base(inner)
    {
        _journal = journal;
        _events = events ?? NullPartnerDeskRuntimeEventSink.Instance;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var requestMessages = messages.ToArray();
        EmitModelRequest(requestMessages, options);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await base.GetResponseAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
        EmitModelResponse(response.Messages, stopwatch.Elapsed);
        Record(response.Messages);
        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestMessages = messages.ToArray();
        EmitModelRequest(requestMessages, options);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in base.GetStreamingResponseAsync(requestMessages, options, cancellationToken)
            .ConfigureAwait(false))
        {
            updates.Add(update);
            yield return update;
        }

        var response = updates.ToChatResponse();
        EmitModelResponse(response.Messages, stopwatch.Elapsed);
        Record(response.Messages);
    }

    private void EmitModelRequest(IReadOnlyList<ChatMessage> messages, ChatOptions? options) =>
        _events.EmitSafely(new(
            PartnerDeskRuntimeEventKind.ModelRequestStarted,
            "agent",
            "model",
            "Model request started",
            "The agent sent the current conversation and available tool contracts to the selected model.",
            PayloadPreview: RenderRequest(messages, options)));

    private void EmitModelResponse(IEnumerable<ChatMessage> messages, TimeSpan elapsed) =>
        _events.EmitSafely(new(
            PartnerDeskRuntimeEventKind.ModelResponseReceived,
            "model",
            "agent",
            "Model response received",
            $"The selected model returned its next action or final answer after {elapsed.TotalSeconds:0.000}s.",
            PayloadPreview: RenderMessages(messages)));

    private static string RenderRequest(IReadOnlyList<ChatMessage> messages, ChatOptions? options)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(options?.Instructions))
        {
            builder.AppendLine("[SYSTEM INSTRUCTIONS]");
            builder.AppendLine(options.Instructions.Trim());
            builder.AppendLine();
        }

        if (options?.Tools is { Count: > 0 } tools)
        {
            builder.AppendLine("[AVAILABLE TOOLS]");
            foreach (var tool in tools)
            {
                builder.Append("- ").AppendLine(tool.Name);
            }
            builder.AppendLine();
        }

        builder.AppendLine("[CONVERSATION SENT TO MODEL]");
        builder.Append(RenderMessages(messages));
        return Bound(builder.ToString());
    }

    private static string RenderMessages(IEnumerable<ChatMessage> messages)
    {
        var builder = new StringBuilder();
        foreach (var message in messages)
        {
            builder.Append('[').Append(message.Role.ToString().ToUpperInvariant()).AppendLine("]");
            var visibleContent = false;
            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case TextContent text when !string.IsNullOrWhiteSpace(text.Text):
                        builder.AppendLine(text.Text.Trim());
                        visibleContent = true;
                        break;
                    case FunctionCallContent call:
                        builder.Append("TOOL CALL: ").Append(call.Name).AppendLine();
                        builder.AppendLine(RenderValue(call.Arguments));
                        visibleContent = true;
                        break;
                    case FunctionResultContent result:
                        builder.AppendLine("TOOL RESULT:");
                        builder.AppendLine(RenderValue(result.Result));
                        visibleContent = true;
                        break;
                }
            }

            if (!visibleContent)
            {
                builder.AppendLine("[No displayable text or tool content]");
            }
            builder.AppendLine();
        }

        return Bound(builder.ToString());
    }

    private static string RenderValue(object? value)
    {
        if (value is null) return "null";
        if (value is string text) return text;
        try
        {
            return JsonSerializer.Serialize(value, PayloadJsonOptions);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return value.ToString() ?? $"[{value.GetType().Name}]";
        }
    }

    private static string Bound(string value) => value.Length <= MaximumPayloadCharacters
        ? value.TrimEnd()
        : value[..MaximumPayloadCharacters].TrimEnd() + "\n… [bounded preview truncated]";

    private void Record(IEnumerable<ChatMessage> produced)
    {
        var calls = produced
            .SelectMany(message => message.Contents.OfType<FunctionCallContent>())
            .ToArray();
        if (calls.Length == 0)
        {
            return;
        }

        var turn = _journal.BeginTurn();
        foreach (var call in calls)
        {
            _journal.RecordProposal(
                turn,
                call.Name,
                call.Arguments as IReadOnlyDictionary<string, object?>
                    ?? call.Arguments?.ToDictionary(pair => pair.Key, pair => pair.Value)
                    ?? new Dictionary<string, object?>(StringComparer.Ordinal));
        }
    }
}
