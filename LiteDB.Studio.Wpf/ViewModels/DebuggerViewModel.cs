using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LiteDB.Studio.Wpf.ViewModels;

public partial class DebuggerViewModel : ObservableObject, IDisposable
{
    private readonly IDatabaseService _dbService;
    private readonly ILogger<DebuggerViewModel> _logger;
    private DatabaseDebuggerService? _debugger;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _port;

    public IAsyncRelayCommand StartCommand { get; }
    public IRelayCommand StopCommand { get; }
    public IRelayCommand OpenBrowserCommand { get; }

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

    public void Dispose()
    {
        _dbService.ConnectionStateChanged -= OnConnectionStateChanged;
        _debugger?.Dispose();
        _debugger = null;
        GC.SuppressFinalize(this);
    }
}
