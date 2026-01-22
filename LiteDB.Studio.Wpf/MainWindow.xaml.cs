using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LiteDB.Studio.Wpf
{
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
                this.DataContext = vm;
            }
            else
            {
                // Fall back to creating a local service if Host is not available
                var dbService = new LiteDbService();
                this.DataContext = new MainViewModel(dbService);
            }

            // populate recent list from settings
            var vmContext = this.DataContext as ViewModels.MainViewModel;
            if (vmContext != null)
            {
                vmContext.Initialize();
            }
        }

        private void LoadLastDb_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ViewModels.MainViewModel;

            var last = LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;

            if (!string.IsNullOrEmpty(last) && vm != null)
            {
                _ = vm.OpenRecentAsync(last);
            }
        }




    }
}
