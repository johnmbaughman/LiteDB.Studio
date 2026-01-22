using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LiteDB.Studio.Wpf.Converters;
using LiteDB.Studio.Wpf.Services;
using Serilog;

namespace LiteDB.Studio.Wpf.ViewModels
{
    public partial class ResultGridViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;

        [ObservableProperty]
        private string _collectionName = string.Empty;

        [ObservableProperty]
        private QueryResult? _queryResult;

        [ObservableProperty]
        private ObservableCollection<DataGridColumn> _columns = new();

        public ResultGridViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        partial void OnQueryResultChanged(QueryResult? value)
        {
            UpdateColumns();
        }

        private void UpdateColumns()
        {
            Columns.Clear();
            if (QueryResult?.Columns == null) return;

            var converter = new BsonValueToStringConverter();

            foreach (var column in QueryResult.Columns)
            {
                var binding = new Binding($"[{column.Name}]") { Converter = converter };
                var tooltipBinding = new Binding($"[{column.Name}]") { Converter = converter, ConverterParameter = "full" };
                var dataGridColumn = new DataGridTextColumn
                {
                    Header = column.Name,
                    Binding = binding
                };
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.ToolTipProperty, tooltipBinding));
                dataGridColumn.ElementStyle = style;
                Columns.Add(dataGridColumn);
            }
        }

        public async Task UpdateCellValueAsync(object row, string columnName, object newValue, CancellationToken cancellationToken)
        {
            if (row is not BsonDocument document)
                return;

            if (!document.TryGetValue("_id", out var idValue))
                return;

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
                Serilog.Log.Error(ex, "Failed to update document field {Field} in collection {Collection}", columnName, GetCollectionName());
                // Perhaps show message or revert
            }
        }

        private string GetCollectionName()
        {
            return CollectionName;
        }
    }
}