using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views;
using LiteDB.Studio.Mvvm.Views.Shell;
using LiteDB.Studio.Wpf.ViewModels;
using LiteDB.Studio.Wpf.Services;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LiteDB.Studio.Wpf.Views;

public partial class MainWindow : IShellContentView, IView
{
    private readonly MainViewModel _viewModel;
    private readonly IAppSettingsService _appSettingsService;

    public MainWindow(MainViewModel viewModel, DatabaseTreeView databaseTreeView, IAppSettingsService appSettingsService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(databaseTreeView);
        ArgumentNullException.ThrowIfNull(appSettingsService);

        _viewModel = viewModel;
        _appSettingsService = appSettingsService;

        InitializeComponent();

        DataContext = _viewModel;

        // Set the DatabaseTreeView's Content property after InitializeComponent
        DatabaseTreeViewHost.Content = databaseTreeView;

        if (_viewModel is IShellContentViewModel shellContentViewModel)
        {
            shellContentViewModel.View = this;
        }

        Loaded += async (_, _) =>
        {
            await ShellContentViewModel.ViewLoaded();

            // Register SQL highlighting after the view is loaded so editors inside templates exist
            RegisterSqlHighlighting();

            // Ensure highlighting is applied when tab containers are generated or selection changes
            try
            {
                QueryTabs.ItemContainerGenerator.StatusChanged += (_, _) =>
                {
                    if (QueryTabs.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                    {
                        ApplyHighlightingToEditors();
                    }
                };

                QueryTabs.SelectionChanged += (_, _) => ApplyHighlightingToEditors();
            }
            catch
            {
                // best-effort
            }
        };
    }

    private void LoadLastDb_Click(object sender, RoutedEventArgs e)
    {
        var last = _appSettingsService.ApplicationSettings.LastConnectionStrings?.Filename;

        if (!string.IsNullOrEmpty(last))
        {
            _ = _viewModel.OpenRecentAsync(last);
        }
    }

    public IShellContentViewModel ShellContentViewModel => _viewModel;

    public IViewModel ViewModel => _viewModel;

    private void RegisterSqlHighlighting()
    {
        try
        {
            // Load SQL highlighting from this assembly's embedded resources (SQL-Mode.xshd)
            Assembly asm = typeof(MainWindow).Assembly;
            var resourceName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("SQL-Mode.xshd", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
            {
                return;
            }

            using Stream? stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return;
            }

            using var reader = new XmlTextReader(stream);
            XshdSyntaxDefinition? xshd = HighlightingLoader.LoadXshd(reader);
            IHighlightingDefinition? highlight = HighlightingLoader.Load(xshd, HighlightingManager.Instance);

            // Register under the name SQL if not present
            if (HighlightingManager.Instance.GetDefinition("SQL") == null)
            {
                HighlightingManager.Instance.RegisterHighlighting("SQL", [".sql"], highlight);
            }

            // Apply highlight to any existing AvalonEdit editors in the visual tree
            try
            {
                IHighlightingDefinition? def = HighlightingManager.Instance.GetDefinition("SQL") ?? highlight;
                if (def == null)
                {
                    return;
                }

                foreach (TextEditor editor in FindVisualChildren<TextEditor>(this))
                {
                    editor.SyntaxHighlighting = def;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying highlighting: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SQL highlighting failed: {ex.Message}");
            // Best-effort: ignore and continue without SQL highlighting
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
        {
            if (depObj == null)
            {
                yield break;
            }

            var count = VisualTreeHelper.GetChildrenCount(depObj);
            for (var i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                {
                    yield return t;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

    private void ApplyHighlightingToEditors()
    {
        try
        {
            IHighlightingDefinition? def = HighlightingManager.Instance.GetDefinition("SQL");
            if (def == null)
            {
                return;
            }

            foreach (TextEditor editor in FindVisualChildren<TextEditor>(this))
            {
                editor.SyntaxHighlighting = def;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ApplyHighlightingToEditors failed: {ex.Message}");
        }
    }
}


