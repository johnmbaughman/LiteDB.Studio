using System.Windows;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LiteDB.Studio.Wpf.Services;

/// <summary>Holds the values entered by the user in the connection-manager dialog.</summary>
/// <param name="Mode">Direct or Shared connection mode.</param>
/// <param name="Filename">Path to the LiteDB database file.</param>
/// <param name="Password">Optional encryption password.</param>
/// <param name="InitialSize">Initial database file size in megabytes (0 = default).</param>
/// <param name="CollationLeft">Left part of the collation string (e.g. locale code).</param>
/// <param name="CollationRight">Right part of the collation string (e.g. sort options).</param>
/// <param name="ReadOnly">Whether to open the database in read-only mode.</param>
/// <param name="UpgradeFromV4">Whether to upgrade the file from LiteDB v4 format.</param>
public sealed record ConnectionManagerDialogResult(
    ConnectionMode Mode,
    string Filename,
    string Password,
    int InitialSize,
    string CollationLeft,
    string CollationRight,
    bool ReadOnly,
    bool UpgradeFromV4);

/// <summary>Shows the connection-manager dialog and returns the user's input, or <c>null</c> if cancelled.</summary>
public interface IConnectionManagerDialogService
{
    /// <summary>Shows the dialog modally and returns the entered settings, or <c>null</c> when the user cancels.</summary>
    ConnectionManagerDialogResult? ShowDialog();
}

/// <summary>Service-locator–based implementation of <see cref="IConnectionManagerDialogService"/>.</summary>
public sealed class ConnectionManagerDialogService(IServiceProvider services) : IConnectionManagerDialogService
{
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
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
