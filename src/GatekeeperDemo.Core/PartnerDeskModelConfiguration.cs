using AgentEval.PartnerDeskDemo.Demo;
using AgentEval.PartnerDeskDemo.Tools;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace GatekeeperDemo.Core;

public enum PartnerDeskModelMode
{
    Scripted,
    AzureOpenAI,
}

/// <summary>
/// Selects the model boundary for one run. Secrets are held only in this in-memory object and are never exposed
/// through display metadata, events, artifacts, or <see cref="ToString"/>.
/// </summary>
public sealed class PartnerDeskModelConfiguration
{
    private readonly string? _endpoint;
    private readonly string? _apiKey;

    private PartnerDeskModelConfiguration(
        PartnerDeskModelMode mode,
        string? endpoint,
        string? apiKey,
        string? deployment)
    {
        Mode = mode;
        _endpoint = endpoint;
        _apiKey = apiKey;
        Deployment = deployment?.Trim();
    }

    public static PartnerDeskModelConfiguration Scripted { get; } =
        new(PartnerDeskModelMode.Scripted, null, null, null);

    public static PartnerDeskModelConfiguration AzureOpenAI(
        string? endpoint,
        string? apiKey,
        string? deployment) =>
        new(PartnerDeskModelMode.AzureOpenAI, endpoint?.Trim(), apiKey, deployment);

    public PartnerDeskModelMode Mode { get; }

    public string? Deployment { get; }

    public bool IsDeterministic => Mode == PartnerDeskModelMode.Scripted;

    public string DisplayName => Mode switch
    {
        PartnerDeskModelMode.Scripted => "Scripted offline provider",
        PartnerDeskModelMode.AzureOpenAI => $"Azure OpenAI · {Deployment ?? "deployment not set"}",
        _ => Mode.ToString(),
    };

    public ModelExecutionDescriptor Descriptor => new(
        Mode,
        Mode == PartnerDeskModelMode.AzureOpenAI ? "Azure OpenAI" : "Scripted offline provider",
        Deployment,
        IsDeterministic);

    public IReadOnlyList<string> Validate()
    {
        if (Mode == PartnerDeskModelMode.Scripted)
        {
            return [];
        }

        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(_endpoint))
        {
            errors.Add("Set AZURE_OPENAI_ENDPOINT before starting the app.");
        }
        else if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var endpointUri)
                 || endpointUri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add("AZURE_OPENAI_ENDPOINT must be an absolute HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            errors.Add("Set AZURE_OPENAI_API_KEY before starting the app.");
        }

        if (string.IsNullOrWhiteSpace(Deployment))
        {
            errors.Add("Enter an Azure OpenAI deployment/model name.");
        }

        return errors;
    }

    public string AudienceDisclosure()
    {
        if (Mode == PartnerDeskModelMode.Scripted)
        {
            return "Scripted offline model selected. Decisions are fixed and repeatable.";
        }

        var host = Uri.TryCreate(_endpoint, UriKind.Absolute, out var endpoint) ? endpoint.Host : "endpoint not ready";
        return $"Live Azure OpenAI deployment '{Deployment ?? "not set"}' at {host}. " +
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

        var client = new AzureOpenAIClient(new Uri(_endpoint!), new AzureKeyCredential(_apiKey!));
        return client.GetChatClient(Deployment!).AsIChatClient();
    }

    public override string ToString() => DisplayName;
}

/// <summary>Secret-free model provenance retained with a replay artifact.</summary>
public sealed record ModelExecutionDescriptor(
    PartnerDeskModelMode Mode,
    string Provider,
    string? Deployment,
    bool Deterministic);
