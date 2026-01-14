using System;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LiteDB.Studio.Wpf
{
    public partial class App : Application
    {
        public IHost? HostInstance { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            HostInstance = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register ViewModels and services here
                    services.AddSingleton<LiteDB.Studio.Wpf.ViewModels.MainViewModel>();
                    services.AddSingleton<LiteDB.Studio.Wpf.Services.IDatabaseService, LiteDB.Studio.Wpf.Services.LiteDbService>();
                })
                .Build();

            await HostInstance.StartAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (HostInstance != null)
            {
                await HostInstance.StopAsync();
                HostInstance.Dispose();
            }

            base.OnExit(e);
        }
    }
}
