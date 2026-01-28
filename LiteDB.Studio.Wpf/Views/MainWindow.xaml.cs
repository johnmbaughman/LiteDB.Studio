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
    private readonly DatabaseTreeView _databaseTreeView;

    public MainWindow(MainViewModel viewModel, DatabaseTreeView databaseTreeView)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(databaseTreeView);

        _viewModel = viewModel;
        _databaseTreeView = databaseTreeView;

        InitializeComponent();

        DataContext = _viewModel;

        // Set the DatabaseTreeView's Content property after InitializeComponent
        DatabaseTreeViewHost.Content = _databaseTreeView;

        if (_viewModel is IShellContentViewModel shellContentViewModel)
        {
            shellContentViewModel.View = this;
        }

        Loaded += async (_, _) => await ShellContentViewModel.ViewLoaded();
    }

    private void LoadLastDb_Click(object sender, RoutedEventArgs e)
    {
        var last = Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;

        if (!string.IsNullOrEmpty(last))
        {
            _ = _viewModel.OpenRecentAsync(last);
        }
    }

    public IShellContentViewModel ShellContentViewModel => _viewModel;

    public IViewModel ViewModel => _viewModel;
}
