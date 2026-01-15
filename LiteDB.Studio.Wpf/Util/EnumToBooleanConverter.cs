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
            string param = parameter.ToString();
            if (Enum.IsDefined(value.GetType(), value) == false) return false;
            var enumValue = Enum.Parse(value.GetType(), param);
            return enumValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) return Binding.DoNothing;
            if ((bool)value)
            {
                return Enum.Parse(targetType, parameter.ToString());
            }
            return Binding.DoNothing;
        }
    }
}
