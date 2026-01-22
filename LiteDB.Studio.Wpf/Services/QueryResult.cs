using System;
using System.Collections.Generic;

namespace LiteDB.Studio.Wpf.Services
{
    public class QueryResult
    {
        // Rows are represented as a collection of LiteDB.BsonDocument or generic objects
        // Use object here to avoid adding a direct dependency; concrete implementation can use BsonDocument
        public IEnumerable<object> Rows { get; set; } = Array.Empty<object>();

        public IReadOnlyList<ColumnInfo> Columns { get; set; } = Array.Empty<ColumnInfo>();

        public bool LimitExceeded { get; set; }

        public int RowCount { get; set; }

        public TimeSpan ExecutionTime { get; set; }

        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
    }
}
