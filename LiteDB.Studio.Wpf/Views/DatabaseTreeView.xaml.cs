using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels;
using LiteDB.Studio.Mvvm.Views;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views;

// TODO: Implement abstract base class instead of interface.
public partial class DatabaseTreeView : IContentView
{
    private readonly DatabaseTreeViewModel _viewModel;

    public DatabaseTreeView(DatabaseTreeViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        _viewModel = viewModel;

        InitializeComponent();

        DataContext = _viewModel;

        Loaded += async (_, _) => await ViewModel.ViewLoaded();
        TreeView.MouseDoubleClick += TreeView_MouseDoubleClick;
    }

    private void TreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TreeView.SelectedItem is not DbTreeNode node) { return; }

        // TODO: move this logic to ViewModel
        // On double-click, open a NEW editor tab and insert the snippet (always create new tab)
        var snippet =
            // System collections should behave like regular collections for double-click insertion
            node.Tag is "collection" or "system" ? $"SELECT $ FROM {node.Header};" : node.Header;

        // Try to find MainViewModel via Window DataContext and call AddSqlSnippet to force a new tab insertion
        if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
        {
            vm.AddSqlSnippet(snippet);
        }
        else
        {
            // Fallback: invoke the node command which will insert into current tab or create one if empty
            node.InsertSnippetCommand.Execute(null);
        }
    }

    public IViewModel ViewModel => _viewModel;
}
