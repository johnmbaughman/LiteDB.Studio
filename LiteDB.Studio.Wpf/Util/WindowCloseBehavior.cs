using System.Windows;

namespace LiteDB.Studio.Wpf.Util;

public static class WindowCloseBehavior
{
    public static readonly DependencyProperty CloseTriggerProperty =
        DependencyProperty.RegisterAttached("CloseTrigger", typeof(bool?), typeof(WindowCloseBehavior),
            new PropertyMetadata(null, OnCloseTriggerChanged));

    public static bool? GetCloseTrigger(DependencyObject obj)
    {
        return (bool?)obj.GetValue(CloseTriggerProperty);
    }

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