namespace LiteDB.Studio.Wpf.Services;

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string BsonType { get; set; } = string.Empty;
    public string? DisplayFormat { get; set; }
}