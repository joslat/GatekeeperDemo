using AgentEval.PartnerDeskDemo.Demo;
using AgentEval.PartnerDeskDemo.Providers;
using AgentEval.PartnerDeskDemo.Tools;
using GatekeeperDemo.App.ViewModels;
using GatekeeperDemo.Core;
using Xunit;

namespace GatekeeperDemo.App.Tests;

public sealed class ViewModelTests
{
    [Fact]
    public async Task DefaultScene_IsVisiblySelectedAndRunnable()
    {
        await using var viewModel = new MainWindowViewModel(null, null, null);

        Assert.True(viewModel.IsCleanPresetSelected);
        Assert.False(viewModel.IsCompromisedPresetSelected);
        Assert.True(viewModel.IsConfigurationValid);
        Assert.True(viewModel.RunCommand.CanExecute(null));
        Assert.Contains("Demo 1 selected", viewModel.ConfigurationNotice, StringComparison.Ordinal);
        Assert.Equal(0, viewModel.SelectedModelIndex);
        Assert.Contains("SCRIPTED", viewModel.ModelModeBadge, StringComparison.Ordinal);
        Assert.Contains("no credentials", viewModel.ModelReadinessText, StringComparison.Ordinal);
        Assert.Null(viewModel.SelectedModel);
        Assert.True(viewModel.IsSetupExpanded);
        Assert.Contains("Demo 1", viewModel.SetupSelectionSummary, StringComparison.Ordinal);
        Assert.Contains("Scripted", viewModel.SetupSelectionSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetupPanel_CanCollapseWithoutChangingTheSelectedRun()
    {
        await using var viewModel = new MainWindowViewModel(null, null, null);
        var summary = viewModel.SetupSelectionSummary;

        viewModel.IsSetupExpanded = false;

        Assert.False(viewModel.IsSetupExpanded);
        Assert.Equal(summary, viewModel.SetupSelectionSummary);
        Assert.Contains("EDIT MODEL", viewModel.SetupPanelAction, StringComparison.Ordinal);
        Assert.True(viewModel.RunCommand.CanExecute(null));
    }

    [Fact]
    public async Task LiveModelWithoutEnvironment_IsExplainedAndCannotRun()
    {
        await using var viewModel = new MainWindowViewModel(null, null, null)
        {
            SelectedModelIndex = 1,
        };

        Assert.True(viewModel.IsLiveModelSelected);
        Assert.False(viewModel.IsConfigurationValid);
        Assert.False(viewModel.RunCommand.CanExecute(null));
        Assert.Contains("AZURE_OPENAI_ENDPOINT", viewModel.ModelReadinessText, StringComparison.Ordinal);
        Assert.Contains("Model blocked", viewModel.ConfigurationNotice, StringComparison.Ordinal);
        Assert.True(viewModel.IsCleanPresetSelected);
    }

    [Fact]
    public async Task ReadyLiveModel_ShowsDeploymentAndNondeterministicDisclosure()
    {
        await using var viewModel = new MainWindowViewModel(
            "https://example.openai.azure.com/",
            "not-a-real-secret",
            "gpt-5.5");

        Assert.Equal(1, viewModel.SelectedModelIndex);
        Assert.True(viewModel.IsConfigurationValid);
        Assert.True(viewModel.RunCommand.CanExecute(null));
        Assert.Contains("gpt-5.5", viewModel.ModelNodeTitle, StringComparison.Ordinal);
        Assert.Contains("NONDETERMINISTIC", viewModel.ModelModeBadge, StringComparison.Ordinal);
        Assert.Contains("output is nondeterministic", viewModel.ModelDisclosure, StringComparison.Ordinal);
        Assert.True(viewModel.IsCleanPresetSelected);
    }

    [Fact]
    public async Task SingleModelDropdown_UpdatesTheExactExecutionModeAndDeployment()
    {
        await using var viewModel = new MainWindowViewModel(
            "https://example.openai.azure.com/",
            "not-a-real-secret",
            null);

        Assert.Equal(0, viewModel.SelectedModelIndex);
        viewModel.SelectedModelIndex = 2;

        Assert.Equal("gpt-5-mini", viewModel.SelectedModel);
        Assert.Contains("measured 5/5", viewModel.ModelSelectionEvidence, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gpt-5-mini", viewModel.ModelReadinessText, StringComparison.Ordinal);
        Assert.True(viewModel.IsConfigurationValid);
    }

    [Fact]
    public async Task ReferenceEnvironment_PreselectsItsHardcodedAzureDeployment()
    {
        await using var viewModel = new MainWindowViewModel(
            "https://example.openai.azure.com/",
            "not-a-real-secret",
            "gpt-5-chat");

        Assert.Equal(3, viewModel.SelectedModelIndex);
        Assert.Equal("gpt-5-chat", viewModel.SelectedModel);
        Assert.True(viewModel.IsLiveModelSelected);
    }

    [Fact]
    public async Task BitdeerEnvironment_OffersItsModelAndPreselectsIt()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "bitdeer"),
            ("BITDEER_API_KEY", "not-a-real-secret")));

