using AgentEval.PartnerDeskDemo.Demo;
using AgentEval.PartnerDeskDemo.Providers;
using AgentEval.PartnerDeskDemo.Tools;
using Microsoft.Extensions.AI;

namespace GatekeeperDemo.Core;

public enum PartnerDeskModelMode
{
    Scripted,

    /// <summary>
    /// A real inference host. Which one is the resolver's business, not this type's.
    /// </summary>
    Live,
}

/// <summary>
/// Selects the model boundary for one run. Secrets are held only in this in-memory object and are never exposed
/// through display metadata, events, artifacts, or <see cref="ToString"/>.
/// </summary>
/// <remarks>
/// The live half is whichever host <see cref="InferenceProviderEnvironment"/> resolves — Azure OpenAI, Bitdeer,
/// OpenAI, a Foundry resource or any OpenAI-compatible endpoint. Nothing here branches on which.
/// </remarks>
public sealed class PartnerDeskModelConfiguration
{
    private readonly IReadOnlyList<string>? _explicitErrors;

    private PartnerDeskModelConfiguration(
        PartnerDeskModelMode mode,
        InferenceProviderSettings settings,
        string? model,
        IReadOnlyList<string>? explicitErrors)
    {
        Mode = mode;
        Settings = settings;
        Deployment = model?.Trim();
        _explicitErrors = explicitErrors;
    }

    public static PartnerDeskModelConfiguration Scripted { get; } =
        new(PartnerDeskModelMode.Scripted,
            InferenceProviderSettings.NotConfigured("The scripted offline provider needs no credentials."),
            null,
            null);

    /// <summary>The live host as the environment describes it, with an optional model override.</summary>
    public static PartnerDeskModelConfiguration FromEnvironment(string? model = null)
    {
        var settings = InferenceProviderEnvironment.Settings;
        return new PartnerDeskModelConfiguration(
            PartnerDeskModelMode.Live, settings, model ?? settings.Model, null);
    }

    /// <summary>The live host from already-resolved settings, with an optional model override.</summary>
    public static PartnerDeskModelConfiguration Live(InferenceProviderSettings settings, string? model = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new PartnerDeskModelConfiguration(
            PartnerDeskModelMode.Live, settings, model ?? settings.Model, null);
    }

    /// <summary>
    /// Azure OpenAI from values supplied outright rather than read from the environment.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from the environment-variable convention, the way an explicit endpoint/key pair always
    /// is: it is how a caller points one run at a resource it already holds, and how tests exercise the live shape
    /// without touching process-wide state.
    /// </remarks>
    public static PartnerDeskModelConfiguration AzureOpenAI(
        string? endpoint,
        string? apiKey,
        string? deployment)
    {
        List<string> errors = [];
        Uri? endpointUri = null;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            errors.Add("Set AZURE_OPENAI_ENDPOINT before starting the app.");
        }
        else if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out endpointUri)
                 || endpointUri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add("AZURE_OPENAI_ENDPOINT must be an absolute HTTPS URL.");
            endpointUri = null;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            errors.Add("Set AZURE_OPENAI_API_KEY before starting the app.");
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            errors.Add("Enter an Azure OpenAI deployment/model name.");
        }

        var model = deployment?.Trim();
        var settings = errors.Count == 0
            ? new InferenceProviderSettings(
                InferenceProvider.AzureOpenAI,
                InferenceProviderEnvironment.TagOf(InferenceProvider.AzureOpenAI),
                InferenceProviderEnvironment.DisplayNameOf(InferenceProvider.AzureOpenAI),
                endpointUri,
                apiKey,
                model,
                model,
                model,
                InferenceProviderSelection.Explicit,
                Diagnostic: null)
            : InferenceProviderSettings.NotConfigured(string.Join(" ", errors));

        return new PartnerDeskModelConfiguration(
            PartnerDeskModelMode.Live, settings, model, errors.Count == 0 ? null : errors);
    }

    public PartnerDeskModelMode Mode { get; }

    /// <summary>The resolved host. Carries the key, so it is never rendered except through its redacting members.</summary>
    public InferenceProviderSettings Settings { get; }

    /// <summary>The model this run will ask for. Named for the Azure deployment it used to be.</summary>
    public string? Deployment { get; }

    public bool IsDeterministic => Mode == PartnerDeskModelMode.Scripted;

    public string DisplayName => Mode switch
    {
        PartnerDeskModelMode.Scripted => "Scripted offline provider",
        PartnerDeskModelMode.Live when Settings.IsConfigured =>
            $"{Settings.DisplayName} · {Deployment ?? "model not set"}",
        PartnerDeskModelMode.Live => "Live provider · not configured",
        _ => Mode.ToString(),
    };

    public ModelExecutionDescriptor Descriptor => new(
        Mode,
        Mode == PartnerDeskModelMode.Live ? Settings.DisplayName : "Scripted offline provider",
        Deployment,
        // model@provider: the same model name on two hosts is not the same measurement, and this is the field a
        // replayed artifact is read back through.
        Mode == PartnerDeskModelMode.Live
            ? (Settings with { Model = Deployment }).ModelIdentity
            : "scripted@none",
        IsDeterministic);

    public IReadOnlyList<string> Validate()
    {
        if (Mode == PartnerDeskModelMode.Scripted)
        {
            return [];
        }

        if (_explicitErrors is { Count: > 0 })
        {
            return _explicitErrors;
        }

        return Settings.IsConfigured
            ? []
            : [Settings.Diagnostic ?? "No inference provider is configured."];
    }

    public string AudienceDisclosure()
    {
        if (Mode == PartnerDeskModelMode.Scripted)
        {
            return "Scripted offline model selected. Decisions are fixed and repeatable.";
        }

        if (!Settings.IsConfigured)
        {
            return "No inference provider is configured. " + (Settings.Diagnostic ?? string.Empty);
        }

        // SafeEndpoint, not the configured URL: a configured URL can carry the key in four different parts.
        return $"Live {Settings.DisplayName} model '{Deployment ?? "not set"}' at " +
               $"{InferenceProviderEnvironment.SafeEndpoint(Settings.Endpoint)}. " +
               "Outputs and attack compliance are nondeterministic.";
    }

    internal IChatClient CreateChatClient(PhaseRunContext context, PartnerRegister register)
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }

        if (Mode == PartnerDeskModelMode.Scripted)
        {
            return ScriptedPartnerDeskModel.Create(context, register);
        }

        var (client, _, diagnostic) =
            ProviderChatClientFactory.TryCreate(Settings, Deployment, generousTimeout: true);
        return client ?? throw new InvalidOperationException(
            diagnostic ?? "No inference provider is configured.");
    }

    public override string ToString() => DisplayName;
}

/// <summary>Secret-free model provenance retained with a replay artifact.</summary>
public sealed record ModelExecutionDescriptor(
    PartnerDeskModelMode Mode,
    string Provider,
    string? Deployment,
    string ModelIdentity,
    bool Deterministic);
