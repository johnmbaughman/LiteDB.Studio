namespace LiteDB.Studio.Wpf.Services;

/// <summary>A lightweight completion item returned by <see cref="TabViewModel.GetCollectionCompletionsAsync"/>.</summary>
/// <param name="Text">The text to insert when the item is applied.</param>
/// <param name="Description">Optional description shown in the completion tooltip.</param>
/// <param name="Tag">Optional metadata tag (e.g. <c>"collection"</c>).</param>
public record CompletionItem(string Text, string? Description = null, object? Tag = null);
