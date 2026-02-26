using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LiteDB.Studio.Wpf.ViewModels;

/// <summary>ViewModel for the LiteDB page-dump debugger window.</summary>
public partial class DebuggerViewModel : ObservableObject, IDisposable
{
    private readonly IDatabaseService _dbService;
    private readonly ILogger<DebuggerViewModel> _logger;
    private DatabaseDebuggerService? _debugger;

    /// <summary>Gets or sets a value indicating whether the debugger HTTP server is running.</summary>
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>Gets or sets the TCP port the debugger HTTP server is listening on.</summary>
    [ObservableProperty]
    private int _port;

    /// <summary>Command that starts the debugger server. Enabled only when connected and not already running.</summary>
    public IAsyncRelayCommand StartCommand { get; }
    /// <summary>Command that stops the debugger server.</summary>
    public IRelayCommand StopCommand { get; }
    /// <summary>Command that opens the default browser pointing at the debugger URL.</summary>
    public IRelayCommand OpenBrowserCommand { get; }

    /// <summary>Initializes a new instance of <see cref="DebuggerViewModel"/>.</summary>
    /// <param name="dbService">The database service whose underlying database is inspected.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DebuggerViewModel(IDatabaseService dbService, ILogger<DebuggerViewModel> logger)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning && _dbService.IsConnected);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        OpenBrowserCommand = new RelayCommand(OpenBrowser, () => IsRunning);

        _dbService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    partial void OnIsRunningChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        OpenBrowserCommand.NotifyCanExecuteChanged();
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_dbService.Database is not LiteDatabase db)
        {
            _logger.LogWarning("Cannot start debugger: no connected LiteDatabase");
            return;
        }

        try
        {
            var port = Random.Shared.Next(8000, 9000);
            _debugger = new DatabaseDebuggerService();
            await _debugger.StartAsync(db, port);
            Port = port;
            IsRunning = true;
            _logger.LogInformation("Debugger started on port {Port}", port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start debugger");
            _debugger?.Dispose();
            _debugger = null;
        }
    }

    private void Stop()
    {
        try
        {
            _debugger?.Stop();
            _debugger?.Dispose();
            _debugger = null;
            IsRunning = false;
            _logger.LogInformation("Debugger stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop debugger");
        }
    }

    private void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo($"http://localhost:{Port}") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open browser for debugger on port {Port}", Port);
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        StartCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Stops the debugger server and releases all resources.</summary>
    public void Dispose()
    {
        _dbService.ConnectionStateChanged -= OnConnectionStateChanged;
        _debugger?.Dispose();
        _debugger = null;
        GC.SuppressFinalize(this);
    }
}
