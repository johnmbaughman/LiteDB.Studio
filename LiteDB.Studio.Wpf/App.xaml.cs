using System.Windows;
using LiteDB.Studio.Mvvm.Hosting;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LiteDB.Studio.Wpf;

public partial class App
{
    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureLogging()
            .ConfigureUi<MainWindow, MainViewModel>()
            .ConfigureServices((context, services) =>
            {
                services.Configure<LiteDbOptions>(context.Configuration.GetSection("LiteDb"));
                services.AddSingleton<IDatabaseService, LiteDbService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IConnectionManagerDialogService, ConnectionManagerDialogService>();
                services.AddSingleton<IAppSettingsService, AppSettingsService>();
                services.AddSingleton<DatabaseTreeViewModel>();
                services.AddSingleton<DatabaseTreeView>();
                services.AddSingleton<DebuggerViewModel>();
                services.AddSingleton<DebuggerView>();
                services.AddTransient<ConnectionManagerViewModel>();
                services.AddTransient<ConnectionManagerWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Await host startup to ensure services are ready before showing the UI.
        await StartApplicationAsync("LiteDB.Studio.Wpf", e);
        ShowMainWindow();
    }
    
    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Application shutting down");

            await AppHost.StopAsync();

            if (AppHost is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                AppHost.Dispose();
            }

            base.OnExit(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to exit cleanly: {Message}", ex.Message);
        }
    }
}
