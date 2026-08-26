using AgentEval.PartnerDeskDemo.Demo;
using Avalonia.Media;
using GatekeeperDemo.Core;

namespace GatekeeperDemo.App.ViewModels;

public sealed class EventItemViewModel : BindableBase
{
    private bool _isExpanded;

    public EventItemViewModel(ControlRoomEvent runtimeEvent)
    {
        Event = runtimeEvent;
        ToggleExpandedCommand = new RelayCommand(ToggleExpanded, () => HasExpandableContent);
        (Badge, Icon, Accent) = runtimeEvent.Disposition switch
        {
            PartnerDeskRuntimeDisposition.Risky => ("RISK", "!", Brush.Parse("#FF6577")),
            PartnerDeskRuntimeDisposition.Untrusted => ("UNTRUSTED", "!", Brush.Parse("#FF7A66")),
            PartnerDeskRuntimeDisposition.Blocked => ("BLOCKED", "▣", Brush.Parse("#FFB454")),
            PartnerDeskRuntimeDisposition.Withheld => ("WITHHELD", "◇", Brush.Parse("#C99CFF")),
            PartnerDeskRuntimeDisposition.Failed => ("ERROR", "×", Brush.Parse("#FF5364")),
            PartnerDeskRuntimeDisposition.Safe => ("SAFE", "✓", Brush.Parse("#44D891")),
            PartnerDeskRuntimeDisposition.SimulatedEffect => ("SIMULATED", "↳", Brush.Parse("#5CC8FF")),
            _ => (NeutralBadge(runtimeEvent.Kind), "•", Brush.Parse("#6FA8FF")),
        };
    }

    public ControlRoomEvent Event { get; }

    public string Badge { get; }

    public string Icon { get; }

    public IBrush Accent { get; }

    public RelayCommand ToggleExpandedCommand { get; }

    public string Sequence => $"{Event.Sequence:000}";

    public string Time => $"+{Event.Elapsed.TotalSeconds:0.000}s";

    public string Title => Event.Title;

    public string Detail => Event.Detail;

    public string Route => $"{DisplayActor(Event.Source)}  →  {DisplayActor(Event.Target)}";

    public string Payload => string.IsNullOrWhiteSpace(Event.PayloadPreview)
        ? "No payload preview was retained for this event."
        : Event.PayloadPreview;

    public bool HasExpandableContent => !string.IsNullOrWhiteSpace(Event.PayloadPreview);

    public bool IsExpanded
    {
        get => _isExpanded;
        private set
        {
            if (!SetProperty(ref _isExpanded, value)) return;
            RaisePropertyChanged(nameof(ExpandControlText));
            RaisePropertyChanged(nameof(IsCollapsed));
            RaisePropertyChanged(nameof(IsExpandControlVisible));
        }
    }

    public bool IsCollapsed => !IsExpanded;

    public bool IsExpandControlVisible => HasExpandableContent && IsCollapsed;

    public string ExpandControlText => $"{(IsExpanded ? "▼" : "▶")}  {PayloadLabel}";

    public string PayloadLabel => Event.Kind switch
    {
        PartnerDeskRuntimeEventKind.UserMessageSubmitted => "SENT MESSAGE",
        PartnerDeskRuntimeEventKind.AgentAnswerProduced => "RECEIVED MESSAGE",
        PartnerDeskRuntimeEventKind.ModelRequestStarted => "MODEL INPUT",
        PartnerDeskRuntimeEventKind.ModelResponseReceived => "MODEL OUTPUT",
        PartnerDeskRuntimeEventKind.ToolProposed => "TOOL CALL",
        PartnerDeskRuntimeEventKind.ToolCompleted => "TOOL RESPONSE",
        _ => "PAYLOAD",
    };

    private void ToggleExpanded()
    {
        if (HasExpandableContent)
        {
            IsExpanded = !IsExpanded;
        }
    }

    private static string DisplayActor(string actor) => actor switch
    {
        "user" => "User",
        "agent" => "PartnerDesk agent",
        "model" => "Model",
        "mcp" => "PartnerIntel MCP",
        "gatekeeper" => "Gatekeeper",
        "query_partner_database" => "Database tool",
        "send_email" => "Email tool",
        "control-room" => "Control room",
        _ => actor,
    };

    private static string NeutralBadge(PartnerDeskRuntimeEventKind kind) => kind switch
    {
        PartnerDeskRuntimeEventKind.RunConfigured or PartnerDeskRuntimeEventKind.RunStarted
            or PartnerDeskRuntimeEventKind.RunCompleted => "RUN",
        PartnerDeskRuntimeEventKind.UserMessageSubmitted or PartnerDeskRuntimeEventKind.AgentAnswerProduced =>
            "MESSAGE",
        PartnerDeskRuntimeEventKind.AgentWorking => "AGENT",
        PartnerDeskRuntimeEventKind.ModelProviderSelected or PartnerDeskRuntimeEventKind.ModelRequestStarted
            or PartnerDeskRuntimeEventKind.ModelResponseReceived =>
            "MODEL",
        PartnerDeskRuntimeEventKind.McpSessionStarting or PartnerDeskRuntimeEventKind.McpSessionReady => "MCP",
        PartnerDeskRuntimeEventKind.ToolProposed or PartnerDeskRuntimeEventKind.ToolExecutionStarted
            or PartnerDeskRuntimeEventKind.ToolCompleted => "TOOL",
        PartnerDeskRuntimeEventKind.RetryStarted => "RETRY",
        _ => "EVENT",
    };
}
