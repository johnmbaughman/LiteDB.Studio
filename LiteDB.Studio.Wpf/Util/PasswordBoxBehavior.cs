using System.Windows;
using System.Windows.Controls;

namespace LiteDB.Studio.Wpf.Util;

/// <summary>
/// Attached behavior that enables two-way binding of a <see cref="PasswordBox"/>'s password
/// without exposing the plain text through the visual tree.
/// </summary>
public static class PasswordBoxBehavior
{
    /// <summary>Attached property that bi-directionally synchronises with <see cref="PasswordBox.Password"/>.</summary>
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    /// <summary>Gets the <see cref="BoundPasswordProperty"/> value from <paramref name="d"/>.</summary>
    /// <param name="d">Target dependency object.</param>
    public static string GetBoundPassword(DependencyObject d)
    {
        return (string)d.GetValue(BoundPasswordProperty);
    }

    /// <summary>Sets the <see cref="BoundPasswordProperty"/> value on <paramref name="d"/>.</summary>
    /// <param name="d">Target dependency object.</param>
    /// <param name="value">Value to set.</param>
    public static void SetBoundPassword(DependencyObject d, string value)
    {
        d.SetValue(BoundPasswordProperty, value);
    }

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox) { return; }

        passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
        if (!string.IsNullOrEmpty((string)e.NewValue))
        {
            passwordBox.Password = (string)e.NewValue;
        }
        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
    }

    private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            SetBoundPassword(passwordBox, passwordBox.Password);
        }
    }
}