        await using var viewModel = new MainWindowViewModel(settings);

        Assert.Equal(1, viewModel.SelectedModelIndex);
        Assert.Equal(InferenceProviderEnvironment.BitdeerDefaultModel, viewModel.SelectedModel);
        Assert.True(viewModel.IsLiveModelSelected);
        Assert.True(viewModel.IsConfigurationValid);
        Assert.Contains("Bitdeer", viewModel.ModelNodeTitle, StringComparison.Ordinal);
        // A Bitdeer model reports its own spot check rather than borrowing the Azure deployments' published rates.
        Assert.Contains("SPOT CHECK 0/1", viewModel.ModelSelectionEvidence, StringComparison.Ordinal);
        Assert.DoesNotContain("5/5", viewModel.ModelSelectionEvidence, StringComparison.Ordinal);
        Assert.DoesNotContain("not-a-real-secret", viewModel.ModelReadinessText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StaleAzureVariables_DoNotPutADeadDeploymentOnTheDropdown()
    {
        // The reported bug: a shell that never saw AI_INFERENCE_PROVIDER still had the retired AZURE_OPENAI_*
        // variables, so the app opened on a deployment whose endpoint no longer resolves.
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AZURE_OPENAI_ENDPOINT", "https://retired.openai.azure.com/"),
            ("AZURE_OPENAI_API_KEY", "not-a-real-secret"),
            ("AZURE_OPENAI_DEPLOYMENT", "gpt-5.5"),
            ("BITDEER_API_KEY", "not-a-real-secret")));

        await using var viewModel = new MainWindowViewModel(settings);

