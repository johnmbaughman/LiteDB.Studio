using System.Windows;
using LiteDB.Studio.Mvvm;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views.Shell;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views;

// TODO: Implement abstract base class instead of interface.
public partial class MainWindow : IShellContentView
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = LiteDbStudioApplication.ShellContentViewModel;
        Loaded += async (_, _) => await ShellContentViewModel.ViewLoaded();

        //// Use App Host to create ViewModel with the runtime editor adapter
        //Microsoft.Extensions.Hosting.IHost? host = (Application.Current as App)?.HostInstance;

        //if (host != null)
        //{
        //    MainViewModel vm = host.Services.GetRequiredService<MainViewModel>();
        //    DataContext = vm;
        //}
        //else
        //{
        //    // Fall back to creating a local service if Host is not available
        //    var dbService = new LiteDbService();
        //    DataContext = new MainViewModel(dbService);
        //}

        //// populate recent list from settings
        //var vmContext = DataContext as MainViewModel;
        //vmContext?.Initialize();
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

    public IShellContentViewModel ShellContentViewModel => (IShellContentViewModel)DataContext;
}
