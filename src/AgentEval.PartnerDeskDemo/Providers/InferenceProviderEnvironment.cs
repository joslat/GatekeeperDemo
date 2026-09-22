// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AgentEval Contributors

namespace AgentEval.PartnerDeskDemo.Providers;

/// <summary>The inference hosts this demo can be pointed at.</summary>
/// <remarks>
/// Most of these speak the same OpenAI-compatible protocol at their own base URL, which is why adding a host is a
/// row in a switch rather than a branch at every call site. Azure OpenAI and an Azure AI Foundry resource are the
/// two that share the other protocol.
/// </remarks>
public enum InferenceProvider
{
    /// <summary>Nothing is configured; <see cref="InferenceProviderSettings.Diagnostic"/> says why.</summary>
    None,

    /// <summary>Azure OpenAI, the provider this demo shipped with.</summary>
    AzureOpenAI,

    /// <summary>Bitdeer AI Model Studio.</summary>
    Bitdeer,

    /// <summary>OpenAI itself.</summary>
    OpenAI,

    /// <summary>An Azure AI Foundry resource.</summary>
    Foundry,

    /// <summary>Any other OpenAI-compatible host: vLLM, Ollama, LM Studio, Together, Groq.</summary>
    OpenAICompatible,
}

/// <summary>How the provider in force was arrived at.</summary>
public enum InferenceProviderSelection
{
    /// <summary>No provider is configured.</summary>
    None,

    /// <summary>Named outright by <see cref="InferenceProviderEnvironment.SelectorVariable"/>.</summary>
    Explicit,

    /// <summary>Found by the auto-detect order because the selector was unset.</summary>
    AutoDetected,
}

/// <summary>
/// One resolved inference host: where to send a request, which credential to send, and which model to ask for.
/// </summary>
/// <remarks>
/// This type deliberately names no SDK type. The resolver that produces it reads only environment variables, so the
/// console demo, the evaluation harness and the Avalonia app share one answer to "which model are we talking to"
/// while the SDK dependency stays at the edge where the client is actually built.
/// </remarks>
public sealed record InferenceProviderSettings(
    InferenceProvider Provider,
    string ProviderTag,
    string DisplayName,
    Uri? Endpoint,
    string? ApiKey,
    string? Model,
    string? SecondaryModel,
    string? TertiaryModel,
    InferenceProviderSelection Selection,
    string? Diagnostic)
{
    /// <summary>A host with complete credentials was found.</summary>
    public bool IsConfigured => Provider != InferenceProvider.None;

    /// <summary>Azure OpenAI and a Foundry resource speak one protocol; everything else speaks the other.</summary>
    public bool UsesAzureProtocol => Provider is InferenceProvider.AzureOpenAI or InferenceProvider.Foundry;

    /// <summary>
    /// The identity a measurement should carry, as <c>model@provider</c>. The same model name on two hosts is not
    /// the same measurement, so recording the model alone would make a Bitdeer run and an Azure run
    /// indistinguishable in the artifact.
    /// </summary>
    public string ModelIdentity => $"{Model ?? "?"}@{ProviderTag}";

    /// <summary>The models offered for a comparison run: the primary first, then any distinct alternates.</summary>
    public IReadOnlyList<string> Models
    {
        get
        {
            List<string> models = [];
            foreach (var candidate in new[] { Model, SecondaryModel, TertiaryModel })
            {
                if (!string.IsNullOrWhiteSpace(candidate) && !models.Contains(candidate, StringComparer.Ordinal))
                {
                    models.Add(candidate);
                }
            }

            return models;
        }
    }

    /// <summary>
    /// A positional record generates a <c>ToString</c> that prints every property, and this object is exactly the
    /// kind that ends up in a line such as "resolved settings: {settings}". Redact the key by hand.
    /// </summary>
    public override string ToString() =>
        $"InferenceProviderSettings {{ Provider = {Provider}, ProviderTag = {ProviderTag}, " +
        $"Endpoint = {InferenceProviderEnvironment.SafeEndpoint(Endpoint)}, " +
        $"ApiKey = {(ApiKey is null ? "null" : "[redacted]")}, Model = {Model}, " +
        $"Selection = {Selection}, Diagnostic = {Diagnostic} }}";

    /// <summary>Nothing is configured, and here is the reason an operator can act on.</summary>
    public static InferenceProviderSettings NotConfigured(string diagnostic) =>
        new(InferenceProvider.None, "none", "No inference provider", null, null, null, null, null,
            InferenceProviderSelection.None, diagnostic);
}

/// <summary>
/// Turns the environment into an <see cref="InferenceProviderSettings"/>. This is the only place that knows the
/// name of a provider variable.
/// </summary>
public static class InferenceProviderEnvironment
{
    /// <summary>Names the host outright. Unset means auto-detect.</summary>
    public const string SelectorVariable = "AI_INFERENCE_PROVIDER";

