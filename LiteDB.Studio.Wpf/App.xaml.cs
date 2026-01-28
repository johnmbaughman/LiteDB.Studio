using System.Windows;
using LiteDB.Studio.Mvvm;
using LiteDB.Studio.Mvvm.Hosting;
using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LiteDB.Studio.Wpf;

public partial class App : LiteDbStudioApplication
{
    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureLogging()
            .ConfigureUi<MainWindow, MainViewModel>()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IDatabaseService, LiteDbService>();
                services.AddSingleton<DatabaseTreeViewModel>();
                services.AddSingleton<DatabaseTreeView>();
                services.AddTransient<ConnectionManagerViewModel>();
                services.AddTransient<ConnectionManagerWindow>();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        StartApplication("LiteDB.Studio.Wpf", e);
        ShowMainWindow();
    }

    private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled exception in UI thread: {Message}", e.Exception.Message);
        // Optionally set e.Handled = true to prevent app crash, but for unhandled, let it crash
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception in background thread: {Message}", ex.Message);
        }
        else
        {
            Log.Fatal("Unhandled exception in background thread: {ExceptionObject}", e.ExceptionObject);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Application shutting down");

            if (AppHost != null)
            {
                await AppHost.StopAsync();
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
