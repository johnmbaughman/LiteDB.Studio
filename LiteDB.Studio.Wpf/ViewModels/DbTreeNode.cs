using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class DbTreeNode(IDatabaseService databaseService, Action<string>? insertSnippetAction = null, Func<string, bool>? confirmer = null) : ObservableObject
{
    private readonly IDatabaseService _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    
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
        if (IsLoaded) return;

        Children.Clear();

        if (Tag == "collection")
        {
            var schema = await _databaseService.GetCollectionSchemaAsync(Header, CancellationToken.None);
            foreach (var column in schema)
            {
                var childNode = new DbTreeNode(_databaseService)
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
        if (Tag != "collection") return;

        var message = $"Are you sure you want to drop the collection '{Header}'? This action cannot be undone.";
        var confirmed = confirmer?.Invoke(message) ?? (MessageBox.Show(message, "Confirm Drop", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes);
        if (!confirmed) return;

        var dropQuery = $"DROP COLLECTION {Header}";
        await _databaseService.ExecuteAsync(dropQuery, CancellationToken.None);

        // Note: In a full implementation, the tree should be refreshed after drop
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (Tag != "collection") return;

        var saveFileDialog = new SaveFileDialog
        {
            Title = $"Export {Header} collection",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"{Header}.json"
        };

        if (saveFileDialog.ShowDialog() != true) return;

        var selectQuery = $"SELECT $ FROM {Header}";
        var result = await _databaseService.ExecuteAsync(selectQuery, CancellationToken.None);

        // Export to JSON
        var json = System.Text.Json.JsonSerializer.Serialize(result.Rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(saveFileDialog.FileName, json);
    }

    [RelayCommand]
    private void InsertSnippet()
    {
        // Support systems collections the same way as regular collections (double-click / insert snippet)
        if (Tag != "collection" && Tag != "field" && Tag != "system") return;

        string snippet;
        if (Tag == "collection" || Tag == "system")
        {
            // Use $ as the projection operator for fetching the full document
            snippet = $"SELECT $ FROM {Header};";
        }
        else
        {
            // For field, perhaps insert the field name
            snippet = Header;
        }

        insertSnippetAction?.Invoke(snippet);
    }
}