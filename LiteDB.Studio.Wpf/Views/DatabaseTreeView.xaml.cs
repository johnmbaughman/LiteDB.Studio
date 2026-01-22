using System.Windows;
using System.Windows.Controls;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views
{
    public partial class DatabaseTreeView : UserControl
    {
        public DatabaseTreeView()
        {
            InitializeComponent();
            TreeView.MouseDoubleClick += TreeView_MouseDoubleClick;
        }

        private void TreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TreeView.SelectedItem is DbTreeNode node)
            {
                node.InsertSnippetCommand.Execute(null);
            }
        }
    }
}