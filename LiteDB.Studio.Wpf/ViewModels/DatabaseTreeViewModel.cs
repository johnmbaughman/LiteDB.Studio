using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Studio.Wpf.ViewModels
{
    public partial class DatabaseTreeViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;

        public event EventHandler<string>? InsertSnippetRequested;

        public DatabaseTreeViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            RootNodes = new ObservableCollection<DbTreeNode>();
            RootNodes.CollectionChanged += RootNodes_CollectionChanged;
        }

        [ObservableProperty]
        private ObservableCollection<DbTreeNode> _rootNodes;

        [ObservableProperty]
        private int _collectionsCount;

        [ObservableProperty]
        private int _systemCount;

        public string StatusText => $"Collections: {CollectionsCount} / System: {SystemCount}";

        private void RootNodes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DbTreeNode node in e.NewItems)
                {
                    node.Children.CollectionChanged += ChildNodes_CollectionChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (DbTreeNode node in e.OldItems)
                {
                    node.Children.CollectionChanged -= ChildNodes_CollectionChanged;
                }
            }

            RecalculateCounts();
        }

        private void ChildNodes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RecalculateCounts();
        }

        private void RecalculateCounts()
        {
            try
            {
                var collections = 0;
                var system = 0;

                foreach (var root in RootNodes)
                {
                    foreach (var child in root.Children)
                    {
                        if (child.Tag == "collection") collections++;
                        else if (child.Tag == "systemfolder")
                        {
                            system += child.Children.Count;
                        }
                    }
                }

                CollectionsCount = collections;
                SystemCount = system;
                OnPropertyChanged(nameof(StatusText));
            }
            catch { }
        }
        public async Task LoadRootNodesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                RootNodes.Clear();

                var collectionNames = (await _databaseService.GetCollectionNamesAsync(cancellationToken)).ToList();
                var systemCollectionNames = (await _databaseService.GetSystemCollectionNamesAsync(cancellationToken)).ToList();

                Serilog.Log.Debug("Loading database tree - collections: {Count}, system: {SystemCount}", collectionNames.Count, systemCollectionNames.Count);

                Action<string> insertSnippet = snippet => InsertSnippetRequested?.Invoke(this, snippet);

                // Create root database node
                var databaseName = "Database";
                var rootNode = new DbTreeNode(_databaseService, insertSnippet)
                {
                    Header = System.IO.Path.GetFileName(databaseName),
                    Tag = "database",
                    IconUri = "pack://application:,,,/Resources/Icons/database.png"
                };

                // Add system collections under root
                var systemNode = new DbTreeNode(_databaseService, insertSnippet)
                {
                    Header = "System",
                    Tag = "systemfolder",
                    IconUri = "pack://application:,,,/Resources/Icons/system.png" // or a folder icon, but using system.png
                };
                foreach (var name in systemCollectionNames)
                {
                    var node = new DbTreeNode(_databaseService, insertSnippet)
                    {
                        Header = name,
                        Tag = "system",
                        IconUri = "pack://application:,,,/Resources/Icons/system.png"
                    };
                    systemNode.Children.Add(node);
                }
                rootNode.Children.Add(systemNode);

                // Add user collections directly under root
                foreach (var name in collectionNames)
                {
                    var node = new DbTreeNode(_databaseService, insertSnippet)
                    {
                        Header = name,
                        Tag = "collection",
                        IconUri = "pack://application:,,,/Resources/Icons/collection.png"
                    };
                    rootNode.Children.Add(node);
                }

                RootNodes.Add(rootNode);

                // Ensure counts reflect newly loaded nodes
                RecalculateCounts();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load root nodes");
                throw;
            }
        }
    }
}