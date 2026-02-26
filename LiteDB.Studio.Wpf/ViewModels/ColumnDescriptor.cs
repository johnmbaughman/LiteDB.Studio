namespace LiteDB.Studio.Wpf.ViewModels;

/// <summary>Describes a grid column for display in the result grid.</summary>
/// <param name="Name">The field name used for data binding.</param>
/// <param name="Header">The column header text shown in the UI.</param>
/// <param name="IsEditable">Whether the column's cells can be edited by the user.</param>
public sealed record ColumnDescriptor(string Name, string Header, bool IsEditable = false);
