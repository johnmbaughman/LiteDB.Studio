using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using LiteDB.Studio.Wpf.ViewModels;
using Serilog;

namespace LiteDB.Studio.Wpf.Controls;

public partial class ResultGrid
{
    public ResultGrid()
    {
        InitializeComponent();
        ResultsDataGrid.CellEditEnding += ResultsDataGrid_CellEditEnding;
        ResultsDataGrid.LoadingRow += ResultsDataGrid_LoadingRow;
        ResultsDataGrid.Sorting += ResultsDataGrid_Sorting;
        ResultsDataGrid.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(ResultsDataGrid_RowHeaderMouseLeftButtonDown), true);
        // Handle clicks on the row body to support Ctrl+Click and Shift+Click consistently
        ResultsDataGrid.PreviewMouseLeftButtonDown += ResultsDataGrid_PreviewMouseLeftButtonDown;
        ResultsDataGrid.SelectionChanged += ResultsDataGrid_SelectionChanged;
        DataContextChanged += ResultGrid_DataContextChanged;
    }

    public static readonly DependencyProperty QueryResultProperty =
        DependencyProperty.Register(nameof(QueryResult), typeof(Services.QueryResult), typeof(ResultGrid), new PropertyMetadata(null, OnQueryResultChanged));

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

    private ResultGridViewModel? _subscribedViewModel;
    private NotifyCollectionChangedEventHandler? _columnsChangedHandler;
    private PropertyChangedEventHandler? _propertyChangedHandler;

    private void ResultGrid_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        // Unsubscribe old ViewModel handlers to prevent leaks and stale updates
        if (_subscribedViewModel != null)
        {
            if (_columnsChangedHandler != null)
            {
                _subscribedViewModel.Columns.CollectionChanged -= _columnsChangedHandler;
            }

            if (_propertyChangedHandler != null)
            {
                _subscribedViewModel.PropertyChanged -= _propertyChangedHandler;
            }

            _subscribedViewModel = null;
        }

        if (DataContext is not ResultGridViewModel viewModel)
        {
            ResultsDataGrid.ItemsSource = null;
            return;
        }

        _subscribedViewModel = viewModel;

        UpdateColumnsFromViewModel(viewModel);

        // Reset ItemsSource to the new ViewModel's current state (null for a fresh tab)
        ResultsDataGrid.ItemsSource = viewModel.QueryResult?.Rows;

        _columnsChangedHandler = (_, _) => UpdateColumnsFromViewModel(viewModel);
        _propertyChangedHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(ResultGridViewModel.QueryResult))
            {
                ResultsDataGrid.ItemsSource = viewModel.QueryResult?.Rows;
            }
        };

        viewModel.Columns.CollectionChanged += _columnsChangedHandler;
        viewModel.PropertyChanged += _propertyChangedHandler;
    }

    private void UpdateColumnsFromViewModel(ResultGridViewModel viewModel)
    {
        ResultsDataGrid.Columns.Clear();
        _columnNames.Clear();

        var converter = new Util.BsonValueToStringConverter();

        foreach (ColumnDescriptor desc in viewModel.Columns)
        {
            var binding = new Binding($"[{desc.Name}]") { Converter = converter };
            var tooltipBinding = new Binding($"[{desc.Name}]") { Converter = converter, ConverterParameter = "full" };

            var dataGridColumn = new DataGridTextColumn
            {
                Header = desc.Header,
                Binding = binding
            };

            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(ToolTipProperty, tooltipBinding));
            // Slightly larger padding for improved readability
            style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(3,3,3,3)));
            // Ensure long text is trimmed with an ellipsis when cell width is constrained
            style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.NoWrap));
            dataGridColumn.ElementStyle = style;

            // Allow user resizing per column
            dataGridColumn.CanUserResize = true;

            // Ensure header displays raw column name (preserve underscores and exact text)
            // Avoid using raw string as Header to prevent access-key underscore processing
            if (dataGridColumn.Header is string headerText)
            {
                dataGridColumn.Header = new TextBlock { Text = headerText };
            }

            ResultsDataGrid.Columns.Add(dataGridColumn);

            // store the logical column name for sorting and header restoration
            // If column header was replaced with TextBlock above, use its Text property
            _columnNames[dataGridColumn] = (dataGridColumn.Header is TextBlock tb) ? tb.Text : (dataGridColumn.Header?.ToString() ?? string.Empty);

            // Measure header text and set MinWidth so the column cannot be narrower than the header
            try
            {
                var header = _columnNames[dataGridColumn];
                DpiScale dpi = VisualTreeHelper.GetDpi(this);
                var typeface = new Typeface(ResultsDataGrid.FontFamily, ResultsDataGrid.FontStyle, ResultsDataGrid.FontWeight, ResultsDataGrid.FontStretch);
                var ft = new FormattedText(header, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, ResultsDataGrid.FontSize, Brushes.Black, dpi.PixelsPerDip);
                // Add padding allowance to account for cell padding and sort glyphs
                dataGridColumn.MinWidth = Math.Ceiling(ft.Width) + 24.0;
            }
            catch
            {
                // ignore measurement errors and retain default MinWidth
            }
        }

        // Adjust column widths based on content (similar to WinForms behaviour):
        // 1) Size to cells so we compute a suitable width, 2) fix the width capped at 400px,
        // and leave the column resizable for users.
        AdjustColumnsToContent();
    }

    private void ResultsDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        // Display 1-based row numbers in the row header and attach an indicator element
        try
        {
            var index = e.Row.GetIndex();

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            // Reserve a fixed small width for the indicator so numbers don't shift when it appears.
            var indicator = new TextBlock
            {
                Text = "▶",
                Visibility = Visibility.Hidden,
                Foreground = Brushes.Blue,
                Width = 12,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            var number = new TextBlock
            {
                Text = (index + 1).ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };

            sp.Children.Add(indicator);
            sp.Children.Add(number);

            e.Row.Header = sp;

            // Store the indicator for quick access
            e.Row.Tag = indicator;

            // Show indicator if this row is the last anchor (use Hidden for layout reservation)
            indicator.Visibility = (index == _lastAnchorRow) ? Visibility.Visible : Visibility.Hidden;
        }
        catch(Exception ex)
        {
            Log.Error(ex, "Error loading row {RowIndex}", e.Row.GetIndex());
        }
    }

    private async void ResultsDataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        try
        {
            if (DataContext is not ResultGridViewModel viewModel) {
                return;
            }

            if (e.EditAction != DataGridEditAction.Commit) {
                return;
            }

            var row = e.Row.Item;
            var column = e.Column as DataGridTextColumn;
            if (column == null) {
                return;
            }

            var textBox = e.EditingElement as TextBox;
            if (textBox == null) {
                return;
            }

            var newValue = textBox.Text;
            var columnName = column.Header?.ToString() ?? string.Empty;

            // Parse the new value to appropriate type
            var parsedValue = ParseValue(newValue);

            await viewModel.UpdateCellValueAsync(row, columnName, parsedValue, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating cell value: {Message}", ex.Message);
        }
    }

    private static object ParseValue(string value)
    {
        // Simple parsing, can be improved
        if (int.TryParse(value, out var i)) {
            return i;
        }

        if (long.TryParse(value, out var l)) {
            return l;
        }

        if (double.TryParse(value, out var d)) {
            return d;
        }

        if (bool.TryParse(value, out var b)) {
            return b;
        }

        return value;
    }

    private void ResultsDataGrid_Sorting(object? sender, DataGridSortingEventArgs e)
    {
        // Handle sorting manually because rows are BsonDocument and column bindings use indexer syntax
        e.Handled = true;

        DataGridColumn column = e.Column;
        var columnName = _columnNames.TryGetValue(column, out var nm) ? nm : column.Header?.ToString();
        if (string.IsNullOrEmpty(columnName)) {
            return;
        }

        // Toggle sort direction
        ListSortDirection direction = column.SortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

        // Clear other column sort indicators and headers
        foreach (DataGridColumn? col in ResultsDataGrid.Columns)
        {
            if (col == column) { continue; }

            col.SortDirection = null;
            // restore header text from stored mapping
            if (_columnNames.TryGetValue(col, out var name)) {
                col.Header = name;
            }
        }

        column.SortDirection = direction;

        // update header to show green arrow
        UpdateColumnHeaderWithSortIndicator(column, direction);

        var view = CollectionViewSource.GetDefaultView(ResultsDataGrid.ItemsSource) as ListCollectionView;
        if (view == null) {
            return;
        }

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

    private class BsonColumnComparer(string columnName, ListSortDirection direction) : System.Collections.IComparer
    {
        public int Compare(object? x, object? y)
        {
            // Handle nulls
            if (ReferenceEquals(x, y)) {
                return 0;
            }

            if (x is null) {
                return direction == ListSortDirection.Ascending ? -1 : 1;
            }

            if (y is null) {
                return direction == ListSortDirection.Ascending ? 1 : -1;
            }

            // Expect rows to be BsonDocument or objects that can be treated like BsonDocument
            var docX = x as BsonDocument;
            var docY = y as BsonDocument;

            BsonValue valX = BsonValue.Null;
            BsonValue valY = BsonValue.Null;

            try
            {
                if (docX != null) {
                    valX = docX[columnName];
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting value from document X: {Message}", ex.Message);
            }

            try
            {
                if (docY != null) {
                    valY = docY[columnName];
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting value from document Y: {Message}", ex.Message);
            }

            // Use BsonValue.CompareTo which handles types
            var result = valX.CompareTo(valY);

            return direction == ListSortDirection.Ascending ? result : -result;
        }
    }

    private int _lastAnchorRow = -1;
    // Temporarily store the index of the row that was last clicked by the user so SelectionChanged can prefer it.
    private int? _lastClickedIndexOverride;
    private readonly Dictionary<DataGridColumn, string> _columnNames = new();

    private void ResultsDataGrid_RowHeaderMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        // Disable row resizing entirely: only intercept Thumb instances that belong to a DataGridRowHeader.
        Thumb? possibleThumb = FindAncestor<Thumb>(e.OriginalSource as DependencyObject);
        if (possibleThumb != null && FindAncestor<DataGridRowHeader>(possibleThumb) != null)
        {
            // If the Thumb is part of the row header template, block it so rows can't be resized.
            e.Handled = true;
            return;
        }

        // Detect the row header that was clicked
        DataGridRowHeader? header = FindAncestor<DataGridRowHeader>(e.OriginalSource as DependencyObject);
        if (header == null) {
            return;
        }

        var item = header.DataContext;
        var index = ResultsDataGrid.Items.IndexOf(item);
        if (index < 0) {
            return;
        }

        var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        var ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        HandleRowMouseClick(index, shift, ctrl);

        e.Handled = true;
    }

    private void ResultsDataGrid_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        // Disable row resizing entirely: only block Thumb interactions that are part of the row header template.
        Thumb? possibleThumb = FindAncestor<Thumb>(e.OriginalSource as DependencyObject);
        if (possibleThumb != null && FindAncestor<DataGridRowHeader>(possibleThumb) != null)
        {
            e.Handled = true;
            return;
        }

        // If the click is on the row header (e.g. the number text), handle it here so clicking the number works
        DataGridRowHeader? header = FindAncestor<DataGridRowHeader>(e.OriginalSource as DependencyObject);
        if (header != null)
        {
            var item = header.DataContext;
            var index = ResultsDataGrid.Items.IndexOf(item);
            if (index < 0) {
                return;
            }

            var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            var ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            HandleRowMouseClick(index, shift, ctrl);
            e.Handled = true;
            return;
        }

        DataGridRow? row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row == null) {
            return;
        }

        var index2 = ResultsDataGrid.Items.IndexOf(row.Item);
        if (index2 < 0) {
            return;
        }

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
        DataGridCellInfo currentCell = ResultsDataGrid.CurrentCell;
        var currentItem = currentCell.Item;
        if (currentItem != null)
        {
            var idx = ResultsDataGrid.Items.IndexOf(currentItem);
            if (idx >= 0) {
                _lastAnchorRow = idx;
            }
        }
        else if (ResultsDataGrid.SelectedItems.Count > 0)
        {
            var last = ResultsDataGrid.SelectedItems[^1];
            if (last != null)
            {
                var idx = ResultsDataGrid.Items.IndexOf(last);
                if (idx >= 0) {
                    _lastAnchorRow = idx;
                }
            }
        }

        UpdateRowHeaderIndicators();
    }

    private void HandleRowMouseClick(int index, bool shift, bool ctrl)
    {
        // Remember the explicit mouse-clicked row so SelectionChanged will respect it.
        _lastClickedIndexOverride = index;

        if (shift)
        {
            if (_lastAnchorRow < 0) {
                _lastAnchorRow = index;
            }

            var start = Math.Min(_lastAnchorRow, index);
            var end = Math.Max(_lastAnchorRow, index);

            ResultsDataGrid.SelectedItems.Clear();
            for (var i = start; i <= end; i++)
            {
                var it = ResultsDataGrid.Items[i];
                if (it == CollectionView.NewItemPlaceholder) {
                    continue;
                }

                ResultsDataGrid.SelectedItems.Add(it);
            }
        }
        else if (ctrl)
        {
            var item = ResultsDataGrid.Items[index];
            if (ResultsDataGrid.SelectedItems.Contains(item)) {
                ResultsDataGrid.SelectedItems.Remove(item);
            }
            else {
                ResultsDataGrid.SelectedItems.Add(item);
            }
        }
        else
        {
            ResultsDataGrid.SelectedItems.Clear();
            var item = ResultsDataGrid.Items[index];
            ResultsDataGrid.SelectedItems.Add(item);
        }

        // Ensure the indicator is placed on the clicked row (the target of the shift selection)
        _lastAnchorRow = index;

        UpdateRowHeaderIndicators();
    }

    private void UpdateRowHeaderIndicators()
    {
        for (var i = 0; i < ResultsDataGrid.Items.Count; i++)
        {
            var row = ResultsDataGrid.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
            if (row == null) {
                continue;
            }

            if (row.Tag is TextBlock indicator)
            {
                // Use Hidden so the left space for the arrow is preserved and numbers don't shift
                indicator.Visibility = (i == _lastAnchorRow) ? Visibility.Visible : Visibility.Hidden;
            }
            else if (row.Header is StackPanel { Children.Count: > 0 } sp && sp.Children[0] is TextBlock tb)
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
        foreach (DataGridColumn? col in ResultsDataGrid.Columns)
        {
            try
            {
                col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while setting column width to SizeToCells: {Message}", ex.Message);
            }
        }

        // Run after layout so ActualWidth is available
        Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (DataGridColumn? col in ResultsDataGrid.Columns)
            {
                try
                {
                    var measured = Math.Min(col.ActualWidth, 400.0);
                    // Fix pixel width but allow users to resize later
                    var finalWidth = Math.Max(measured, col.MinWidth);
                            col.Width = new DataGridLength(finalWidth, DataGridLengthUnitType.Pixel);
                    col.CanUserResize = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception occurred while adjusting column widths: {Message}", ex.Message);
                }
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        DependencyObject? current = child;
        while (current != null)
        {
            if (current is T typed) {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

}
