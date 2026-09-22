// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AgentEval Contributors

using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AgentEval.PartnerDeskDemo.Providers;

/// <summary>
/// The single place an <see cref="IChatClient"/> is constructed from resolved provider settings.
/// </summary>
/// <remarks>
/// Two protocol families, one entry point. Azure OpenAI and an Azure AI Foundry resource take the Azure client;
/// Bitdeer, OpenAI and every other OpenAI-compatible host take the OpenAI client pointed at a different base URL.
/// Adding Together, Groq or a local vLLM is a new enum value and a row in the resolver's switches, not a change
/// here.
/// </remarks>
public static class ProviderChatClientFactory
{
    /// <summary>Per-attempt network timeout for a slow subject under test.</summary>
    /// <remarks>
    /// The SDK default of 100 seconds can fire on a real agent behind an MCP boundary and abort a whole run, so the
    /// demo path asks for a generous one.
    /// </remarks>
    public const string NetworkTimeoutVariable = "GATEKEEPER_NETWORK_TIMEOUT_S";

    /// <summary>The timeout used when none is configured.</summary>
    public static readonly TimeSpan DefaultNetworkTimeout = TimeSpan.FromSeconds(180);

    /// <summary>Resolves the environment and builds a client for the provider in force.</summary>
    /// <param name="model">Overrides the provider's primary model, for a comparison run.</param>
    /// <param name="generousTimeout">Apply <see cref="NetworkTimeout"/> instead of the SDK default.</param>
    /// <returns>
    /// The client and the model it will ask for, or a diagnostic saying why neither exists. The caller keeps
    /// control of its own stderr and exit code, so nothing here writes to the console.
    /// </returns>
    public static (IChatClient? Client, string? Model, string? Diagnostic) TryCreate(
        string? model = null,
        bool generousTimeout = false) =>
        TryCreate(InferenceProviderEnvironment.Settings, model, generousTimeout);

    /// <summary>Builds a client from already-resolved settings.</summary>
    public static (IChatClient? Client, string? Model, string? Diagnostic) TryCreate(
        InferenceProviderSettings settings,
        string? model = null,
        bool generousTimeout = false)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsConfigured)
        {
            return (null, null, settings.Diagnostic ?? "No inference provider is configured.");
        }

        var resolved = string.IsNullOrWhiteSpace(model) ? settings.Model : model.Trim();
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return (null, null, $"{settings.DisplayName} is selected but names no model.");
        }

        try
        {
            var timeout = generousTimeout ? NetworkTimeout() : (TimeSpan?)null;
            IChatClient client;
            if (settings.UsesAzureProtocol)
            {
                var options = new AzureOpenAIClientOptions();
                if (timeout is { } azureTimeout)
                {
                    options.NetworkTimeout = azureTimeout;
                }

                client = new AzureOpenAIClient(settings.Endpoint!, new AzureKeyCredential(settings.ApiKey!), options)
                    .GetChatClient(resolved)
                    .AsIChatClient();
            }
            else
            {
                var options = new OpenAIClientOptions { Endpoint = settings.Endpoint };
                if (timeout is { } openAITimeout)
                {
                    options.NetworkTimeout = openAITimeout;
                }

                client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey!), options)
                    .GetChatClient(resolved)
                    .AsIChatClient();
            }

            return (client, resolved, null);
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException or InvalidOperationException
                                       or NotSupportedException or FormatException)
        {
            // The SDK's own message can quote the configured URI, which may carry a credential, and this diagnostic
            // goes to stderr where CI keeps it. Name the exception type and the sanitised endpoint only.
            return (null, null,
                $"Failed to construct the {settings.DisplayName} chat client ({ex.GetType().Name}) for " +
                $"endpoint={InferenceProviderEnvironment.SafeEndpoint(settings.Endpoint)}. " +
                "Check the key and the endpoint.");
        }
    }

    /// <summary>The per-attempt network timeout, from the environment or the default.</summary>
    public static TimeSpan NetworkTimeout() => NetworkTimeout(Environment.GetEnvironmentVariable);

    /// <summary>The per-attempt network timeout, from an arbitrary variable reader.</summary>
    public static TimeSpan NetworkTimeout(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        var raw = getEnvironmentVariable(NetworkTimeoutVariable);
        return int.TryParse(raw, out var seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : DefaultNetworkTimeout;
    }

    /// <summary>
    /// The one-line provenance a banner prints: who is answering, which model, and at which host.
    /// </summary>
    /// <remarks>Built from <see cref="InferenceProviderEnvironment.SafeEndpoint"/>, so it carries no credential.</remarks>
    public static string Banner(InferenceProviderSettings settings, string? model = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsConfigured)
        {
            return settings.Diagnostic ?? "No inference provider is configured.";
        }

        var resolved = string.IsNullOrWhiteSpace(model) ? settings.Model : model.Trim();
        var how = settings.Selection == InferenceProviderSelection.Explicit
            ? $"{InferenceProviderEnvironment.SelectorVariable}={settings.ProviderTag}"
            : "auto-detected";
        return $"{settings.DisplayName} · model '{resolved}' · " +
               $"{InferenceProviderEnvironment.SafeEndpoint(settings.Endpoint)} · {how}";
    }
}
