using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace LiteDB.Studio.Wpf.Util
{
    // Trims long paths to a balanced display similar to WinForms BalanceString
    public class PathTrimmerConverter : IValueConverter
    {
        public int StartCounter { get; set; } = 30;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var path = value as string;
            if (string.IsNullOrEmpty(path)) return string.Empty;

            try
            {
                var fileName = Path.GetFileName(path);
                var dir = Path.GetDirectoryName(path) ?? string.Empty;

                if (StartCounter > dir.Length + 3) return path;

                var prefix = dir.Substring(0, Math.Max(0, StartCounter - 3));
                return $"{prefix}...\\{fileName}";
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
