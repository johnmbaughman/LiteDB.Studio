using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LiteDB.Studio.Wpf.ViewModels
{
    public class DbTreeNode : ObservableObject
    {
        public DbTreeNode()
        {
            Children = new ObservableCollection<DbTreeNode>();
        }

        public string Header { get; set; } = string.Empty;

        public string? Tag { get; set; }

        // pack uri to resource image (e.g. Resources/table.png)
        public string? Icon { get; set; }

        public ObservableCollection<DbTreeNode> Children { get; }
    }
}
