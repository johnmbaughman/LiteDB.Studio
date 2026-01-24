using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Windows.Media;

namespace LiteDB.Studio.Wpf.Services
{
    /// <summary>
    /// A lightweight AvalonEdit completion item for SQL keywords and collection names.
    /// Implements <see cref="ICompletionData"/> and replaces the completion segment with the provided text.
    /// </summary>
    public class SqlCompletionProvider : ICompletionData
    {
        public SqlCompletionProvider(string text, string? description = null, object? tag = null)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Description = description;
            Tag = tag;
        }

        /// <summary>
        /// Optional image for the completion entry (not used by default).
        /// </summary>
        public ImageSource? Image => null;

        /// <summary>
        /// The text to insert when the completion is applied.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The content shown in the completion list. Defaults to <see cref="Text"/>.
        /// </summary>
        public object Content => Text;

        /// <summary>
        /// A short description shown in the completion tooltip/details.
        /// </summary>
        public object? Description { get; }
        
        /// <summary>
        /// Optional tag carried with this completion item (e.g., kind, metadata).
        /// </summary>
        public object? Tag { get; }

        /// <summary>
        /// Priority of the completion item (0 = default). Higher values are shown first.
        /// </summary>
        public double Priority => 0;

        /// <summary>
        /// Replaces the completion segment in the editor with <see cref="Text"/>.
        /// </summary>
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            if (textArea == null) throw new ArgumentNullException(nameof(textArea));

            var doc = textArea.Document ?? throw new InvalidOperationException("TextArea has no document");
            doc.Replace(completionSegment.Offset, completionSegment.Length, Text);
        }

        /// <summary>
        /// Helper factory for creating keyword completion entries.
        /// </summary>
        public static SqlCompletionProvider FromKeyword(string keyword)
            => new SqlCompletionProvider(keyword.ToUpperInvariant(), description: $"SQL keyword: {keyword.ToUpperInvariant()}");

        /// <summary>
        /// Helper factory for creating collection name completion entries.
        /// </summary>
        public static SqlCompletionProvider FromCollection(string collectionName)
            => new SqlCompletionProvider(collectionName, description: $"Collection: {collectionName}", tag: "collection");

        /// <summary>
        /// Fetches collection names from the database service and returns them as completion items.
        /// </summary>
        public static async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<SqlCompletionProvider>>
            GetCollectionCompletionsAsync(IDatabaseService databaseService, System.Threading.CancellationToken cancellationToken = default)
        {
            if (databaseService == null) throw new System.ArgumentNullException(nameof(databaseService));

            var names = await databaseService.GetCollectionNamesAsync(cancellationToken).ConfigureAwait(false);
            var list = new System.Collections.Generic.List<SqlCompletionProvider>();

            if (names != null)
            {
                foreach (var n in names)
                {
                    if (string.IsNullOrWhiteSpace(n)) continue;
                    list.Add(FromCollection(n));
                }
            }

            return list;
        }

        private const string DefaultKeywordsRelativePath = "Resources/sql_keywords.json";
        private static readonly string[] DefaultKeywords = new[]
        {
            "SELECT", "FROM", "WHERE", "GROUP", "BY", "ORDER", "INSERT", "UPDATE", "DELETE", "VALUES",
            "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN", "AS", "TOP", "SET", "DROP", "CREATE", "INDEX",
            "BEGIN", "COMMIT", "ROLLBACK", "CHECKPOINT"
        };

        /// <summary>
        /// Loads keyword strings from a JSON file and returns completion items.
        /// The file path is configurable; if <paramref name="filePath"/> is null, the default resource file
        /// at 'Resources/sql_keywords.json' (copied to output) is used.
        /// </summary>
        public static async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<SqlCompletionProvider>>
            GetKeywordCompletionsFromFileAsync(string? filePath = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var path = filePath;
            var customProvided = !string.IsNullOrWhiteSpace(filePath);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = System.IO.Path.Combine(System.AppContext.BaseDirectory, DefaultKeywordsRelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            }

            var list = new System.Collections.Generic.List<SqlCompletionProvider>();

            string[]? keywords = null;
            if (System.IO.File.Exists(path))
            {
                using var stream = System.IO.File.OpenRead(path);
                keywords = await System.Text.Json.JsonSerializer.DeserializeAsync<string[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else if (customProvided)
            {
                // If a specific file path was provided but the file does not exist, return empty result (caller explicitly asked for this file)
                return list;
            }

            keywords ??= DefaultKeywords;

            foreach (var kw in keywords)
            {
                if (string.IsNullOrWhiteSpace(kw)) continue;
                list.Add(FromKeyword(kw.Trim()));
            }

            return list;
        }
    }
}
