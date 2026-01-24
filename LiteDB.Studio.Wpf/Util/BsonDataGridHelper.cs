using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Serilog;

namespace LiteDB.Studio.Wpf.Util;

public static class BsonDataGridHelper
{
    public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
        "Enable",
        typeof(bool),
        typeof(BsonDataGridHelper),
        new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) return;
        if ((bool)e.NewValue)
        {
            grid.AutoGeneratingColumn += Grid_AutoGeneratingColumn;
            grid.LoadingRow += Grid_LoadingRow;
        }
        else
        {
            grid.AutoGeneratingColumn -= Grid_AutoGeneratingColumn;
            grid.LoadingRow -= Grid_LoadingRow;
        }
    }

    private static void Grid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        try
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
        catch(Exception ex)
        {
            Log.Error(ex, "Error occurred while loading row: {Message}", ex.Message);
        }
    }

    private static void Grid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        try
        {
            // Apply BsonValue string converter to all text columns so BsonValue types render properly
            if (e.Column is not DataGridTextColumn textColumn) return;
            if (textColumn.Binding is Binding binding)
            {
                binding.Converter = new BsonValueToStringConverter();
            }
        }
        catch(Exception ex)
        {
            Log.Error(ex, "Error occurred while auto-generating column: {Message}", ex.Message);
        }
    }
}