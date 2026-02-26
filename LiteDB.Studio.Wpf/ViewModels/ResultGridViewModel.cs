using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging;

namespace LiteDB.Studio.Wpf.ViewModels;

/// <summary>ViewModel that backs the result grid, managing column descriptors and cell-update operations.</summary>
public partial class ResultGridViewModel(IDatabaseService databaseService, ILogger<ResultGridViewModel> logger) : ObservableObject
{
    private readonly IDatabaseService _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    private readonly ILogger<ResultGridViewModel> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Gets or sets the name of the collection whose rows are displayed. Used for inline edits.</summary>
    [ObservableProperty]
    private string _collectionName = string.Empty;

    /// <summary>Gets or sets the current query result bound to the grid.</summary>
    [ObservableProperty]
    private QueryResult? _queryResult;

    /// <summary>Gets or sets the column descriptors derived from the current <see cref="QueryResult"/>.</summary>
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

    /// <summary>Persists an inline cell edit to the database.</summary>
    /// <param name="row">The row object (expected to be a <c>BsonDocument</c>).</param>
    /// <param name="columnName">The name of the field to update.</param>
    /// <param name="newValue">The new value to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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
