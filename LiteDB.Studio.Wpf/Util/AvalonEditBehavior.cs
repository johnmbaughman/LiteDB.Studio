using System.Windows;
using ICSharpCode.AvalonEdit;

namespace LiteDB.Studio.Wpf.Util
{
    public static class AvalonEditHelper
    {
        public static readonly DependencyProperty BoundTextProperty =
            DependencyProperty.RegisterAttached("BoundText", typeof(string), typeof(AvalonEditHelper),
                new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundTextChanged));

        public static string GetBoundText(DependencyObject obj)
        {
            return (string)obj.GetValue(BoundTextProperty);
        }

        public static void SetBoundText(DependencyObject obj, string value)
        {
            obj.SetValue(BoundTextProperty, value);
        }

        private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                editor.TextChanged -= OnEditorTextChanged;
                if (e.NewValue is string text && editor.Text != text)
                {
                    editor.Text = text;
                }
                editor.TextChanged += OnEditorTextChanged;
            }
        }

        private static void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (sender is TextEditor editor)
            {
                // Update the bound property to trigger two-way binding
                SetBoundText(editor, editor.Text);
            }
        }
    }
}