using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace LiteDB.Studio.Wpf.Util;

public static class Logging
{
    public static void Configure()
    {
        var basePath = AppContext.BaseDirectory;
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var tempConfig = builder.Build();

        // compute default file path under user profile temp folder
        var defaultLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "LiteDB.Studio", "log-.txt");
        var overrides = new Dictionary<string, string>();

        var writeToChildren = tempConfig.GetSection("Serilog:WriteTo").GetChildren().ToList();
        for (var i = 0; i < writeToChildren.Count; i++)
        {
            var path = writeToChildren[i].GetValue<string>("Args:path");
            if (string.IsNullOrEmpty(path)) continue;
            var expanded = Environment.ExpandEnvironmentVariables(path);
            if (expanded != path) overrides[$"Serilog:WriteTo:{i}:Args:path"] = expanded;

            var dir = Path.GetDirectoryName(expanded);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
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
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        if (overrides.Any())
        {
            builder.AddInMemoryCollection(overrides.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));
        }

        var configuration = builder.Build();

        var loggerConfig = new LoggerConfiguration().Enrich.FromLogContext();

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
    }
}