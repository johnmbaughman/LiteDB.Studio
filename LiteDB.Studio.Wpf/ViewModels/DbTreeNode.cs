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
    private readonly Action<string>? _insertSnippetAction = insertSnippetAction;

    public string Header { get; set; } = string.Empty;

    public string? Tag { get; set; }

    // pack uri to resource image (e.g. pack://application:,,,/Resources/Icons/table.png)
    public string? IconUri { get; set; }

    public ObservableCollection<DbTreeNode> Children { get; } = [];

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private async Task LoadChildrenAsync()
    {
        if (IsLoaded) {
            return;
        }

        Children.Clear();

        if (Tag == "collection")
        {
            IEnumerable<ColumnInfo> schema = await _databaseService.GetCollectionSchemaAsync(Header, CancellationToken.None);
            foreach (ColumnInfo column in schema)
            {
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
    private async Task DropAsync()
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
        await _databaseService.ExecuteAsync(dropQuery, CancellationToken.None);

        // Note: In a full implementation, the tree should be refreshed after drop
    }

    [RelayCommand]
    private async Task ExportAsync()
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
        QueryResult result = await _databaseService.ExecuteAsync(selectQuery, CancellationToken.None);

        // Export to JSON
        var json = System.Text.Json.JsonSerializer.Serialize(result.Rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await _fileService.WriteAllTextAsync(filename, json);
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

        _insertSnippetAction?.Invoke(snippet);
    }
}
