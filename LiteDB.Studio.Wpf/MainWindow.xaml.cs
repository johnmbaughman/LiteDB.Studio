using LiteDB.Studio.Wpf.Services;
using LiteDB.Studio.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace LiteDB.Studio.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Use App Host to create ViewModel with the runtime editor adapter
        var host = (Application.Current as App)?.HostInstance;

        if (host != null)
        {
            var vm = host.Services.GetRequiredService<MainViewModel>();
            DataContext = vm;
        }
        else
        {
            // Fall back to creating a local service if Host is not available
            var dbService = new LiteDbService();
            DataContext = new MainViewModel(dbService);
        }

        // populate recent list from settings
        var vmContext = DataContext as MainViewModel;
        vmContext?.Initialize();


    }

    private void LoadLastDb_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;

        var last = Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;

        if (!string.IsNullOrEmpty(last) && vm != null)
        {
            _ = vm.OpenRecentAsync(last);
        }
    }
}