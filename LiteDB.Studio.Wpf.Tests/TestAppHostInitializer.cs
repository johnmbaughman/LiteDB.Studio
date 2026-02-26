using System.Reflection;
using LiteDB.Studio.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LiteDB.Studio.Wpf.Tests;

/// <summary>
/// Ensures <see cref="LiteDbStudioApplication.AppHost"/> is initialised once for the test process,
/// satisfying any code paths that resolve services from the host.
/// </summary>
internal static class TestAppHostInitializer
{
    private static bool _initialized;

    /// <summary>Initialises the application host if it has not already been set up.</summary>
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
