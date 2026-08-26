using Avalonia.Controls;
using Avalonia.Threading;
using GatekeeperDemo.App.ViewModels;
using System.Collections.Specialized;

namespace GatekeeperDemo.App;

public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private bool _disposeInProgress;
    private bool _closeAfterDispose;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        _viewModel.StoryEvents.CollectionChanged += (_, args) => ScrollLatest(StoryList, args, respectFollowMode: true);
        _viewModel.SecurityEvents.CollectionChanged += (_, args) => ScrollLatest(SecurityList, args, respectFollowMode: true);
        _viewModel.DebugEvents.CollectionChanged += (_, args) => ScrollLatest(DebugList, args, respectFollowMode: true);
        _viewModel.EvaluationProgress.CollectionChanged += (_, args) =>
            ScrollLatest(EvaluationProgressList, args, respectFollowMode: false);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeAfterDispose)
        {
            return;
        }

        e.Cancel = true;
        if (_disposeInProgress)
        {
            return;
        }

        _disposeInProgress = true;
        await _viewModel.DisposeAsync();
        _closeAfterDispose = true;
        Close();
    }

    private void ScrollLatest(ListBox list, NotifyCollectionChangedEventArgs args, bool respectFollowMode)
    {
        if (args.NewItems is not { Count: > 0 }
            || (respectFollowMode && !_viewModel.AutoFollowEvents))
        {
            return;
        }

        var item = args.NewItems[args.NewItems.Count - 1];
        if (item is null)
        {
            return;
        }
        Dispatcher.UIThread.Post(() => list.ScrollIntoView(item), DispatcherPriority.Background);
    }
}
