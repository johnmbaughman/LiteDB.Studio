using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LiteDB.Studio.Wpf.Behaviors;

/// <summary>
/// Attached behavior that registers and applies the bundled SQL syntax-highlighting definition
/// to AvalonEdit <see cref="TextEditor"/> controls.
/// </summary>
public static class SqlHighlightingBehavior
{
    private static readonly Lock _sync = new();
    private static bool _registered;

    /// <summary>
    /// Attached property. Set to <c>true</c> on a <see cref="TextEditor"/> or parent <see cref="FrameworkElement"/>
    /// to enable SQL syntax highlighting for all descendant editors.
    /// </summary>
    public static readonly DependencyProperty EnableSqlHighlightingProperty = DependencyProperty.RegisterAttached(
        "EnableSqlHighlighting",
        typeof(bool),
        typeof(SqlHighlightingBehavior),
        new PropertyMetadata(false, OnEnableSqlHighlightingChanged));

    /// <summary>Gets the <see cref="EnableSqlHighlightingProperty"/> value from <paramref name="obj"/>.</summary>
    /// <param name="obj">Target dependency object.</param>
    public static bool GetEnableSqlHighlighting(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableSqlHighlightingProperty);
    }

    /// <summary>Sets the <see cref="EnableSqlHighlightingProperty"/> value on <paramref name="obj"/>.</summary>
    /// <param name="obj">Target dependency object.</param>
    /// <param name="value">Value to set.</param>
    public static void SetEnableSqlHighlighting(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableSqlHighlightingProperty, value);
    }

    private static void OnEnableSqlHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor editor)
        {
            if (e.NewValue is true)
            {
                editor.Loaded += OnEditorLoaded;
                ApplyHighlightingToEditor(editor);
            }
            else
            {
                editor.Loaded -= OnEditorLoaded;
            }

            return;
        }

        if (d is not FrameworkElement element)
        {
            return;
        }

        if (e.NewValue is true)
        {
            element.Loaded += OnElementLoaded;
        }
        else
        {
            element.Loaded -= OnElementLoaded;
        }
    }

    private static void OnEditorLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        ApplyHighlightingToEditor(editor);
    }

    private static void OnElementLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        RegisterSqlHighlighting();
        ApplyHighlightingToEditors(element);
    }

    private static void RegisterSqlHighlighting()
    {
        lock (_sync)
        {
            if (_registered)
            {
                return;
            }

            Assembly asm = typeof(SqlHighlightingBehavior).Assembly;
            var resourceName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("SQL-Mode.xshd", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
            {
                return;
            }

            using Stream? stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return;
            }

            using var reader = new XmlTextReader(stream);
            XshdSyntaxDefinition? xshd = HighlightingLoader.LoadXshd(reader);
            IHighlightingDefinition? highlight = HighlightingLoader.Load(xshd, HighlightingManager.Instance);

            if (HighlightingManager.Instance.GetDefinition("SQL") == null)
            {
                HighlightingManager.Instance.RegisterHighlighting("SQL", [".sql"], highlight);
            }

            _registered = true;
        }
    }

    private static void ApplyHighlightingToEditors(DependencyObject root)
    {
        IHighlightingDefinition? def = HighlightingManager.Instance.GetDefinition("SQL");
        if (def == null)
        {
            return;
        }

        foreach (TextEditor editor in FindVisualChildren<TextEditor>(root))
        {
            editor.SyntaxHighlighting = def;
        }
    }

    private static void ApplyHighlightingToEditor(TextEditor editor)
    {
        RegisterSqlHighlighting();

        IHighlightingDefinition? def = HighlightingManager.Instance.GetDefinition("SQL");
        if (def == null)
        {
            return;
        }

        editor.SyntaxHighlighting = def;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
    {
        if (depObj == null)
        {
            yield break;
        }

        var count = VisualTreeHelper.GetChildrenCount(depObj);
        for (var i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T t)
            {
                yield return t;
            }

            foreach (T childOfChild in FindVisualChildren<T>(child))
            {
                yield return childOfChild;
            }
        }
    }
}
