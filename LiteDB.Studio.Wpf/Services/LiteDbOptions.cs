namespace LiteDB.Studio.Wpf.Services;

public class LiteDbOptions
{
    public const int DefaultMaxRows = 1000;

    /// <summary>Maximum number of rows returned by a single query. Defaults to 1000.</summary>
    public int MaxRows { get; set; } = DefaultMaxRows;
}
