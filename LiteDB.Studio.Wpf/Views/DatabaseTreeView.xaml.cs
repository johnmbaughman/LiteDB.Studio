using System.Windows;
using System.Windows.Controls;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views;

public partial class DatabaseTreeView : UserControl
{
    public DatabaseTreeView()
    {
        InitializeComponent();
        TreeView.MouseDoubleClick += TreeView_MouseDoubleClick;
    }

    private void TreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TreeView.SelectedItem is not DbTreeNode node) { return; }

        // On double-click, open a NEW editor tab and insert the snippet (always create new tab)
        string snippet;
        // System collections should behave like regular collections for double-click insertion
        if (node.Tag == "collection" || node.Tag == "system") snippet = $"SELECT $ FROM {node.Header};";
        else snippet = node.Header;

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
}