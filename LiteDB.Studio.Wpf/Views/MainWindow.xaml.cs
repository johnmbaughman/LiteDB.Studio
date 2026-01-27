using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views;
using LiteDB.Studio.Mvvm.Views.Shell;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views;

// TODO: Implement abstract base class instead of interface.
public partial class MainWindow : IShellContentView, IViewFor<MainViewModel>
{
    private readonly DatabaseTreeView _databaseTreeView;

    public MainWindow(MainViewModel viewModel, DatabaseTreeView databaseTreeView)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(databaseTreeView);

        _databaseTreeView = databaseTreeView;

        InitializeComponent();

        DataContext = viewModel;

        // Set the DatabaseTreeView's Content property after InitializeComponent
        DatabaseTreeViewHost.Content = _databaseTreeView;

        if (viewModel is IShellContentViewModel shellContentViewModel)
        {
            shellContentViewModel.View = this;
        }

        Loaded += async (_, _) => await ShellContentViewModel.ViewLoaded();
    }

    private void LoadLastDb_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;

        var last = Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;

        if (!string.IsNullOrEmpty(last) && vm != null)
        {
            _ = vm.OpenRecentAsync(last);
        }
    }

    public IShellContentViewModel ShellContentViewModel => (IShellContentViewModel)DataContext;

    public MainViewModel ViewModel => (MainViewModel)DataContext;

    IViewModel IView.ViewModel => ViewModel;
}
