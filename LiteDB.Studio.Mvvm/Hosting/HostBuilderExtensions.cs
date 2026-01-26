using System.Runtime;
using LiteDB.Studio.Mvvm.Properties;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            services.AddSingleton<IShellView, ShellView>();
            services.AddSingleton<IShellViewModel, ShellViewModel>();
            services.AddSingleton<IShellContentView, TV>();
            services.AddSingleton<IShellContentViewModel, TVm>();
        });

        return hostBuilder;
    }
}
