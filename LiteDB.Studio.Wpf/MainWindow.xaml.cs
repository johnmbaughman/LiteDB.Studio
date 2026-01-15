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

        private void LoadLastDb_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ViewModels.MainViewModel;

            var last = LiteDB.Studio.Wpf.Util.AppSettingsManager.ApplicationSettings.LastConnectionStrings?.Filename;

            if (!string.IsNullOrEmpty(last) && vm != null)
            {
                _ = vm.OpenRecentAsync(last);
            }
        }

        private void DbTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = this.DataContext as MainViewModel;
            if (vm == null) return;

            if (DbTree.SelectedItem is DbTreeNode node && node.Tag is string cmd)
            {
                vm.AddSqlSnippet(cmd);
                if (vm.RunCommand.CanExecute(null)) vm.RunCommand.Execute(null);
            }
        }

        private void DbTree_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // select the item under mouse
            var element = e.OriginalSource as DependencyObject;
            var tvi = VisualUpwardSearch<TreeViewItem>(element);
            if (tvi != null)
            {
                tvi.IsSelected = true;
                e.Handled = true;

                if (tvi.DataContext is DbTreeNode node)
                {
                    var ctx = new ContextMenu();
                    void AddQueryItem(string header, RoutedEventHandler handler)
                    {
                        var mi = new MenuItem { Header = header };
                        mi.Click += handler;
                        ctx.Items.Add(mi);
                    }

                    AddQueryItem("Query", (s, ev) =>
                    {
                        var vm2 = this.DataContext as MainViewModel;
                        if (vm2 != null && node.Tag is string c) vm2.AddSqlSnippet(c);
                    });

                    AddQueryItem("Count", (s, ev) =>
                    {
                        var vm2 = this.DataContext as MainViewModel;
                        if (vm2 != null && node.Header != null)
                        {
                            var name = node.Header;
                            var count = vm2.GetCollectionCount(name);
                            MessageBox.Show(this, $"{name}: {count}", "Count", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });

                    AddQueryItem("Explain plan", (s, ev) =>
                    {
                        var vm2 = this.DataContext as MainViewModel;
                        if (vm2 != null && node.Tag is string c)
                        {
                            vm2.AddSqlSnippet("EXPLAIN " + c);
                        }
                    });

                    AddQueryItem("Indexes", (s, ev) =>
                    {
                        var vm2 = this.DataContext as MainViewModel;
                        if (vm2 != null && node.Header != null)
                        {
                            var idx = string.Join("\n", vm2.GetCollectionIndexes(node.Header));
                            MessageBox.Show(this, idx.Length == 0 ? "No indexes" : idx, "Indexes", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });

                    AddQueryItem("Export to JSON", (s, ev) =>
                    {
                        var vm2 = this.DataContext as MainViewModel;
                        if (vm2 != null && node.Header != null)
                        {
                            var dlg = new Microsoft.Win32.SaveFileDialog { FileName = node.Header + ".json", Filter = "JSON files|*.json|All files|*.*" };
                            if (dlg.ShowDialog() == true)
                            {
                                vm2.ExportCollectionToJson(node.Header, dlg.FileName);
                            }
                        }
                    });

                    AddQueryItem("Analyze", (s, ev) =>
                    {
                        MessageBox.Show(this, "Analyze is not implemented.", "Analyze", MessageBoxButton.OK, MessageBoxImage.Information);
                    });

                    AddQueryItem("Rename", (s, ev) =>
                    {
                        var vm2 = this.DataContext as MainViewModel;
                        if (vm2 != null && node.Header != null)
                        {
                            var input = Microsoft.VisualBasic.Interaction.InputBox($"Rename collection '{node.Header}' to:", "Rename Collection", node.Header);
                            if (!string.IsNullOrWhiteSpace(input) && input != node.Header)
                            {
                                if (!vm2.RenameCollection(node.Header, input))
                                {
                                    MessageBox.Show(this, "Rename failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    });

                    AddQueryItem("Drop collection", (s, ev) =>
                    {
                        var vm2 = this.DataContext as MainViewModel;
                        if (vm2 != null && node.Header != null)
                        {
                            var res = MessageBox.Show(this, $"Drop collection '{node.Header}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (res == MessageBoxResult.Yes)
                            {
                                vm2.DropCollection(node.Header);
                            }
                        }
                    });

                    tvi.ContextMenu = ctx;
                    ctx.IsOpen = true;
                }
            }
        }

        private static T? VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as T;
        }
    }
}
