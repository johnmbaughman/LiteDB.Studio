namespace LiteDB.Studio.Wpf.Services;

/// <summary>Encapsulates the result of a query executed against the database.</summary>
public class QueryResult
{
    // Rows are represented as a collection of LiteDB.BsonDocument or generic objects
    // Use object here to avoid adding a direct dependency; concrete implementation can use BsonDocument

    /// <summary>Gets or sets the result rows. Each element is typically a <c>BsonDocument</c>.</summary>
    public IEnumerable<object> Rows { get; set; } = [];

    /// <summary>Gets or sets the column descriptors inferred from the first result row.</summary>
    public IReadOnlyList<ColumnInfo> Columns { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the result was truncated by the row limit.</summary>
    public bool LimitExceeded { get; set; }

    /// <summary>Gets or sets the number of rows returned (before any limit truncation).</summary>
    public int RowCount { get; set; }

    /// <summary>Gets or sets the time taken to execute the query.</summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>Gets or sets advisory warnings produced during query execution.</summary>
    public IReadOnlyList<string> Warnings { get; set; } = [];

    /// <summary>Gets or sets arbitrary key/value metadata attached to the result.</summary>
    public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
}
