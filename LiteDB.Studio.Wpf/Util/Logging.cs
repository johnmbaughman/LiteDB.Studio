using System;
using System.IO;
using Serilog;

namespace LiteDB.Studio.Wpf.Util
{
    public static class Logging
    {
        public static void Configure()
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp",
                "LiteDB.Studio",
                "log-.txt"
            );

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30, // Keep logs for 30 days
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
        }
    }
}