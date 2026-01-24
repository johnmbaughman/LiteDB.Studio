using System.Windows;
using System.Windows.Controls;

namespace LiteDB.Studio.Wpf.Util;

public static class PasswordBoxBehavior
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    public static string GetBoundPassword(DependencyObject d)
    {
        return (string)d.GetValue(BoundPasswordProperty);
    }

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