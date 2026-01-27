using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB.Studio.Wpf.Services;
using Serilog;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class DatabaseTreeViewModel : ViewModel
{
    private readonly IDatabaseService _databaseService;

    public event EventHandler<string>? InsertSnippetRequested;

    public DatabaseTreeViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        RootNodes = [];
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

            foreach (DbTreeNode root in RootNodes)
            {
                foreach (DbTreeNode child in root.Children)
                {
                    switch (child.Tag)
                    {
                        case "collection":
                            collections++;
                            break;
                        case "systemfolder":
                            system += child.Children.Count;
                            break;
                    }
                }
            }

            CollectionsCount = collections;
            SystemCount = system;
            OnPropertyChanged(nameof(StatusText));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception recalculating counts: {Message}", ex.Message);
        }
    }
    public async Task LoadRootNodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            RootNodes.Clear();

            var collectionNames = (await _databaseService.GetCollectionNamesAsync(cancellationToken)).ToList();
            var systemCollectionNames = (await _databaseService.GetSystemCollectionNamesAsync(cancellationToken)).ToList();

            // Remove any names reported as system collections to avoid duplicates and miscategorization
            collectionNames = collectionNames.Except(systemCollectionNames).ToList();

            Log.Debug("Loading database tree - collections: {Count}, system: {SystemCount}", collectionNames.Count, systemCollectionNames.Count);

            Action<string> insertSnippet = snippet => InsertSnippetRequested?.Invoke(this, snippet);

            // Create root database node
            const string databaseName = "Database";
            var rootNode = new DbTreeNode(_databaseService, insertSnippet)
            {
                Header = System.IO.Path.GetFileName(databaseName),
                Tag = "database",
                IconUri = "pack://application:,,,/Resources/Icons/database.png"
            };

            // Add system collections under root (sorted ascending)
            var systemNode = new DbTreeNode(_databaseService, insertSnippet)
            {
                Header = "System",
                Tag = "systemfolder",
                IconUri = "pack://application:,,,/Resources/Icons/system.png" // or a folder icon, but using system.png
            };

            foreach (var name in systemCollectionNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
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

            // Add user collections directly under root (sorted ascending)
            foreach (var name in collectionNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
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
            Log.Error(ex, "Failed to load root nodes");
            throw;
        }
    }

    public override void RegisterMessengerReceivers() => throw new NotImplementedException();
}
