using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.Util;
using Microsoft.Extensions.Logging;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class ResultGridViewModel(IDatabaseService databaseService, ILogger<ResultGridViewModel> logger) : ObservableObject
{
    private readonly IDatabaseService _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    private readonly ILogger<ResultGridViewModel> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [ObservableProperty]
    private string _collectionName = string.Empty;

    [ObservableProperty]
    private QueryResult? _queryResult;

    [ObservableProperty]
    private ObservableCollection<ColumnDescriptor> _columns = [];

    // TODO: Use passed variable.
    partial void OnQueryResultChanged(QueryResult? value)
    {
        // Update the column definitions when a new QueryResult arrives
        UpdateColumns();
    }

    private void UpdateColumns()
    {
        Columns.Clear();
        if (QueryResult?.Columns == null) {
            return;
        }

        foreach (ColumnInfo column in QueryResult.Columns)
        {
            // Expose a UI-agnostic descriptor for the view to build UI-specific columns
            Columns.Add(new ColumnDescriptor(column.Name, column.Name, IsEditable: false));
        }
    }

    public async Task UpdateCellValueAsync(object row, string columnName, object newValue, CancellationToken cancellationToken)
    {
        if (row is not BsonDocument document) {
            return;
        }

        if (!document.TryGetValue("_id", out BsonValue? idValue)) {
            return;
        }

        try
        {
            await _databaseService.UpdateDocumentFieldAsync(
                collectionName: GetCollectionName(), // Need to determine collection name
                documentId: idValue.RawValue,
                fieldPath: columnName,
                newValue: newValue,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "Failed to update document field {Field} in collection {Collection}", columnName, GetCollectionName());
            // Perhaps show message or revert
        }
    }

    private string GetCollectionName()
    {
        return CollectionName;
    }
}
