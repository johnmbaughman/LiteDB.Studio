using System.IO;
using System.Runtime;
using LiteDB.Studio.Mvvm.Properties;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views.Shell;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace LiteDB.Studio.Mvvm.Hosting;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Registers MVVM UI services for the application, including ShellView, ShellViewModel,
    /// ShellContentView, and ShellContentViewModel.
    /// <para>
    /// Shell services are registered as singletons.
    /// Throws <see cref="AmbiguousImplementationException"/> if any service is already registered.
    /// </para>
    /// </summary>
    /// <typeparam name="TV">Shell content view type implementing <see cref="IShellContentView"/>.</typeparam>
    /// <typeparam name="TVm">Shell content view model type implementing <see cref="IShellContentViewModel"/>.</typeparam>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <returns>The configured <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureUi<TV, TVm>(this IHostBuilder hostBuilder)
        where TV : class, IShellContentView
        where TVm : class, IShellContentViewModel {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        hostBuilder.ConfigureServices((_, services) => {
            if (services.Any(sd => sd.ServiceType == typeof(IShellView))
                || services.Any(sd => sd.ServiceType == typeof(IShellViewModel))
                || services.Any(sd => sd.ServiceType == typeof(IShellContentView))
                || services.Any(sd => sd.ServiceType == typeof(IShellContentViewModel))) {
                throw new AmbiguousImplementationException(Resources.UiAlreadyInitialzed);
            }

            services.AddViewFactory();

            services.AddSingleton<ShellView>();
            services.AddSingleton<IShellView, ShellView>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<IShellViewModel, ShellViewModel>();

            services.AddSingleton<TV>();
            services.AddSingleton<IShellContentView, TV>();
            services.AddSingleton<TVm>();
            services.AddSingleton<IShellContentViewModel, TVm>();

            services.AddSingleton(new ViewRegistration(typeof(ShellView), typeof(IShellViewModel)));
            services.AddSingleton(new ViewRegistration(typeof(TV), typeof(IShellContentViewModel)));
        });

        return hostBuilder;
    }

    /// <summary>
    /// Configures Serilog-based logging for the host builder.
    /// Initializes the <see cref="Log.Logger"/> instance from configuration and registers it as a singleton.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureLogging((context, logBuilder) =>
        {
            // Build logger from configuration first
            //    var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(context.Configuration);

            //    // Ensure file sink writes to a temp folder under the user's temp path for easier cleanup.
            //    try
            //    {
            //        var tempDir = Path.Combine(Path.GetTempPath(), "MaiBookmarks");
            //        Directory.CreateDirectory(tempDir);
            //        var tempFile = Path.Combine(tempDir, "MaiBookmarks-.log");
            //        // Add a file sink that rolls daily and uses a consistent template. Adding this here
            //        // ensures file logs land in the user's temp folder regardless of appsettings.json path.
            //        loggerConfig = loggerConfig.WriteTo.File(
            //            path: tempFile,
            //            rollingInterval: Serilog.RollingInterval.Day,
            //            outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss} {Level}] : {SourceContext} : {Message:lj} {NewLine}{Exception}{NewLine}");
            //    }
            //    catch
            //    {
            //        // Swallow any IO errors configuring the temp folder - logging should not crash the app.
            //    }

            //    Log.Logger = loggerConfig.CreateLogger();
            //    logBuilder.AddSerilog(Log.Logger, dispose: true);
            //    logBuilder.Services.AddLogging();
            //});

            //return hostBuilder.ConfigureServices((_, services) =>
            //{
            //    services.AddSingleton(Log.Logger);
            //});
            var basePath = AppContext.BaseDirectory;
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot tempConfig = builder.Build();

            // compute default file path under user profile temp folder
            var defaultLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "LiteDB.Studio", "log-.txt");
            var overrides = new Dictionary<string, string>();

            var writeToChildren = tempConfig.GetSection("Serilog:WriteTo").GetChildren().ToList();
            for (var i = 0; i < writeToChildren.Count; i++)
            {
                var path = writeToChildren[i].GetValue<string>("Args:path");
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var expanded = Environment.ExpandEnvironmentVariables(path);
                if (expanded != path)
                {
                    overrides[$"Serilog:WriteTo:{i}:Args:path"] = expanded;
                }

                var dir = Path.GetDirectoryName(expanded);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            var hasFileSink = writeToChildren.Any(x => string.Equals(x.GetValue<string>("Name"), "File", StringComparison.OrdinalIgnoreCase));
            if (!hasFileSink)
            {
                var idx = writeToChildren.Count;
                overrides[$"Serilog:WriteTo:{idx}:Name"] = "File";
                overrides[$"Serilog:WriteTo:{idx}:Args:path"] = defaultLogPath;
                overrides[$"Serilog:WriteTo:{idx}:Args:rollingInterval"] = "Day";
                overrides[$"Serilog:WriteTo:{idx}:Args:retainedFileCountLimit"] = "30";
                overrides[$"Serilog:WriteTo:{idx}:Args:outputTemplate"] = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

                var dir = Path.GetDirectoryName(defaultLogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            if (overrides.Any())
            {
                builder.AddInMemoryCollection(overrides.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));
            }

            IConfigurationRoot configuration = builder.Build();

            LoggerConfiguration loggerConfig = new LoggerConfiguration().Enrich.FromLogContext();

            if (configuration.GetSection("Serilog").Exists())
            {
                // apply configuration from appsettings.json (and overrides)
                loggerConfig = loggerConfig.ReadFrom.Configuration(configuration);

                // ensure a sane default minimum level if none specified in config
                var hasMin = !string.IsNullOrEmpty(configuration["Serilog:MinimumLevel:Default"]) || !string.IsNullOrEmpty(configuration["Logging:LogLevel:Default"]);
                if (!hasMin)
                {
                    loggerConfig = loggerConfig.MinimumLevel.Is(LogEventLevel.Information);
                }
            }
            else
            {
                // No Serilog section — use defaults (console, debug, file)
                loggerConfig = loggerConfig.MinimumLevel.Is(LogEventLevel.Information)
                    .WriteTo.Console()
                    .WriteTo.Debug()
                    .WriteTo.File(defaultLogPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            Log.Logger = loggerConfig.CreateLogger();
        });

        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton(Log.Logger);
        });
    }
}
