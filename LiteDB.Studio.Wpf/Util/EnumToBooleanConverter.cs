using System;
using System.Globalization;
using System.Windows.Data;

namespace LiteDB.Studio.Wpf.Util
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || value == null) return false;
            var param = parameter?.ToString();
            if (string.IsNullOrEmpty(param)) return false;
            if (!Enum.IsDefined(value.GetType(), param)) return false;
            try
            {
                var enumValue = Enum.Parse(value.GetType(), param);
                return enumValue.Equals(value);
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) return Binding.DoNothing;
            if (!(value is bool b) || !b) return Binding.DoNothing;
            var param = parameter?.ToString();
            if (string.IsNullOrEmpty(param)) return Binding.DoNothing;
            try
            {
                return Enum.Parse(targetType, param);
            }
            catch
            {
                return Binding.DoNothing;
            }
        }
    }
}
