using System.Windows;
using LiteDB.Studio.Mvvm.Extensions;
using LiteDB.Studio.Mvvm.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LiteDB.Studio.Mvvm;

public class LiteDbStudioApplication : Application {
    /// <summary>
    /// The <see cref="IHost"/> instance for the application. All configurable items are contained here during startup.
    /// </summary>
    public static IHost AppHost { get; protected set; } = null!;

    /// <summary>
    /// Gets the name of the application as set during startup.
    /// </summary>
    /// <remarks>
    /// This property is assigned in <see cref="StartApplication"/> and provides the identifier for the running application instance.
    /// </remarks>
    /// <value>The name of the application.</value>
    public static string ApplicationName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the Serilog <see cref="ILogger"/> instance from the <see cref="IServiceProvider"/> instance.
    /// </summary>
    public static ILogger? Logger => Services.TryGetService(out ILogger logger) ? logger : null;

    /// <summary>
    /// Gets the root <see cref="IServiceProvider"/> for dependency resolution.
    /// </summary>
    /// <value>The services.</value>
    public static IServiceProvider Services => AppHost.Services;

    /// <summary>
    /// Displays the main <see cref="IShellView"/>, <see cref="Window"/>-based window.
    /// </summary>
    public void ShowMainWindow() {
        MainWindow = (Window)Services.GetRequiredService<IShellView>();
        MainWindow.Show();
    }

    /// <summary>
    /// Starts the application lifecycle and triggers host startup.
    /// Sets the application name, starts the host asynchronously, and calls the base OnStartup method.
    /// </summary>
    /// <param name="applicationName">The name to assign to the running application instance.</param>
    /// <param name="e">The startup event arguments provided by WPF.</param>
    public async Task StartApplicationAsync(string applicationName, StartupEventArgs e) {
        ApplicationName = applicationName;

        try
        {
            await AppHost.StartAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Ensure logging is recorded if host fails to start
            Logger?.Error(ex, "Host failed to start: {Message}", ex.Message);
            throw;
        }

        base.OnStartup(e);
    }
}
