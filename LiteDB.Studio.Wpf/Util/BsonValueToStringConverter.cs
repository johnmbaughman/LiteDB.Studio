using System;
using System.Globalization;
using System.Windows.Data;
using LiteDB;
using System.Text.Json;

namespace LiteDB.Studio.Wpf.Util
{
    public class BsonValueToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            if (value is BsonValue bson)
            {
                switch (bson.Type)
                {
                    case BsonType.MinValue:
                        return "-∞";
                    case BsonType.MaxValue:
                        return "+∞";
                    case BsonType.Boolean:
                        return bson.AsBoolean.ToString().ToLower();
                    case BsonType.DateTime:
                        return bson.AsDateTime.ToString();
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
                    case BsonType.ObjectId:
                    case BsonType.Guid:
                        return bson.ToString();
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
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
