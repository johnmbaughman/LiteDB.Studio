namespace LiteDB.Studio.Wpf.Services;

public interface IEditorAdapter
{
    string Text { get; set; }
    int CaretOffset { get; }
    (int Line, int Column) GetCaretLineColumn();
    void Select(int offset, int length);
    void ShowCompletion(IEnumerable<CompletionItem> items);
    event EventHandler? CaretPositionChanged;
    event EventHandler? TextChanged;
}