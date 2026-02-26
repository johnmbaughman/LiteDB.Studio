using System.Globalization;
using System.Windows.Data;

namespace LiteDB.Studio.Wpf.Util;

/// <summary>
/// WPF value converter that converts an enum value to <see cref="bool"/> for radio-button bindings.
/// The converter parameter must be the string name of the enum member to match.
/// </summary>
public class EnumToBooleanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || value == null) {
            return false;
        }

        var param = parameter?.ToString();
        if (string.IsNullOrEmpty(param)) {
            return false;
        }

        if (!Enum.IsDefined(value.GetType(), param)) {
            return false;
        }

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

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || value is not bool b || !b) {
            return Binding.DoNothing;
        }

        var param = parameter.ToString();
        if (string.IsNullOrEmpty(param)) {
            return Binding.DoNothing;
        }

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
