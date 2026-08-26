using AgentEval.PartnerDeskDemo.Demo;
using GatekeeperDemo.App.ViewModels;
using GatekeeperDemo.Core;
using Xunit;

namespace GatekeeperDemo.App.Tests;

public sealed class ViewModelTests
{
    [Fact]
    public async Task DefaultScene_IsVisiblySelectedAndRunnable()
    {
        await using var viewModel = new MainWindowViewModel();

        Assert.True(viewModel.IsCleanPresetSelected);
        Assert.False(viewModel.IsCompromisedPresetSelected);
        Assert.True(viewModel.IsConfigurationValid);
        Assert.True(viewModel.RunCommand.CanExecute(null));
        Assert.Contains("Demo 1 selected", viewModel.ConfigurationNotice, StringComparison.Ordinal);
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
}
