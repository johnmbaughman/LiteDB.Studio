using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Controls
{
    public partial class ResultGrid : UserControl
    {
        public ResultGrid()
        {
            InitializeComponent();
            ResultsDataGrid.CellEditEnding += ResultsDataGrid_CellEditEnding;
            DataContextChanged += ResultGrid_DataContextChanged;
        }

        public static readonly DependencyProperty QueryResultProperty =
            DependencyProperty.Register("QueryResult", typeof(Services.QueryResult), typeof(ResultGrid), new PropertyMetadata(null, OnQueryResultChanged));

        public Services.QueryResult QueryResult
        {
            get => (Services.QueryResult)GetValue(QueryResultProperty);
            set => SetValue(QueryResultProperty, value);
        }

        private static void OnQueryResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ResultGrid)d;
            if (control.DataContext is ResultGridViewModel viewModel)
            {
                viewModel.QueryResult = (Services.QueryResult)e.NewValue;
            }
        }

        private void ResultGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ResultGridViewModel viewModel)
            {
                UpdateColumnsFromViewModel(viewModel);
                viewModel.Columns.CollectionChanged += (s, args) => UpdateColumnsFromViewModel(viewModel);
            }
        }

        private void UpdateColumnsFromViewModel(ResultGridViewModel viewModel)
        {
            ResultsDataGrid.Columns.Clear();
            foreach (var col in viewModel.Columns)
            {
                ResultsDataGrid.Columns.Add(col);
            }
        }

        private async void ResultsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (DataContext is not ResultGridViewModel viewModel)
                return;

            if (e.EditAction != DataGridEditAction.Commit)
                return;

            var row = e.Row.Item;
            var column = e.Column as DataGridTextColumn;
            if (column == null)
                return;

            var textBox = e.EditingElement as TextBox;
            if (textBox == null)
                return;

            var newValue = textBox.Text;
            var columnName = column.Header.ToString();

            // Parse the new value to appropriate type
            object parsedValue = ParseValue(newValue);

            await viewModel.UpdateCellValueAsync(row, columnName, parsedValue, CancellationToken.None);
        }

        private object ParseValue(string value)
        {
            // Simple parsing, can be improved
            if (int.TryParse(value, out var i)) return i;
            if (long.TryParse(value, out var l)) return l;
            if (double.TryParse(value, out var d)) return d;
            if (bool.TryParse(value, out var b)) return b;
            return value;
        }
    }
}