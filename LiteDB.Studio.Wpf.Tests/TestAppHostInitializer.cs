using System.Reflection;
using LiteDB.Studio.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LiteDB.Studio.Wpf.Tests;

internal static class TestAppHostInitializer
{
    private static bool _initialized;

    internal static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ILogger>(new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .CreateLogger());
            })
            .Build();

        SetAppHost(host);
        _initialized = true;
    }

    private static void SetAppHost(IHost host)
    {
        PropertyInfo? appHostProperty = typeof(LiteDbStudioApplication)
            .GetProperty("AppHost", BindingFlags.Public | BindingFlags.Static);
        appHostProperty?.SetValue(null, host);
    }
}
