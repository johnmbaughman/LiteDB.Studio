namespace LiteDB.Studio.Wpf.Services;

/// <summary>Configuration options for <see cref="LiteDbService"/>.</summary>
public class LiteDbOptions
{
    /// <summary>The default maximum number of rows returned by a single query.</summary>
    public const int DEFAULT_MAX_ROWS = 1000;

    /// <summary>Maximum number of rows returned by a single query. Defaults to <see cref="DEFAULT_MAX_ROWS"/>.</summary>
    public int MaxRows { get; set; } = DEFAULT_MAX_ROWS;
}
