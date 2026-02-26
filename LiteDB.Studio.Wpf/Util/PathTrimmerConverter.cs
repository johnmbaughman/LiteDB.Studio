using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace LiteDB.Studio.Wpf.Util;

/// <summary>
/// WPF value converter that trims long file paths to a compact form (e.g. <c>C:\User...\file.db</c>).
/// </summary>
public class PathTrimmerConverter : IValueConverter
{
    /// <summary>Gets or sets the maximum number of characters shown from the directory portion. Default is 30.</summary>
    public int StartCounter { get; set; } = 30;

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) {
            return string.Empty;
        }

        var path = value as string;
        if (string.IsNullOrEmpty(path)) {
            return string.Empty;
        }

        try
        {
            var fileName = Path.GetFileName(path);
            var dir = Path.GetDirectoryName(path) ?? string.Empty;

            if (StartCounter > dir.Length + 3) {
                return path;
            }

            var prefix = dir[..Math.Max(0, StartCounter - 3)];
            return $"{prefix}...\\{fileName}";
        }
        catch
        {
            return value;
        }
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
