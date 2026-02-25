using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views;
using LiteDB.Studio.Mvvm.Views.Shell;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views;

public partial class MainWindow : IShellContentView, IView
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel, DatabaseTreeView databaseTreeView, DebuggerView debuggerView)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(databaseTreeView);
        ArgumentNullException.ThrowIfNull(debuggerView);

        _viewModel = viewModel;

        InitializeComponent();

        DataContext = _viewModel;

        // Set the DatabaseTreeView's Content property after InitializeComponent
        DatabaseTreeViewHost.Content = databaseTreeView;
        DebuggerViewHost.Content = debuggerView;

        Loaded += async (_, _) =>
        {
            await ShellContentViewModel.ViewLoaded();
        };
    }

    public IShellContentViewModel ShellContentViewModel => _viewModel;

    public IViewModel ViewModel => _viewModel;

}


