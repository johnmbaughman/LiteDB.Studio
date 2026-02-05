using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class TabViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _filename;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _editorText = string.Empty;

    [ObservableProperty]
    private int _caretOffset;

    [ObservableProperty]
    private int _selectionStart;

    [ObservableProperty]
    private int _selectionLength;

    [ObservableProperty]
    private QueryResult? _lastResult;

    [ObservableProperty]
    private string? _lastError;

    [ObservableProperty]
    private bool _isResultLoaded;

    [ObservableProperty]
    private bool _isPlus;

    [ObservableProperty]
    private ResultGridViewModel _resultGridViewModel;

    [ObservableProperty]
    private int _selectedResultTabIndex;

    [ObservableProperty]
    private IEnumerable<CompletionItem>? _lastCompletions;

    public TabViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        RunCommand = new AsyncRelayCommand(ExecuteRunAsync);
        ShowCompletionCommand = new AsyncRelayCommand(ExecuteShowCompletionAsync);
        ResultGridViewModel = new ResultGridViewModel(_databaseService);
        CloseCommand = new RelayCommand(ExecuteClose);
    }

    public IAsyncRelayCommand RunCommand { get; }
    public IAsyncRelayCommand ShowCompletionCommand { get; }
    public IRelayCommand CloseCommand { get; }

    private async Task ExecuteRunAsync(CancellationToken cancellationToken)
    {
        LastError = null;
        LastResult = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query =
                // Execute selection
                SelectionLength > 0 ? EditorText.Substring(SelectionStart, SelectionLength) :
                // Execute entire buffer
                EditorText;

            Serilog.Log.Information("Executing query from Tab '{Title}' (len={Len})", Title, query.Length);
            QueryResult result = await _databaseService.ExecuteAsync(query, cancellationToken);
            LastResult = result;
            IsResultLoaded = true;
            SelectedResultTabIndex = 0; // show Grid tab when results are available
            Serilog.Log.Information("Query executed - Rows: {Count}, Columns: {Cols}", result.RowCount, result.Columns.Count);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Query execution failed in Tab '{Title}'", Title);
            LastError = ex.Message;
            IsResultLoaded = false;
        }
    }

    private void ExecuteClose()
    {
        // TODO: Implement save prompt if IsModified
        // For now, just mark as not modified to allow close
        IsModified = false;
    }

    partial void OnLastResultChanged(QueryResult? value)
    {
        ResultGridViewModel.QueryResult = value;
    }

    /// <summary>
    /// Returns collection completion items by querying the live database service.
    /// </summary>
    public async Task<IEnumerable<CompletionItem>> GetCollectionCompletionsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<CompletionItem>();
        try
        {
            IEnumerable<SqlCompletionProvider> cols = await SqlCompletionProvider.GetCollectionCompletionsAsync(_databaseService, cancellationToken);
            list.AddRange(cols.Select(c => new CompletionItem(c.Text, c.Description?.ToString(), c.Tag)));
        }
        catch
        {
            // ignore collection completion errors
        }

        return list;
    }

    private async Task ExecuteShowCompletionAsync(CancellationToken cancellationToken)
    {
        try
        {
            LastCompletions = await GetCollectionCompletionsAsync(cancellationToken);
        }
        catch
        {
            LastCompletions = [];
        }
    }
}
