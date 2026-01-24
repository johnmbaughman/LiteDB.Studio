using System;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Document;
using LiteDB.Studio.Wpf.Controls;

namespace LiteDB.Studio.Wpf.Util
{
    /// <summary>
    /// Lightweight compatibility shim for older code that used the legacy AvalonEdit helper.
    /// New code should use <see cref="LiteDB.Studio.Wpf.Controls.AvalonEditBehaviors"/> and bind commands/properties in XAML/ViewModels.
    /// This shim delegates to the new behaviors and exposes a fallback completion command when no ViewModel command is present.
    /// </summary>
    [Obsolete("Use LiteDB.Studio.Wpf.Controls.AvalonEditBehaviors for MVVM bindings and completion. This shim maintains backward compatibility.")]
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
            // Delegate to the new AvalonEditBehaviors.EditorText property to preserve two-way behavior and remove duplicated handler logic.
            if (d is TextEditor editor)
            {
                try
                {
                    var newText = e.NewValue as string ?? string.Empty;
                    AvalonEditBehaviors.SetEditorText(editor, newText);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "OnBoundTextChanged shim failed");
                }
            }
        }

        /// <summary>
        /// Enable or disable completion behavior. This now delegates to setting a fallback ShowCompletionCommand on the editor
        /// only when no ViewModel-provided command exists. This avoids duplicating input handlers and centralizes completion triggers.
        /// </summary>
        public static readonly DependencyProperty EnableCompletionProperty =
            DependencyProperty.RegisterAttached("EnableCompletion", typeof(bool), typeof(AvalonEditHelper), new PropertyMetadata(true, OnEnableCompletionChanged));

        public static bool GetEnableCompletion(DependencyObject obj) => (bool)obj.GetValue(EnableCompletionProperty);
        public static void SetEnableCompletion(DependencyObject obj, bool value) => obj.SetValue(EnableCompletionProperty, value);

        private static void OnEnableCompletionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                var attach = (bool)e.NewValue;
                try
                {
                    if (attach)
                    {
                        // If a ViewModel is already providing a ShowCompletionCommand via the new behaviors, prefer that. Otherwise, provide a fallback command
                        // that executes the legacy TryShowCompletion logic so completion still works without new wiring.
                        var current = AvalonEditBehaviors.GetShowCompletionCommand(editor);
                        if (current == null)
                        {
                            AvalonEditBehaviors.SetShowCompletionCommand(editor, new SimpleAsyncCommand(async () => await TryShowCompletion(editor?.TextArea, "Fallback")));
                            Serilog.Log.Debug("EnableCompletion shim attached fallback ShowCompletionCommand");
                        }
                    }
                    else
                    {
                        // Only clear fallback commands that we installed (don't remove user-provided commands)
                        var current = AvalonEditBehaviors.GetShowCompletionCommand(editor);
                        if (current is SimpleAsyncCommand)
                        {
                            AvalonEditBehaviors.SetShowCompletionCommand(editor, null);
                            Serilog.Log.Debug("EnableCompletion shim removed fallback ShowCompletionCommand");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "OnEnableCompletionChanged shim failed");
                }
            }
        }

        private static async Task<bool> TryShowCompletion(TextArea? textArea, string triggerName)
        {
            if (textArea?.Document == null)
            {
                Serilog.Log.Information("TryShowCompletion: textArea or document null");
                return false;
            }

            var editor = textArea.TextView.Services.GetService(typeof(TextEditor)) as TextEditor ?? textArea.Parent as TextEditor;

            System.Collections.Generic.List<Services.CompletionItem> items = new System.Collections.Generic.List<Services.CompletionItem>();

            var kws = await Services.SqlCompletionProvider.GetKeywordCompletionsFromFileAsync(null);
            var kwCount = 0;
            if (kws != null)
            {
                foreach (var k in kws)
                {
                    items.Add(new Services.CompletionItem(k.Text, k.Description?.ToString(), k.Tag));
                    kwCount++;
                }
            }

            var dataContext = (editor as FrameworkElement)?.DataContext;
            var colCount = 0;
            if (dataContext is ViewModels.TabViewModel tvm)
            {
                var cols = await tvm.GetCollectionCompletionsAsync(System.Threading.CancellationToken.None);
                if (cols != null)
                {
                    foreach (var c in cols)
                    {
                        items.Add(new Services.CompletionItem(c.Text, c.Description, c.Tag));
                        colCount++;
                    }
                }
            }

            Serilog.Log.Debug("{Trigger} pressed - EditorFound: {EditorFound}, KeywordCount: {KwCount}, CollectionCount: {ColCount}, TotalItems: {Total}", triggerName, editor != null, kwCount, colCount, items.Count);

            if (items.Count > 0)
            {
                if (editor != null)
                {
                    var adapter = new Services.AvalonEditorAdapter(editor);
                    adapter.ShowCompletion(items);
                    Serilog.Log.Debug("Completion shown via TextEditor adapter (items: {Count})", items.Count);
                }
                else
                {
                    var window = new CompletionWindow(textArea);
                    var data = window.CompletionList.CompletionData;
                    foreach (var it in items)
                    {
                        data.Add(new SimpleCompletionData(it.Text, it.Description));
                    }
                    window.Show();
                    Serilog.Log.Debug("Completion shown via TextArea fallback (items: {Count})", items.Count);
                }

                return true;
            }

            Serilog.Log.Debug("No completion items available for {Trigger}", triggerName);
            return false;
        }

        private class SimpleCompletionData : ICompletionData
        {
            public SimpleCompletionData(string text, string? description)
            {
                Text = text;
                Description = description;
            }

            public System.Windows.Media.ImageSource? Image => null;
            public string Text { get; }
            public object Content => Text;
            public object? Description { get; }

            public double Priority => 0;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment.Offset, completionSegment.Length, Text);
            }
        }

        private class SimpleAsyncCommand : System.Windows.Input.ICommand
        {
            private readonly Func<System.Threading.Tasks.Task> _action;
            public SimpleAsyncCommand(Func<System.Threading.Tasks.Task> action) => _action = action;
            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => true;
            public async void Execute(object? parameter) => await _action();
        }
    }
}