    /// <summary>Bitdeer's OpenAI-compatible base URL, so <c>BITDEER_API_KEY</c> alone is enough.</summary>
    public const string BitdeerDefaultEndpoint = "https://api-inference.bitdeer.ai/v1";

    /// <summary>The Bitdeer model this demo is pointed at unless <c>BITDEER_MODEL</c> says otherwise.</summary>
    public const string BitdeerDefaultModel = "zai-org/GLM-5.3-Flash";

    /// <summary>OpenAI's base URL.</summary>
    public const string OpenAIDefaultEndpoint = "https://api.openai.com/v1";

    /// <summary>OpenAI's model when none is named.</summary>
    public const string OpenAIDefaultModel = "gpt-4o-mini";

    /// <summary>A keyless local host still needs a non-empty credential to construct the client.</summary>
    public const string KeylessSentinel = "no-key-needed";

    /// <summary>
    /// Azure first, so a machine that has only ever set the three <c>AZURE_OPENAI_*</c> variables behaves exactly as
    /// it did before this selector existed. That backward compatibility is a feature, and a test pins it.
    /// </summary>
    private static readonly InferenceProvider[] AutoDetectOrder =
    [
        InferenceProvider.AzureOpenAI,
        InferenceProvider.Bitdeer,
        InferenceProvider.OpenAI,
        InferenceProvider.Foundry,
        InferenceProvider.OpenAICompatible,
    ];

    /// <summary>The accepted values of <see cref="SelectorVariable"/>.</summary>
    public static IReadOnlyList<string> KnownValues { get; } = [.. AutoDetectOrder.Select(TagOf)];

    /// <summary>Every variable the resolver reads, required and optional alike, plus the selector.</summary>
    /// <remarks>
    /// <see cref="AnyConfigurationAttempted"/> walks this one list, so a newly added variable cannot quietly stop
    /// counting as configuration. A test asserts it stays in step with the per-provider switches.
    /// </remarks>
    public static IReadOnlyList<string> AllVariables { get; } =
    [
        SelectorVariable,
        .. AutoDetectOrder.SelectMany(provider => RequiredVariables(provider).Concat(OptionalVariables(provider))),
    ];

    /// <summary>Resolves the provider from the real process environment. Never cached; see the remarks.</summary>
    /// <remarks>
    /// Resolving per access is deliberate. A cached first resolution makes the process ignore a variable set later
    /// and makes every test after the first see stale settings, for the sake of a handful of environment reads.
    /// </remarks>
    public static InferenceProviderSettings Settings => Resolve(Environment.GetEnvironmentVariable);

