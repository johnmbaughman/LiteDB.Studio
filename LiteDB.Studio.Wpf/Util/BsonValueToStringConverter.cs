using System.Globalization;
using System.Windows.Data;

namespace LiteDB.Studio.Wpf.Util;

/// <summary>
/// WPF value converter that formats a <see cref="BsonValue"/> (or plain object) as a human-readable string for grid cells.
/// Pass the converter parameter <c>"full"</c> to disable string truncation.
/// </summary>
public class BsonValueToStringConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isFull = parameter?.ToString() == "full";

        switch (value)
        {
            case null:
                return string.Empty;
            case BsonValue bson:
                switch (bson.Type)
                {
                    case BsonType.MinValue:
                        return "-∞";
                    case BsonType.MaxValue:
                        return "+∞";
                    case BsonType.Boolean:
                        return bson.AsBoolean.ToString().ToLower();
                    case BsonType.DateTime:
                        return bson.AsDateTime.ToString(CultureInfo.CurrentCulture);
                    case BsonType.Null:
                        return "(null)";
                    case BsonType.Binary:
                        return System.Convert.ToBase64String(bson.AsBinary);
                    case BsonType.Int32:
                    case BsonType.Int64:
                    case BsonType.Double:
                    case BsonType.Decimal:
                        return bson.RawValue?.ToString() ?? string.Empty;
                    case BsonType.String:
                        var str = bson.AsString ?? string.Empty;
                        if (!isFull && str.Length > 100)
                        {
                            str = str[..97] + "...";
                        }

                        // Escape double-quotes and newlines and wrap in quotes for grid readability
                        var escaped = str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
                        return $"\"{escaped}\"";
                    case BsonType.ObjectId:
                    case BsonType.Guid:
                        return bson.ToString();
                    case BsonType.Document:
                    case BsonType.Array:
                    default:
                        try
                        {
                            return JsonSerializer.Serialize(bson);
                        }
                        catch
                        {
                            return bson.ToString();
                        }
                }

            default:
                return value.ToString() ?? string.Empty;
        }
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
