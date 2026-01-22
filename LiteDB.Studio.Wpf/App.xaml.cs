using System;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using LiteDB.Studio.Wpf.Util;
using Serilog;
using System.IO;

namespace LiteDB.Studio.Wpf
{
    public partial class App : Application
    {
        public IHost? HostInstance { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            Logging.Configure();

            // Add global exception handlers
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Log.Information("Application starting");

            base.OnStartup(e);

            HostInstance = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register ViewModels and services here
                    services.AddSingleton<LiteDB.Studio.Wpf.ViewModels.MainViewModel>();
                    services.AddSingleton<LiteDB.Studio.Wpf.Services.IDatabaseService, LiteDB.Studio.Wpf.Services.LiteDbService>();
                    services.AddTransient<LiteDB.Studio.Wpf.ViewModels.ResultGridViewModel>();
                })
                .Build();

            await HostInstance.StartAsync();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "Unhandled exception in UI thread: {Message}", e.Exception.Message);
            // Optionally set e.Handled = true to prevent app crash, but for unhandled, let it crash
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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
            Log.Information("Application shutting down");

            if (HostInstance != null)
            {
                await HostInstance.StopAsync();
                HostInstance.Dispose();
            }

            base.OnExit(e);
        }
    }
}
