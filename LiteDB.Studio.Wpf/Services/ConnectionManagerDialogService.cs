using System.Windows;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LiteDB.Studio.Wpf.Services;

public sealed record ConnectionManagerDialogResult(
    ConnectionMode Mode,
    string Filename,
    string Password,
    int InitialSize,
    string CollationLeft,
    string CollationRight,
    bool ReadOnly,
    bool UpgradeFromV4);

public interface IConnectionManagerDialogService
{
    ConnectionManagerDialogResult? ShowDialog();
}

public sealed class ConnectionManagerDialogService(IServiceProvider services) : IConnectionManagerDialogService
{
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

    public ConnectionManagerDialogResult? ShowDialog()
    {
        ConnectionManagerWindow window = _services.GetRequiredService<ConnectionManagerWindow>();
        window.Owner = Application.Current?.MainWindow;

        var shown = window.ShowDialog();
        if (shown != true)
        {
            return null;
        }

        if (window.DataContext is not ConnectionManagerViewModel vm)
        {
            return null;
        }

        return new ConnectionManagerDialogResult(
            vm.Mode,
            vm.Filename,
            vm.Password,
            vm.InitialSize,
            vm.CollationLeft,
            vm.CollationRight,
            vm.ReadOnly,
            vm.UpgradeFromV4);
    }
}
