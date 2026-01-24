using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media;
using System.Linq;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Controls
{
    public partial class ResultGrid : UserControl
    {
        public ResultGrid()
        {
            InitializeComponent();
            ResultsDataGrid.CellEditEnding += ResultsDataGrid_CellEditEnding;
            ResultsDataGrid.LoadingRow += ResultsDataGrid_LoadingRow;
            ResultsDataGrid.Sorting += ResultsDataGrid_Sorting;
            ResultsDataGrid.AddHandler(DataGridRowHeader.MouseLeftButtonDownEvent, new MouseButtonEventHandler(ResultsDataGrid_RowHeaderMouseLeftButtonDown), true);
            // Handle clicks on the row body to support Ctrl+Click and Shift+Click consistently
            ResultsDataGrid.PreviewMouseLeftButtonDown += ResultsDataGrid_PreviewMouseLeftButtonDown;
            ResultsDataGrid.SelectionChanged += ResultsDataGrid_SelectionChanged;
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

        private void ResultGrid_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
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
            _columnNames.Clear();
            foreach (var col in viewModel.Columns)
            {
                // Allow user resizing per column and add to grid
                col.CanUserResize = true;
                ResultsDataGrid.Columns.Add(col);
                // store the logical column name for sorting and header restoration
                _columnNames[col] = col.Header?.ToString() ?? string.Empty;
            }

            // Adjust column widths based on content (similar to WinForms behaviour):
            // 1) Size to cells so we compute a suitable width, 2) fix the width capped at 400px,
            // and leave the column resizable for users.
            AdjustColumnsToContent();
        }

        private void ResultsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Display 1-based row numbers in the row header and attach an indicator element
            try
            {
                var index = e.Row.GetIndex();

                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                // Reserve a fixed small width for the indicator so numbers don't shift when it appears.
                var indicator = new TextBlock { Text = "▶", Visibility = Visibility.Hidden, Foreground = Brushes.Blue, Width = 12, TextAlignment = TextAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
                var number = new TextBlock { Text = (index + 1).ToString(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };

                sp.Children.Add(indicator);
                sp.Children.Add(number);

                e.Row.Header = sp;

                // Store the indicator for quick access
                e.Row.Tag = indicator;

                // Show indicator if this row is the last anchor (use Hidden for layout reservation)
                indicator.Visibility = (index == _lastAnchorRow) ? Visibility.Visible : Visibility.Hidden;
            }
            catch { }
        }

        private async void ResultsDataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
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
            var columnName = column.Header?.ToString() ?? string.Empty;

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

        private void ResultsDataGrid_Sorting(object? sender, DataGridSortingEventArgs e)
        {
            // Handle sorting manually because rows are BsonDocument and column bindings use indexer syntax
            e.Handled = true;

            var column = e.Column;
            var columnName = _columnNames.TryGetValue(column, out var nm) ? nm : column.Header?.ToString();
            if (string.IsNullOrEmpty(columnName)) return;

            // Toggle sort direction
            var direction = column.SortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

            // Clear other column sort indicators and headers
            foreach (var col in ResultsDataGrid.Columns)
            {
                if (col != column)
                {
                    col.SortDirection = null;
                    // restore header text from stored mapping
                    if (_columnNames.TryGetValue(col, out var name)) col.Header = name;
                }
            }

            column.SortDirection = direction;

            // update header to show green arrow
            UpdateColumnHeaderWithSortIndicator(column, direction);

            var view = CollectionViewSource.GetDefaultView(ResultsDataGrid.ItemsSource) as ListCollectionView;
            if (view == null) return;

            view.CustomSort = new BsonColumnComparer(columnName, direction);
        }

        private void UpdateColumnHeaderWithSortIndicator(DataGridColumn column, ListSortDirection direction)
        {
            var name = _columnNames.TryGetValue(column, out var nm) ? nm : column.Header?.ToString() ?? string.Empty;

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = direction == ListSortDirection.Ascending ? "▲" : "▼", Foreground = Brushes.Green, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });

            column.Header = sp;
        }

        private class BsonColumnComparer : System.Collections.IComparer
        {
            private readonly string _columnName;
            private readonly ListSortDirection _direction;

            public BsonColumnComparer(string columnName, ListSortDirection direction)
            {
                _columnName = columnName;
                _direction = direction;
            }

            public int Compare(object? x, object? y)
            {
                // Handle nulls
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return _direction == ListSortDirection.Ascending ? -1 : 1;
                if (y is null) return _direction == ListSortDirection.Ascending ? 1 : -1;

                // Expect rows to be BsonDocument or objects that can be treated like BsonDocument
                var docX = x as LiteDB.BsonDocument;
                var docY = y as LiteDB.BsonDocument;

                LiteDB.BsonValue valX = LiteDB.BsonValue.Null;
                LiteDB.BsonValue valY = LiteDB.BsonValue.Null;

                try
                {
                    if (docX != null) valX = docX[_columnName];
                }
                catch { }

                try
                {
                    if (docY != null) valY = docY[_columnName];
                }
                catch { }

                // Use BsonValue.CompareTo which handles types
                var result = valX.CompareTo(valY);

                return _direction == ListSortDirection.Ascending ? result : -result;
            }
        }

        private int _lastAnchorRow = -1;
        // Temporarily store the index of the row that was last clicked by the user so SelectionChanged can prefer it.
        private int? _lastClickedIndexOverride = null;
        private readonly System.Collections.Generic.Dictionary<DataGridColumn, string> _columnNames = new();

        private void ResultsDataGrid_RowHeaderMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // Disable row resizing entirely: only intercept Thumb instances that belong to a DataGridRowHeader.
            var possibleThumb = FindAncestor<Thumb>(e.OriginalSource as DependencyObject);
            if (possibleThumb != null && FindAncestor<DataGridRowHeader>(possibleThumb) != null)
            {
                // If the Thumb is part of the row header template, block it so rows can't be resized.
                e.Handled = true;
                return;
            }

            // Detect the row header that was clicked
            var header = FindAncestor<DataGridRowHeader>(e.OriginalSource as DependencyObject);
            if (header == null) return;

            var item = header.DataContext;
            var index = ResultsDataGrid.Items.IndexOf(item);
            if (index < 0) return;

            var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            var ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            HandleRowMouseClick(index, shift, ctrl);

            e.Handled = true;
        }

        private void ResultsDataGrid_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // Disable row resizing entirely: only block Thumb interactions that are part of the row header template.
            var possibleThumb = FindAncestor<Thumb>(e.OriginalSource as DependencyObject);
            if (possibleThumb != null && FindAncestor<DataGridRowHeader>(possibleThumb) != null)
            {
                e.Handled = true;
                return;
            }

            // If the click is on the row header (e.g. the number text), handle it here so clicking the number works
            var header = FindAncestor<DataGridRowHeader>(e.OriginalSource as DependencyObject);
            if (header != null)
            {
                var item = header.DataContext;
                var index = ResultsDataGrid.Items.IndexOf(item);
                if (index < 0) return;

                var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                var ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

                HandleRowMouseClick(index, shift, ctrl);
                e.Handled = true;
                return;
            }

            var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row == null) return;

            var index2 = ResultsDataGrid.Items.IndexOf(row.Item);
            if (index2 < 0) return;

            var shift2 = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            var ctrl2 = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            HandleRowMouseClick(index2, shift2, ctrl2);

            e.Handled = true;
        }

        private void ResultsDataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // If a mouse click recently determined the index, prefer it (ensures Shift/Ctrl clicks set the indicator to the clicked row)
            if (_lastClickedIndexOverride.HasValue)
            {
                _lastAnchorRow = _lastClickedIndexOverride.Value;
                _lastClickedIndexOverride = null;
                UpdateRowHeaderIndicators();
                return;
            }

            // Keep last anchor in sync with current cell or last selected item
            var currentCell = ResultsDataGrid.CurrentCell;
            var currentItem = currentCell.Item;
            if (currentItem != null)
            {
                var idx = ResultsDataGrid.Items.IndexOf(currentItem);
                if (idx >= 0) _lastAnchorRow = idx;
            }
            else if (ResultsDataGrid.SelectedItems.Count > 0)
            {
                var last = ResultsDataGrid.SelectedItems[ResultsDataGrid.SelectedItems.Count - 1];
                var idx = ResultsDataGrid.Items.IndexOf(last);
                if (idx >= 0) _lastAnchorRow = idx;
            }

            UpdateRowHeaderIndicators();
        }

        private void HandleRowMouseClick(int index, bool shift, bool ctrl)
        {
            // Remember the explicit mouse-clicked row so SelectionChanged will respect it.
            _lastClickedIndexOverride = index;

            if (shift)
            {
                if (_lastAnchorRow < 0) _lastAnchorRow = index;
                var start = Math.Min(_lastAnchorRow, index);
                var end = Math.Max(_lastAnchorRow, index);

                ResultsDataGrid.SelectedItems.Clear();
                for (int i = start; i <= end; i++)
                {
                    var it = ResultsDataGrid.Items[i];
                    if (it == CollectionView.NewItemPlaceholder) continue;
                    ResultsDataGrid.SelectedItems.Add(it);
                }

                // Ensure the indicator is placed on the clicked row (the target of the shift selection)
                _lastAnchorRow = index;
            }
            else if (ctrl)
            {
                var item = ResultsDataGrid.Items[index];
                if (ResultsDataGrid.SelectedItems.Contains(item))
                    ResultsDataGrid.SelectedItems.Remove(item);
                else
                    ResultsDataGrid.SelectedItems.Add(item);

                _lastAnchorRow = index;
            }
            else
            {
                ResultsDataGrid.SelectedItems.Clear();
                var item = ResultsDataGrid.Items[index];
                ResultsDataGrid.SelectedItems.Add(item);
                _lastAnchorRow = index;
            }

            UpdateRowHeaderIndicators();
        }

        private void UpdateRowHeaderIndicators()
        {
            for (int i = 0; i < ResultsDataGrid.Items.Count; i++)
            {
                var row = ResultsDataGrid.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row == null) continue;
                var indicator = row.Tag as TextBlock;
                if (indicator != null)
                {
                    // Use Hidden so the left space for the arrow is preserved and numbers don't shift
                    indicator.Visibility = (i == _lastAnchorRow) ? Visibility.Visible : Visibility.Hidden;
                }
                else if (row.Header is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is TextBlock tb)
                {
                    tb.Visibility = (i == _lastAnchorRow) ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        private void AdjustColumnsToContent()
        {
            // Ensure columns can be resized by user
            ResultsDataGrid.CanUserResizeColumns = true;

            // First set columns to size to cells so WPF measures them based on content
            foreach (var col in ResultsDataGrid.Columns)
            {
                try { col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells); } catch { }
            }

            // Run after layout so ActualWidth is available
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var col in ResultsDataGrid.Columns)
                {
                    try
                    {
                        var w = Math.Min(col.ActualWidth, 400.0);
                        // Fix pixel width but allow users to resize later
                        col.Width = new DataGridLength(w, DataGridLengthUnitType.Pixel);
                        col.CanUserResize = true;
                    }
                    catch { }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
        {
            var current = child;
            while (current != null)
            {
                if (current is T typed) return typed;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

    }
}