namespace LiteDB.Studio.Wpf.Services;

/// <summary>Describes a single column (field) returned by a query or inferred from a collection schema.</summary>
public class ColumnInfo
{
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the BSON type name (e.g. <c>"String"</c>, <c>"Int32"</c>).</summary>
    public string BsonType { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional display-format hint for the UI (e.g. a date format string).</summary>
    public string? DisplayFormat { get; set; }
}
