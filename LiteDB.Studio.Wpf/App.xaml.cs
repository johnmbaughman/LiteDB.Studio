using System.Windows;
using LiteDB.Studio.Mvvm.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using LiteDB.Studio.Wpf.Util;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Views;
using Serilog;

namespace LiteDB.Studio.Wpf;

public partial class App
{
    public IHost? HostInstance { get; private set; }

    public App()
    {
        //try {
        //    Logging.Configure();
        AppHost = new HostBuilder()
            .ConfigureUi<MainWindow, MainViewModel>()
            .Build();

        //    // Add global exception handlers
        //    DispatcherUnhandledException += App_DispatcherUnhandledException;
        //    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        //    Log.Information("Application starting");

        //    base.OnStartup(e);

        //    HostInstance = Host.CreateDefaultBuilder()
        //        .ConfigureServices((_, services) => {
        //            // Register views here
        //            services.AddSingleton<Views.MainWindow>();

        //            // Register ViewModels and services here
        //            services.AddSingleton<ViewModels.MainViewModel>();
        //            services.AddSingleton<Services.IDatabaseService, Services.LiteDbService>();
        //            services.AddTransient<ViewModels.ResultGridViewModel>();
        //        })
        //        .Build();

        //await HostInstance.StartAsync();
        //}
        //catch (Exception ex) {
        //    Log.Fatal(ex, "Application failed to start: {Message}", ex.Message);
        //}
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

            if (HostInstance != null)
            {
                await HostInstance.StopAsync();
                HostInstance.Dispose();
            }

            base.OnExit(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to exit cleanly: {Message}", ex.Message);
        }
    }
}