    /// <summary>Resolves the provider from an arbitrary variable reader, which is what tests pass.</summary>
    public static InferenceProviderSettings Resolve(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        string? Env(string name)
        {
            var value = getEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        var selector = Env(SelectorVariable);
        if (selector is not null)
        {
            // Rule 1: explicit beats detected.
            var parsed = Parse(selector);
            if (parsed is null)
            {
                return InferenceProviderSettings.NotConfigured(
                    $"{SelectorVariable} is not one of {string.Join(" | ", KnownValues)}.");
            }

            // Rule 2: an explicit mistake fails closed. Falling back to another host would run against a model the
            // operator did not choose, which is worse than not running at all.
            if (!HasCredentials(parsed.Value, Env))
            {
                return InferenceProviderSettings.NotConfigured(
                    $"{SelectorVariable}={TagOf(parsed.Value)}, but it is missing: " +
                    $"{string.Join(", ", MissingVariables(parsed.Value, Env))}. " +
                    $"It needs {string.Join(", ", RequiredVariables(parsed.Value))}.");
            }

            return Describe(parsed.Value, Env, InferenceProviderSelection.Explicit);
        }

        // Rule 3: auto-detect in a fixed order. The first host with complete credentials wins.
        foreach (var candidate in AutoDetectOrder)
        {
            if (HasCredentials(candidate, Env))
            {
                return Describe(candidate, Env, InferenceProviderSelection.AutoDetected);
            }
        }

        // Rule 4: a half-configured provider is a typo, not an unchosen one. Listing every provider's full
        // requirements here would tell an operator that AZURE_OPENAI_ENDPOINT is missing when they have just set it.
        var partial = AutoDetectOrder.Where(provider => IsPartiallyConfigured(provider, Env)).ToList();
        if (partial.Count > 0)
        {
            return InferenceProviderSettings.NotConfigured(string.Join(" ", partial.Select(provider =>
                $"{DisplayNameOf(provider)} is partially configured - missing: " +
                $"{string.Join(", ", MissingVariables(provider, Env))}.")));
        }

        return InferenceProviderSettings.NotConfigured(
            $"{SelectorVariable} is not set and no provider has credentials. Set one of: " +
            string.Join("; ", AutoDetectOrder.Select(provider =>
                $"{TagOf(provider)} -> {string.Join(", ", RequiredVariables(provider))}")) + ".");
    }

    /// <summary>Was any provider variable, or the selector, set at all?</summary>
    /// <remarks>
    /// The gate on every "no credentials, so fall back to the offline path" branch. Something that was attempted and
    /// could not be built is a typo; falling through would undo the fail-closed contract from beneath, turning a
    /// misconfigured live run into scripted evidence presented as live.
    /// </remarks>
    public static bool AnyConfigurationAttempted(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        return AllVariables.Any(name => !string.IsNullOrWhiteSpace(getEnvironmentVariable(name)));
    }

    /// <summary>
    /// An endpoint that will carry an API key must be absolute https, or http only to loopback so a local server
    /// still works. Anything else is refused rather than sending a credential in cleartext.
    /// </summary>
    public static bool TryValidateEndpoint(string? value, out Uri? endpoint, out string? reason)
    {
        endpoint = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            reason = "is not set.";
            return false;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            reason = "is not an absolute http(s) URL.";
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
        {
            reason = "uses plain http to a non-loopback host; an API key would travel in cleartext. " +
                     "Use https, or a loopback address for a local server.";
            return false;
        }

        endpoint = uri;
        reason = null;
        return true;
    }

    /// <summary>Scheme, host and port only - never user-info, path, query or fragment.</summary>
    /// <remarks>
    /// A configured URL can carry a credential in all four of those parts, and every banner built from this reaches
    /// a console or a log that outlives the run. A guarantee that covers three of the four is not a guarantee.
    /// </remarks>
    public static string SafeEndpoint(Uri? endpoint)
    {
        if (endpoint is null)
        {
            return "(none)";
        }

        if (!endpoint.IsAbsoluteUri)
        {
            return "(relative)";
        }

        var port = endpoint.IsDefaultPort ? string.Empty : $":{endpoint.Port}";
        return $"{endpoint.Scheme}://{endpoint.Host}{port}";
    }

    /// <summary>The selector value for a provider, as an operator would type it.</summary>
    public static string TagOf(InferenceProvider provider) => provider switch
    {
        InferenceProvider.AzureOpenAI => "azure",
        InferenceProvider.Bitdeer => "bitdeer",
        InferenceProvider.OpenAI => "openai",
        InferenceProvider.Foundry => "foundry",
        InferenceProvider.OpenAICompatible => "openai-compatible",
        _ => "none",
    };

    /// <summary>The name a banner shows.</summary>
    public static string DisplayNameOf(InferenceProvider provider) => provider switch
    {
        InferenceProvider.AzureOpenAI => "Azure OpenAI",
        InferenceProvider.Bitdeer => "Bitdeer AI Model Studio",
        InferenceProvider.OpenAI => "OpenAI",
        InferenceProvider.Foundry => "Azure AI Foundry",
        InferenceProvider.OpenAICompatible => "OpenAI-compatible endpoint",
        _ => "No inference provider",
    };

    /// <summary>The variables a provider cannot do without.</summary>
    public static IReadOnlyList<string> RequiredVariables(InferenceProvider provider) => provider switch
    {
        InferenceProvider.AzureOpenAI =>
            ["AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_API_KEY", "AZURE_OPENAI_DEPLOYMENT"],
        // One variable. That is the point of giving the endpoint and the model defaults.
        InferenceProvider.Bitdeer => ["BITDEER_API_KEY"],
        InferenceProvider.OpenAI => ["OPENAI_API_KEY"],
        InferenceProvider.Foundry => ["FOUNDRY_ENDPOINT", "FOUNDRY_API_KEY", "FOUNDRY_MODEL"],
        InferenceProvider.OpenAICompatible => ["OPENAI_COMPATIBLE_ENDPOINT", "OPENAI_COMPATIBLE_MODEL"],
        _ => [],
    };

    /// <summary>
    /// The variables a provider will use if set. The <c>_2</c>/<c>_3</c> pairs carry a second and third model on the
    /// same host, which is what a comparison run needs.
    /// </summary>
    public static IReadOnlyList<string> OptionalVariables(InferenceProvider provider) => provider switch
    {
        InferenceProvider.AzureOpenAI => ["AZURE_OPENAI_DEPLOYMENT_2", "AZURE_OPENAI_DEPLOYMENT_3"],
        InferenceProvider.Bitdeer => ["BITDEER_ENDPOINT", "BITDEER_MODEL", "BITDEER_MODEL_2", "BITDEER_MODEL_3"],
        InferenceProvider.OpenAI => ["OPENAI_BASE_URL", "OPENAI_MODEL", "OPENAI_MODEL_2", "OPENAI_MODEL_3"],
        InferenceProvider.Foundry => ["FOUNDRY_MODEL_2", "FOUNDRY_MODEL_3"],
        InferenceProvider.OpenAICompatible =>
            ["OPENAI_COMPATIBLE_API_KEY", "OPENAI_COMPATIBLE_MODEL_2", "OPENAI_COMPATIBLE_MODEL_3"],
        _ => [],
    };

    /// <summary>Parses a selector value. An unknown value returns <see langword="null"/> and fails closed.</summary>
    public static InferenceProvider? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var tag = value.Trim();
        foreach (var candidate in AutoDetectOrder)
        {
            if (string.Equals(TagOf(candidate), tag, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool HasCredentials(InferenceProvider provider, Func<string, string?> env) =>
        RequiredVariables(provider).All(name => env(name) is not null);

    private static bool IsPartiallyConfigured(InferenceProvider provider, Func<string, string?> env)
    {
        var required = RequiredVariables(provider);
        var present = required.Count(name => env(name) is not null);
        return present > 0 && present < required.Count;
    }

    private static IReadOnlyList<string> MissingVariables(InferenceProvider provider, Func<string, string?> env) =>
        [.. RequiredVariables(provider).Where(name => env(name) is null)];

    private static InferenceProviderSettings Describe(
        InferenceProvider provider,
        Func<string, string?> env,
        InferenceProviderSelection selection)
    {
        var (endpointVariable, rawEndpoint) = provider switch
        {
            InferenceProvider.AzureOpenAI => ("AZURE_OPENAI_ENDPOINT", env("AZURE_OPENAI_ENDPOINT")),
            InferenceProvider.Bitdeer => ("BITDEER_ENDPOINT", env("BITDEER_ENDPOINT") ?? BitdeerDefaultEndpoint),
            InferenceProvider.OpenAI => ("OPENAI_BASE_URL", env("OPENAI_BASE_URL") ?? OpenAIDefaultEndpoint),
            InferenceProvider.Foundry => ("FOUNDRY_ENDPOINT", env("FOUNDRY_ENDPOINT")),
            _ => ("OPENAI_COMPATIBLE_ENDPOINT", env("OPENAI_COMPATIBLE_ENDPOINT")),
        };

        if (!TryValidateEndpoint(rawEndpoint, out var endpoint, out var reason))
        {
            // Name the variable and the reason, never the value: a configured URL can carry the key.
            return InferenceProviderSettings.NotConfigured($"{endpointVariable} {reason}");
        }

        var apiKey = provider switch
        {
            InferenceProvider.AzureOpenAI => env("AZURE_OPENAI_API_KEY"),
            InferenceProvider.Bitdeer => env("BITDEER_API_KEY"),
            InferenceProvider.OpenAI => env("OPENAI_API_KEY"),
            InferenceProvider.Foundry => env("FOUNDRY_API_KEY"),
            _ => env("OPENAI_COMPATIBLE_API_KEY") ?? KeylessSentinel,
        };

        var model = provider switch
        {
            InferenceProvider.AzureOpenAI => env("AZURE_OPENAI_DEPLOYMENT"),
            InferenceProvider.Bitdeer => env("BITDEER_MODEL") ?? BitdeerDefaultModel,
            InferenceProvider.OpenAI => env("OPENAI_MODEL") ?? OpenAIDefaultModel,
            InferenceProvider.Foundry => env("FOUNDRY_MODEL"),
            _ => env("OPENAI_COMPATIBLE_MODEL"),
        };

        // An alternate that was not named falls back to the primary, never to something the operator did not ask for.
        var (secondVariable, thirdVariable) = provider switch
        {
            InferenceProvider.AzureOpenAI => ("AZURE_OPENAI_DEPLOYMENT_2", "AZURE_OPENAI_DEPLOYMENT_3"),
            InferenceProvider.Bitdeer => ("BITDEER_MODEL_2", "BITDEER_MODEL_3"),
            InferenceProvider.OpenAI => ("OPENAI_MODEL_2", "OPENAI_MODEL_3"),
            InferenceProvider.Foundry => ("FOUNDRY_MODEL_2", "FOUNDRY_MODEL_3"),
            _ => ("OPENAI_COMPATIBLE_MODEL_2", "OPENAI_COMPATIBLE_MODEL_3"),
        };

        return new InferenceProviderSettings(
            provider,
            TagOf(provider),
            DisplayNameOf(provider),
            endpoint,
            apiKey,
            model,
            env(secondVariable) ?? model,
            env(thirdVariable) ?? model,
            selection,
            Diagnostic: null);
    }
}
