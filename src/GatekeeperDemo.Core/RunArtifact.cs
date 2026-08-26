using AgentEval.PartnerDeskDemo.Demo;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GatekeeperDemo.Core;

/// <summary>A versioned, bounded and integrity-checkable run artifact for zero-operation replay.</summary>
public sealed record RunArtifact(
    int SchemaVersion,
    Guid RunId,
    DateTimeOffset CreatedUtc,
    PartnerDeskRunConfiguration Configuration,
    string Question,
    string Answer,
    RunEvidence Evidence,
    IReadOnlyList<ControlRoomEvent> Events,
    string IntegritySha256)
{
    public const int CurrentSchemaVersion = 1;

    public static RunArtifact Create(
        PartnerDeskRunConfiguration configuration,
        string question,
        PhaseOutcome outcome,
        RunEvidence evidence,
        IReadOnlyList<ControlRoomEvent> events)
    {
        // Raw console narration can contain hostile third-party text and exception stacks. It stays available in
        // the live Debug tab but is not persisted by default. Replay retains semantic story/security facts only.
        var replayEvents = events
            .Where(item => item.Kind != PartnerDeskRuntimeEventKind.Diagnostic)
            .Select(item => item with
            {
                Detail = Clip(item.Detail, 500),
                PayloadPreview = item.PayloadPreview is null ? null : Clip(item.PayloadPreview, 500),
            })
            .ToArray();
        var draft = new RunArtifact(
            CurrentSchemaVersion,
            events.FirstOrDefault()?.RunId ?? Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            configuration,
            Clip(question),
            Clip(outcome.RetryAfterContainment?.AnswerText ?? outcome.AnswerText),
            evidence,
            replayEvents,
            string.Empty);
        return draft with { IntegritySha256 = ComputeIntegrity(draft) };
    }

    public bool VerifyIntegrity()
    {
        if (SchemaVersion != CurrentSchemaVersion || IntegritySha256.Length != 64)
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(IntegritySha256),
                Convert.FromHexString(ComputeIntegrity(this with { IntegritySha256 = string.Empty })));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    public static RunArtifact FromJson(string json)
    {
        var artifact = JsonSerializer.Deserialize<RunArtifact>(json, SerializerOptions)
            ?? throw new InvalidDataException("The run artifact was empty.");
        if (!artifact.VerifyIntegrity())
        {
            throw new InvalidDataException("The run artifact failed its integrity check.");
        }

        return artifact;
    }

    private static string ComputeIntegrity(RunArtifact artifact)
    {
        var canonical = JsonSerializer.Serialize(artifact with { IntegritySha256 = string.Empty }, SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Clip(string value, int width = 2_000) =>
        value.Length <= width ? value : value[..(width - 1)] + "…";

    private static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}

/// <summary>Pure presentation replay; advancing it never starts an agent, MCP process, or tool.</summary>
public sealed class RunReplay
{
    private int _index;

    public RunReplay(RunArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        Artifact = artifact;
        // A completed live run leaves the inspector on the latest event. Starting replay at the same point makes
        // Previous/Next spatially honest instead of making the first Next click jump backwards to event two.
        _index = artifact.Events.Count - 1;
    }

    public RunArtifact Artifact { get; }

    public int Index => _index;

    public ControlRoomEvent? Current =>
        _index >= 0 && _index < Artifact.Events.Count ? Artifact.Events[_index] : null;

    public bool MoveNext()
    {
        if (_index >= Artifact.Events.Count - 1)
        {
            return false;
        }

        _index++;
        return true;
    }

    public bool MovePrevious()
    {
        if (_index <= 0)
        {
            return false;
        }

        _index--;
        return true;
    }

    public bool MoveFirst()
    {
        if (Artifact.Events.Count == 0)
        {
            return false;
        }

        _index = 0;
        return true;
    }
}
