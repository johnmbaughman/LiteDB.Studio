using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Studio.Wpf.ViewModels
{
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

        public TabViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            RunCommand = new AsyncRelayCommand(ExecuteRunAsync);
            ResultGridViewModel = new ResultGridViewModel(_databaseService);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        public IAsyncRelayCommand RunCommand { get; }
        public IRelayCommand CloseCommand { get; }

        private async Task ExecuteRunAsync()
        {
            LastError = null;
            LastResult = null;

            try
            {
                string query;
                if (SelectionLength > 0)
                {
                    // Execute selection
                    query = EditorText.Substring(SelectionStart, SelectionLength);
                }
                else
                {
                    // Execute entire buffer
                    query = EditorText;
                }

                var result = await _databaseService.ExecuteAsync(query, CancellationToken.None);
                LastResult = result;
                IsResultLoaded = true;
            }
            catch (Exception ex)
            {
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
    }
}
