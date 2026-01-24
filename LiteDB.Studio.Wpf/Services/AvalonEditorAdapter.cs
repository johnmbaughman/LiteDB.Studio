using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Threading;

namespace LiteDB.Studio.Wpf.Services;

public class AvalonEditorAdapter : IEditorAdapter
{
    private readonly TextEditor _editor;

    public AvalonEditorAdapter(TextEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _editor.TextChanged += (_, _) => TextChanged?.Invoke(this, EventArgs.Empty);
        _editor.TextArea.Caret.PositionChanged += (_, _) => CaretPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Text
    {
        get => _editor.Text ?? string.Empty;
        set => _editor.Dispatcher.Invoke(() => _editor.Text = value, DispatcherPriority.Send);
    }

    public int CaretOffset => _editor.TextArea.Caret.Offset;

    public (int Line, int Column) GetCaretLineColumn()
    {
        var c = _editor.TextArea.Caret;
        return (c.Line, c.Column);
    }

    public void Select(int offset, int length)
    {
        _editor.Select(offset, length);
    }

    public event EventHandler? CaretPositionChanged;
    public event EventHandler? TextChanged;

    public void ShowCompletion(IEnumerable<CompletionItem> items)
    {
        var window = new CompletionWindow(_editor.TextArea);
        var data = window.CompletionList.CompletionData;
        foreach (var it in items)
        {
            data.Add(new SimpleCompletionData(it.Text, it.Description));
        }
        window.Show();
    }

    public void Focus()
    {
        _editor.Focus();
    }

    private class SimpleCompletionData(string text, string? description) : ICompletionData
    {
        public System.Windows.Media.ImageSource? Image => null;
        public string Text { get; } = text;
        public object Content => Text;
        public object? Description { get; } = description;

        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment.Offset, completionSegment.Length, Text);
        }
    }
}