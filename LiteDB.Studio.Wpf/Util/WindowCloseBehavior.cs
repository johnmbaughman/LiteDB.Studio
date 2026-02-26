using System.Windows;

namespace LiteDB.Studio.Wpf.Util;

/// <summary>
/// Attached behavior that closes a <see cref="Window"/> and sets its <c>DialogResult</c> when a
/// bound boolean property changes. Replaces code-behind close logic with pure MVVM.
/// </summary>
public static class WindowCloseBehavior
{
    /// <summary>
    /// Attached property. When set to <c>true</c> the window's <c>DialogResult</c> is set to <c>true</c> and it closes;
    /// when set to <c>false</c> the <c>DialogResult</c> is set to <c>false</c> and it closes.
    /// </summary>
    public static readonly DependencyProperty CloseTriggerProperty =
        DependencyProperty.RegisterAttached("CloseTrigger", typeof(bool?), typeof(WindowCloseBehavior),
            new PropertyMetadata(null, OnCloseTriggerChanged));

    /// <summary>Gets the <see cref="CloseTriggerProperty"/> value from <paramref name="obj"/>.</summary>
    /// <param name="obj">Target dependency object.</param>
    public static bool? GetCloseTrigger(DependencyObject obj)
    {
        return (bool?)obj.GetValue(CloseTriggerProperty);
    }

    /// <summary>Sets the <see cref="CloseTriggerProperty"/> value on <paramref name="obj"/>.</summary>
    /// <param name="obj">Target dependency object.</param>
    /// <param name="value">Value to set.</param>
    public static void SetCloseTrigger(DependencyObject obj, bool? value)
    {
        obj.SetValue(CloseTriggerProperty, value);
    }

    private static void OnCloseTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window || e.NewValue is not bool result) {
            return;
        }

        window.DialogResult = result;
        window.Close();
    }
}