        Assert.Contains("Bitdeer", viewModel.ModelNodeTitle, StringComparison.Ordinal);
        Assert.Equal(InferenceProviderEnvironment.BitdeerDefaultModel, viewModel.SelectedModel);
        Assert.DoesNotContain(
            viewModel.ModelOptions, option => option.Contains("gpt-5.5", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BitdeerDropdown_OffersTheAccountsChatModels()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "bitdeer"),
            ("BITDEER_API_KEY", "not-a-real-secret")));

        await using var viewModel = new MainWindowViewModel(settings);

        // Scripted, then the six chat models the account serves. The embedding, reranker and image models it also
        // lists cannot answer a chat request and are deliberately absent.
        Assert.Equal(7, viewModel.ModelOptions.Count);
        foreach (var model in new[]
                 {
                     "zai-org/GLM-5.3-Flash", "deepseek-ai/DeepSeek-V4-Flash", "deepseek-ai/DeepSeek-V4.1-Flash",
                     "Qwen/Qwen3.8-27B", "moonshotai/Kimi-K3", "zai-org/GLM-5.3",
                 })
        {
            Assert.Contains(viewModel.ModelOptions, option => option.Contains(model, StringComparison.Ordinal));
        }

        Assert.DoesNotContain(viewModel.ModelOptions, option => option.Contains("bge-", StringComparison.Ordinal));
        Assert.DoesNotContain(viewModel.ModelOptions, option => option.Contains("seedream", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BitdeerWithAlternates_PutsEveryModelOnTheDropdown()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "bitdeer"),
            ("BITDEER_API_KEY", "not-a-real-secret"),
            ("BITDEER_MODEL_2", "moonshotai/Kimi-K2.5")));

        await using var viewModel = new MainWindowViewModel(settings);

        // The environment's own models lead — primary then BITDEER_MODEL_2 — and the account catalogue follows.
        Assert.Contains("Scripted", viewModel.ModelOptions[0], StringComparison.Ordinal);
        Assert.Contains(
            InferenceProviderEnvironment.BitdeerDefaultModel, viewModel.ModelOptions[1], StringComparison.Ordinal);
        Assert.Contains("moonshotai/Kimi-K2.5", viewModel.ModelOptions[2], StringComparison.Ordinal);

        viewModel.SelectedModelIndex = 2;
        Assert.Equal("moonshotai/Kimi-K2.5", viewModel.SelectedModel);

        // A model named by the environment is listed once, not again by the catalogue.
        Assert.Single(
            viewModel.ModelOptions,
            option => option.Contains(InferenceProviderEnvironment.BitdeerDefaultModel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task MisconfiguredProvider_BlocksTheRunInsteadOfFallingBackToScripted()
    {
        // The selector names a host whose key is absent. Dropping to the scripted model here would present
        // fixed decisions as if a live model had made them.
        var settings = InferenceProviderEnvironment.Resolve(Env(("AI_INFERENCE_PROVIDER", "bitdeer")));

        await using var viewModel = new MainWindowViewModel(settings) { SelectedModelIndex = 1 };

        Assert.False(viewModel.IsConfigurationValid);
        Assert.False(viewModel.RunCommand.CanExecute(null));
        Assert.Contains("BITDEER_API_KEY", viewModel.ModelReadinessText, StringComparison.Ordinal);
        Assert.Contains("Model blocked", viewModel.ConfigurationNotice, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmptyQuestion_DisablesRunAndExplainsWhy()
    {
        await using var viewModel = new MainWindowViewModel
        {
            Question = "   ",
        };

        Assert.False(viewModel.IsConfigurationValid);
        Assert.False(viewModel.RunCommand.CanExecute(null));
        Assert.Equal("Enter the user's request before running.", viewModel.ConfigurationNotice);
    }

    [Fact]
    public async Task ManualChange_ClearsTheCanonicalSceneSelection()
    {
        await using var viewModel = new MainWindowViewModel();
        Assert.NotEqual(viewModel.CleanPresetBackground, viewModel.Level2PresetBackground);

        viewModel.EvilMode = true;

        Assert.False(viewModel.IsCleanPresetSelected);
        Assert.False(viewModel.IsCompromisedPresetSelected);
        Assert.False(viewModel.IsLevel1PresetSelected);
        Assert.False(viewModel.IsLevel2PresetSelected);
        Assert.StartsWith("Custom configuration", viewModel.ConfigurationNotice, StringComparison.Ordinal);
        Assert.Equal(viewModel.CleanPresetBackground, viewModel.Level2PresetBackground);
        Assert.Equal("READY", viewModel.RunBadge);
        Assert.Contains("Custom configuration ready", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MasterOff_TreatsRetainedChildSelectionsAsDormant()
    {
        await using var viewModel = new MainWindowViewModel();
        viewModel.Level2PresetCommand.Execute(null);

        viewModel.GatekeeperEnabled = false;
        viewModel.EvilMode = false;

        Assert.True(viewModel.ContainmentEnabled);
        Assert.Contains("OFF", viewModel.ContainmentLabel, StringComparison.Ordinal);
        Assert.True(viewModel.IsConfigurationValid);
        Assert.True(viewModel.RunCommand.CanExecute(null));
    }

    [Fact]
    public async Task ToolGatesPreset_BrightensOnlyDatabaseAndEmailBeforeDetectAndContainEnablesMcp()
    {
        await using var viewModel = new MainWindowViewModel(null, null, null);

        viewModel.Level1PresetCommand.Execute(null);

        Assert.Equal(1, viewModel.DatabaseGateOpacity);
        Assert.Equal(1, viewModel.EmailGateOpacity);
        Assert.Equal(0.52, viewModel.McpGateOpacity);
        Assert.Equal(viewModel.DatabaseGateSurface, viewModel.EmailGateSurface);
        Assert.NotEqual(viewModel.DatabaseGateSurface, viewModel.McpGateSurface);
        Assert.Contains("RESULT ADMISSION · OFF", viewModel.ResultGateLabel, StringComparison.Ordinal);

        viewModel.Level2PresetCommand.Execute(null);

        Assert.Equal(1, viewModel.DatabaseGateOpacity);
        Assert.Equal(1, viewModel.EmailGateOpacity);
        Assert.Equal(1, viewModel.McpGateOpacity);
        Assert.Equal(viewModel.DatabaseGateSurface, viewModel.McpGateSurface);
        Assert.Contains("RESULT ADMISSION · ON", viewModel.ResultGateLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void AnswerEvent_IsAnObservationRatherThanASafetyVerdict()
    {
        var runtimeEvent = new ControlRoomEvent(
            Guid.NewGuid(),
            10,
            TimeSpan.FromSeconds(1),
            DateTimeOffset.UtcNow,
            PartnerDeskRuntimeEventKind.AgentAnswerProduced,
            "agent",
            "user",
            "Answer produced",
            "A plausible but potentially concealing answer.",
            PartnerDeskRuntimeDisposition.Neutral,
            null,
            null,
            null);

        var item = new EventItemViewModel(runtimeEvent);

        Assert.Equal("MESSAGE", item.Badge);
        Assert.NotEqual("SAFE", item.Badge);
    }

    [Fact]
    public void ModelEvents_UseShortAudienceFacingBadges()
    {
        var runtimeEvent = new ControlRoomEvent(
            Guid.NewGuid(),
            3,
            TimeSpan.FromMilliseconds(20),
            DateTimeOffset.UtcNow,
            PartnerDeskRuntimeEventKind.ModelRequestStarted,
            "agent",
            "model",
            "Model request",
            "Request started.",
            PartnerDeskRuntimeDisposition.Neutral,
            null,
            null,
            null);

        Assert.Equal("MODEL", new EventItemViewModel(runtimeEvent).Badge);
    }

    [Fact]
    public void MessageEvent_ExposesAnIndependentExpandablePayload()
    {
        var runtimeEvent = new ControlRoomEvent(
            Guid.NewGuid(),
            4,
            TimeSpan.FromMilliseconds(30),
            DateTimeOffset.UtcNow,
            PartnerDeskRuntimeEventKind.ModelResponseReceived,
            "model",
            "agent",
            "Model response",
            "Response received.",
            PartnerDeskRuntimeDisposition.Neutral,
            "[ASSISTANT]\nTOOL CALL: get_company_report",
            null,
            null);
        var item = new EventItemViewModel(runtimeEvent);

        Assert.True(item.HasExpandableContent);
        Assert.False(item.IsExpanded);
        Assert.True(item.IsCollapsed);
        Assert.True(item.IsExpandControlVisible);
        Assert.Equal("▶  MODEL OUTPUT", item.ExpandControlText);
        Assert.True(item.ToggleExpandedCommand.CanExecute(null));

        item.ToggleExpandedCommand.Execute(null);

        Assert.True(item.IsExpanded);
        Assert.False(item.IsCollapsed);
        Assert.False(item.IsExpandControlVisible);
        Assert.Equal("▼  MODEL OUTPUT", item.ExpandControlText);
        Assert.Contains("get_company_report", item.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void EventWithoutRetainedPayload_HidesAndDisablesTheExpander()
    {
        var item = new EventItemViewModel(Event(
            5,
            PartnerDeskRuntimeEventKind.AgentWorking,
            "agent",
            "model",
            "Agent working",
            PartnerDeskRuntimeDisposition.Neutral));

        Assert.False(item.HasExpandableContent);
        Assert.False(item.IsExpandControlVisible);
        Assert.False(item.ToggleExpandedCommand.CanExecute(null));
    }

    [Fact]
    public async Task SelectedToolEvent_HighlightsOnlyItsCurrentComponentsAndSegments()
    {
        await using var viewModel = new MainWindowViewModel(null, null, null);
        viewModel.Level1PresetCommand.Execute(null);

        var executionStarted = Event(
            11,
            PartnerDeskRuntimeEventKind.ToolExecutionStarted,
            "agent",
            PartnerDatabaseTool.ToolName,
            "Partner lookup working",
            PartnerDeskRuntimeDisposition.Safe);
        viewModel.SelectedEvent = new EventItemViewModel(executionStarted);

        Assert.Contains("EVENT 011", viewModel.WorkflowFocusText, StringComparison.Ordinal);
        Assert.Equal(viewModel.DatabaseCallRoute, viewModel.DatabaseGateAccent);
        Assert.Equal(viewModel.DatabaseCallRoute, viewModel.DatabaseAllowRoute);
        Assert.Equal(viewModel.DatabaseCallRoute, viewModel.DatabaseNodeAccent);
        Assert.Equal(viewModel.DatabaseCallRoute, viewModel.DispatcherNodeAccent);
        Assert.NotEqual(viewModel.DatabaseCallRoute, viewModel.DatabaseEffectRoute);
        Assert.NotEqual(viewModel.DatabaseCallRoute, viewModel.EmailCallRoute);

        var completed = Event(
            12,
            PartnerDeskRuntimeEventKind.ToolCompleted,
            PartnerDatabaseTool.ToolName,
            "agent",
            "Partner lookup executed",
            PartnerDeskRuntimeDisposition.Safe);
        viewModel.SelectedEvent = new EventItemViewModel(completed);

        Assert.Equal(viewModel.DatabaseNodeAccent, viewModel.DatabaseEffectRoute);
        Assert.Equal(viewModel.DatabaseNodeAccent, viewModel.DatabaseRowsRoute);
        Assert.NotEqual(viewModel.DatabaseNodeAccent, viewModel.DatabaseCallRoute);
        Assert.Equal("Partner lookup executed", viewModel.DatabaseStatus);
    }

    [Fact]
    public async Task PreparingANewRun_ReplacesTheTimelineAndResetsEveryWorkflowSurface()
    {
        await using var viewModel = new MainWindowViewModel(null, null, null);
        var previous = Event(
            19,
            PartnerDeskRuntimeEventKind.ToolCompleted,
            EmailTool.ToolName,
            "agent",
            "Internal email effect executed",
            PartnerDeskRuntimeDisposition.Safe);
        var previousItem = new EventItemViewModel(previous);
        viewModel.StoryEvents.Add(previousItem);
        viewModel.DebugEvents.Add(previousItem);
        viewModel.SecurityEvents.Add(previousItem);
        viewModel.SelectedEvent = previousItem;
        viewModel.AutoFollowEvents = false;

        viewModel.PrepareRunPresentation(
            PartnerDeskRunConfiguration.ForPhase(DemoPhase.Clean),
            clearEvents: true);

        Assert.Empty(viewModel.StoryEvents);
        Assert.Empty(viewModel.DebugEvents);
        Assert.Empty(viewModel.SecurityEvents);
        Assert.Null(viewModel.SelectedEvent);
        Assert.True(viewModel.AutoFollowEvents);
        Assert.Contains("Waiting", viewModel.WorkflowFocusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("new run", viewModel.DatabaseStatus, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("new run", viewModel.EmailStatus, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(viewModel.HumanNodeAccent, viewModel.EmailNodeAccent);
        Assert.Equal(viewModel.UserRequestRoute, viewModel.EmailEffectRoute);
        Assert.NotEqual(viewModel.EmailStatusAccent, viewModel.WorkflowFocusAccent);
    }

    [Fact]
    public async Task ShieldCounters_TrackEachProtectedLanePulseAndResetForTheNextRun()
    {
        await using var viewModel = new MainWindowViewModel(null, null, null);
        var firstDatabaseBlock = new EventItemViewModel(Event(
            21,
            PartnerDeskRuntimeEventKind.GateFindingRecorded,
            "gatekeeper",
            PartnerDatabaseTool.ToolName,
            "Bulk read blocked",
            PartnerDeskRuntimeDisposition.Blocked));
        var secondDatabaseBlock = new EventItemViewModel(Event(
            22,
            PartnerDeskRuntimeEventKind.GateFindingRecorded,
            "gatekeeper",
            PartnerDatabaseTool.ToolName,
            "Second bulk read blocked",
            PartnerDeskRuntimeDisposition.Blocked));
        var emailBlock = new EventItemViewModel(Event(
            23,
            PartnerDeskRuntimeEventKind.GateFindingRecorded,
            "gatekeeper",
            EmailTool.ToolName,
            "External email blocked",
            PartnerDeskRuntimeDisposition.Blocked));
        var resultWithheld = new EventItemViewModel(Event(
            24,
            PartnerDeskRuntimeEventKind.ResultWithheld,
            "gatekeeper",
            "mcp",
            "Untrusted result withheld",
            PartnerDeskRuntimeDisposition.Blocked));

        viewModel.StoryEvents.Add(firstDatabaseBlock);
        viewModel.StoryEvents.Add(secondDatabaseBlock);
        viewModel.StoryEvents.Add(emailBlock);
        viewModel.StoryEvents.Add(resultWithheld);

        Assert.Equal(2, viewModel.DatabaseShieldCount);
        Assert.Equal(1, viewModel.EmailShieldCount);
        Assert.Equal(1, viewModel.McpShieldCount);
        Assert.Contains("2 enforced actions", viewModel.DatabaseShieldToolTip, StringComparison.Ordinal);

        viewModel.SelectedEvent = firstDatabaseBlock;

        Assert.Equal(36, viewModel.DatabaseShieldSize);
        Assert.Equal(1, viewModel.DatabaseShieldOpacity);
        Assert.Equal(32, viewModel.McpShieldSize);
        Assert.Equal(0.72, viewModel.McpShieldOpacity);

        viewModel.PrepareRunPresentation(
            PartnerDeskRunConfiguration.ForPhase(DemoPhase.Clean),
            clearEvents: true);

        Assert.Equal(0, viewModel.DatabaseShieldCount);
        Assert.Equal(0, viewModel.EmailShieldCount);
        Assert.Equal(0, viewModel.McpShieldCount);
        Assert.Equal(32, viewModel.DatabaseShieldSize);
        Assert.Equal(0.56, viewModel.DatabaseShieldOpacity);
    }

    private static ControlRoomEvent Event(
        long sequence,
        PartnerDeskRuntimeEventKind kind,
        string source,
        string target,
        string title,
        PartnerDeskRuntimeDisposition disposition) =>
        new(
            Guid.NewGuid(),
            sequence,
            TimeSpan.FromMilliseconds(sequence * 10),
            DateTimeOffset.UtcNow,
            kind,
            source,
            target,
            title,
            title,
            disposition,
            null,
            null,
            null);

    /// <summary>
    /// A dictionary in place of the process environment, so these tests configure a provider without touching
    /// global mutable state that the rest of the suite would then see.
    /// </summary>
    private static Func<string, string?> Env(params (string Name, string Value)[] variables)
    {
        var map = variables.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
        return name => map.TryGetValue(name, out var value) ? value : null;
    }
}
