using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System.Collections.ObjectModel;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class DbTreeNode(
    IDatabaseService databaseService,
    IDialogService dialogService,
    IFileDialogService fileDialogService,
    IFileService fileService,
    Action<string>? insertSnippetAction = null) : ObservableObject
{
    private readonly IDatabaseService _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    private readonly IDialogService _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    private readonly IFileDialogService _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
    private readonly IFileService _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

    /// <summary>Gets or sets the display text for this node.</summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>Gets or sets the semantic tag (e.g. <c>"collection"</c>, <c>"system"</c>, <c>"field"</c>).</summary>
    public string? Tag { get; set; }

    /// <summary>Gets or sets a pack URI for the node icon (e.g. <c>pack://application:,,,/Resources/Icons/table.png</c>).</summary>
    public string? IconUri { get; set; }

    /// <summary>Gets the child nodes of this tree node.</summary>
    public ObservableCollection<DbTreeNode> Children { get; } = [];

    /// <summary>Gets or sets a value indicating whether this node's children have been loaded from the database.</summary>
    [ObservableProperty]
    private bool _isLoaded;

    /// <summary>Gets or sets a value indicating whether this node is expanded in the tree.</summary>
    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private async Task LoadChildrenAsync(CancellationToken cancellationToken)
    {
        if (IsLoaded) {
            return;
        }

        Children.Clear();

        if (Tag == "collection")
        {
            IEnumerable<ColumnInfo> schema = await _databaseService.GetCollectionSchemaAsync(Header, cancellationToken);
            foreach (ColumnInfo column in schema)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childNode = new DbTreeNode(_databaseService, _dialogService, _fileDialogService, _fileService)
                {
                    Header = column.Name,
                    Tag = "field",
                    IconUri = "pack://application:,,,/Resources/Icons/field.png"
                };
                Children.Add(childNode);
            }
        }

        IsLoaded = true;
    }

    [RelayCommand]
    private async Task DropAsync(CancellationToken cancellationToken)
    {
        if (Tag != "collection") {
            return;
        }

        var message = $"Are you sure you want to drop the collection '{Header}'? This action cannot be undone.";
        var confirmed = _dialogService.Confirm(message, "Confirm Drop", DialogIcon.Warning);
        if (!confirmed) {
            return;
        }

        var dropQuery = $"DROP COLLECTION {Header}";
        await _databaseService.ExecuteAsync(dropQuery, cancellationToken);

        // Note: In a full implementation, the tree should be refreshed after drop
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        if (Tag != "collection") {
            return;
        }

        var filename = _fileDialogService.SaveFile(new SaveFileDialogOptions
        {
            Title = $"Export {Header} collection",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"{Header}.json"
        });

        if (string.IsNullOrWhiteSpace(filename)) {
            return;
        }

        var selectQuery = $"SELECT $ FROM {Header}";
        QueryResult result = await _databaseService.ExecuteAsync(selectQuery, cancellationToken);

        // Export to JSON
        var json = System.Text.Json.JsonSerializer.Serialize(result.Rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await _fileService.WriteAllTextAsync(filename, json, cancellationToken);
    }

    [RelayCommand]
    private void InsertSnippet()
    {
        // Support systems collections the same way as regular collections (double-click / insert snippet)
        if (Tag != "collection" && Tag != "field" && Tag != "system") {
            return;
        }

        var snippet =
            // Use $ as the projection operator for fetching the full document
            Tag is "collection" or "system" ? $"SELECT $ FROM {Header};" :
            // For field, perhaps insert the field name
            Header;

        insertSnippetAction?.Invoke(snippet);
    }
}
