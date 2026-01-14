using System.Windows;
using System.Windows.Controls;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LiteDB.Studio.Wpf
{
    public partial class MainWindow : Window
    {
        private ICSharpCode.AvalonEdit.TextEditor? _editor;
        private TabViewModel? _currentTab;

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
                // load persisted recent list
                foreach (var cs in LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.RecentConnectionStrings)
                {
                    vmContext.RecentDatabases.Add(cs.Filename);
                }

                // set load-last flag
                vmContext.LoadLastDatabaseOnStartup = LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LoadLastDbOnStartup;

                // auto-open last DB if requested
                if (LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LoadLastDbOnStartup && LiteDB.Studio.Wpf.Util.AppSettingsManager.IsLastDbExist())
                {
                    var last = LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;
                    if (!string.IsNullOrEmpty(last))
                    {
                        // kick off open recent (async)
                        _ = vmContext.OpenRecentAsync(last);
                    }
                }
            }

            // Hook into TabControl selection changes to manually save/load editor text
            QueryTabs.SelectionChanged += QueryTabs_SelectionChanged;
        }

        private void QueryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_editor == null) return;

            // Save text from the old tab
            if (_currentTab != null && !_currentTab.IsPlus)
            {
                _currentTab.Content = _editor.Text ?? string.Empty;
            }

            // Load text for the new tab
            var newTab = QueryTabs.SelectedItem as TabViewModel;
            if (newTab != null && !newTab.IsPlus)
            {
                _editor.Text = newTab.Content ?? string.Empty;
                _currentTab = newTab;
            }
        }

        private void OnTabEditorLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ICSharpCode.AvalonEdit.TextEditor te)
            {
                _editor = te;

                // Set current tab and load its content
                var tab = QueryTabs.SelectedItem as TabViewModel;
                if (tab != null && !tab.IsPlus)
                {
                    _currentTab = tab;
                    _editor.Text = tab.Content ?? string.Empty;
                }
            }
        }
    }
}
