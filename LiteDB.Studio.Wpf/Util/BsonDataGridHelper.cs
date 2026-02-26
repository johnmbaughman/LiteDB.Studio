using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Serilog;

namespace LiteDB.Studio.Wpf.Util;

/// <summary>
/// Attached behavior that wires up a <see cref="DataGrid"/> to render <see cref="BsonValue"/> cells correctly,
/// including row-number headers and automatic column converter attachment.
/// </summary>
public static class BsonDataGridHelper
{
    /// <summary>Attached property that enables BsonValue-aware column generation for a <see cref="DataGrid"/>.</summary>
    public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
        "Enable",
        typeof(bool),
        typeof(BsonDataGridHelper),
        new PropertyMetadata(false, OnEnableChanged));

    /// <summary>Sets the <see cref="EnableProperty"/> attached property value on <paramref name="element"/>.</summary>
    /// <param name="element">Target dependency object.</param>
    /// <param name="value">Value to set.</param>
    public static void SetEnable(DependencyObject element, bool value)
    {
        element.SetValue(EnableProperty, value);
    }

    /// <summary>Gets the <see cref="EnableProperty"/> attached property value from <paramref name="element"/>.</summary>
    /// <param name="element">Target dependency object.</param>
    public static bool GetEnable(DependencyObject element)
    {
        return (bool)element.GetValue(EnableProperty);
    }

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) {
            return;
        }

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
            if (e.Column is not DataGridTextColumn textColumn) {
                return;
            }

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
