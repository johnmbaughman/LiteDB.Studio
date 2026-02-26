using LiteDB.Studio.Wpf.Services;
using System.Windows;

namespace LiteDB.Studio.Wpf.Controls;

public partial class ResultTextView
{
    public static readonly DependencyProperty QueryResultProperty =
        DependencyProperty.Register(nameof(QueryResult), typeof(QueryResult), typeof(ResultTextView),
            new PropertyMetadata(null, OnQueryResultChanged));

    public QueryResult? QueryResult
    {
        get => (QueryResult?)GetValue(QueryResultProperty);
        set => SetValue(QueryResultProperty, value);
    }

    public ResultTextView()
    {
        InitializeComponent();
    }

    private static void OnQueryResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResultTextView control)
        {
            control.UpdateJsonDisplay();
        }
    }

    private void UpdateJsonDisplay()
    {
        if (QueryResult?.Rows == null)
        {
            JsonEditor.Text = string.Empty;
            return;
        }

        // Materialize rows to allow multiple passes
        var rows = QueryResult.Rows.ToList();
        var sb = new System.Text.StringBuilder();

        // Header comment with row count (matches WinForms style)
        sb.AppendLine($"/* {rows.Count} */");

        if (rows.Count == 0)
        {
            // nothing else to show
            JsonEditor.Text = sb.ToString();
            return;
        }

        if (rows.Count == 1)
        {
            // Single document: show just the document (pretty-printed) on following lines
            var row = rows[0];
            var body = FormatRow(row);
            sb.AppendLine(body);
            JsonEditor.Text = sb.ToString();
            return;
        }

        // Multiple documents: show as JSON array with indentation and commas
        sb.AppendLine("[");
        for (var i = 0; i < rows.Count; i++)
        {
            var body = FormatRow(rows[i]);
            // indent each line of the document body
            var indented = IndentLines(body, "  ");
            sb.Append(indented);
            if (i < rows.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("]");

        JsonEditor.Text = sb.ToString();
    }

    private static string FormatRow(object? row)
    {
        switch (row)
        {
            case null:
                return "null";
            case BsonDocument doc:
                try
                {
                    Dictionary<string, object?> obj = ConvertBsonDocument(doc);
                    var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    return System.Text.Json.JsonSerializer.Serialize(obj, opts);
                }
                catch
                {
                    // fallback to existing behaviour
                    return doc.ToString();
                }

            default:
                // Fallback to ToString
                return row.ToString() ?? "null";
        }
    }

    private static object? ConvertBsonValue(BsonValue? value)
    {
        if (value == null || value.IsNull) { return null; }

        switch (value.Type)
        {
            case BsonType.Document:
                return ConvertBsonDocument(value.AsDocument);
            case BsonType.Array:
                var list = value.AsArray.Select(ConvertBsonValue).ToList();
                return list;
            case BsonType.String:
                return value.AsString;
            case BsonType.Boolean:
                return value.AsBoolean;
            case BsonType.Int32:
                return value.AsInt32;
            case BsonType.Int64:
                return value.AsInt64;
            case BsonType.Double:
                return value.AsDouble;
            case BsonType.Decimal:
                return value.AsDecimal;
            case BsonType.DateTime:
                // ISO 8601 format
                return value.AsDateTime.ToString("o");
            case BsonType.ObjectId:
                return value.AsObjectId.ToString();
            case BsonType.Guid:
                return value.AsGuid.ToString();
            case BsonType.Binary:
                return Convert.ToBase64String(value.AsBinary);
            case BsonType.Null:
                return null;
            case BsonType.MinValue:
            case BsonType.MaxValue:
            default:
                // default to RawValue or ToString
                return value.RawValue?.ToString();
        }
    }

    private static Dictionary<string, object?> ConvertBsonDocument(BsonDocument doc)
    {
        var dict = new Dictionary<string, object?>();
        foreach (KeyValuePair<string, BsonValue> kv in doc)
        {
            dict[kv.Key] = ConvertBsonValue(kv.Value);
        }
        return dict;
    }

    private static string IndentLines(string text, string indent)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = indent + lines[i];
        }

        return string.Join(Environment.NewLine, lines);
    }
}
