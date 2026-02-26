using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class DatabaseTreeViewModel : ViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;

    /// <summary>Raised when a tree node requests that a snippet be inserted at the editor caret.</summary>
    public event EventHandler<string>? InsertSnippetRequested;
    /// <summary>Raised when a tree node requests that a SQL snippet be placed in a tab.</summary>
    public event EventHandler<string>? AddSqlSnippetRequested;

    public DatabaseTreeViewModel(
        IDatabaseService databaseService,
        IDialogService dialogService,
        IFileDialogService fileDialogService,
        IFileService fileService,
        ILogger<DatabaseTreeViewModel> logger) : base(logger)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        RootNodes = [];
        RootNodes.CollectionChanged += RootNodes_CollectionChanged;
    }

    /// <summary>Gets or sets the root-level nodes of the database tree (one per open database).</summary>
    [ObservableProperty]
    private ObservableCollection<DbTreeNode> _rootNodes;

    /// <summary>Gets or sets the count of user collections shown in the tree.</summary>
    [ObservableProperty]
    private int _collectionsCount;

    /// <summary>Gets or sets the count of system collections shown in the tree.</summary>
    [ObservableProperty]
    private int _systemCount;

    /// <summary>Gets a formatted status string summarising collection counts.</summary>
    public string StatusText => $"Collections: {CollectionsCount} / System: {SystemCount}";

    [RelayCommand]
    private void OpenNodeInNewTab(DbTreeNode? node)
    {
        if (node == null) {
            return;
        }

        if (node.Tag != "collection" && node.Tag != "field" && node.Tag != "system") {
            return;
        }

        var snippet =
            node.Tag is "collection" or "system" ? $"SELECT $ FROM {node.Header};" :
            node.Header;

        RequestAddSqlSnippet(snippet);
    }

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
            Logger.LogError(ex, "Exception recalculating counts: {Message}", ex.Message);
        }
    }
    /// <summary>Reloads the tree by querying collection names from the database service.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task LoadRootNodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            RootNodes.Clear();

            var collectionNames = (await _databaseService.GetCollectionNamesAsync(cancellationToken)).ToList();
            var systemCollectionNames = (await _databaseService.GetSystemCollectionNamesAsync(cancellationToken)).ToList();

            // Remove any names reported as system collections to avoid duplicates and miscategorization
            collectionNames = collectionNames.Except(systemCollectionNames).ToList();

            Logger.LogDebug("Loading database tree - collections: {Count}, system: {SystemCount}", collectionNames.Count, systemCollectionNames.Count);

            Action<string> insertSnippet = snippet => InsertSnippetRequested?.Invoke(this, snippet);

            // Create root database node
            const string databaseName = "Database";
            var rootNode = new DbTreeNode(_databaseService, _dialogService, _fileDialogService, _fileService, insertSnippet)
            {
                Header = System.IO.Path.GetFileName(databaseName),
                Tag = "database",
                IconUri = "pack://application:,,,/Resources/Icons/database.png"
            };

            // Add system collections under root (sorted ascending)
            var systemNode = new DbTreeNode(_databaseService, _dialogService, _fileDialogService, _fileService, insertSnippet)
            {
                Header = "System",
                Tag = "systemfolder",
                IconUri = "pack://application:,,,/Resources/Icons/system.png" // or a folder icon, but using system.png
            };

            foreach (var name in systemCollectionNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                var node = new DbTreeNode(_databaseService, _dialogService, _fileDialogService, _fileService, insertSnippet)
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
                var node = new DbTreeNode(_databaseService, _dialogService, _fileDialogService, _fileService, insertSnippet)
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
            Logger.LogError(ex, "Failed to load root nodes");
            throw;
        }
    }

    /// <summary>Raises <see cref="AddSqlSnippetRequested"/> with the given <paramref name="snippet"/>.</summary>
    /// <param name="snippet">SQL text to forward to the tab manager.</param>
    public void RequestAddSqlSnippet(string snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet)) {
            return;
        }

        AddSqlSnippetRequested?.Invoke(this, snippet);
    }

    public override void RegisterMessengerReceivers() => throw new NotImplementedException();
}
