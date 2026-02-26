using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB.Studio.Wpf.Services;
using Microsoft.Extensions.Logging;

namespace LiteDB.Studio.Wpf.ViewModels;

/// <summary>
/// Manages the collection of editor tabs, including creation, selection, closure, and snippet insertion.
/// </summary>
public sealed class TabManager : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDialogService? _dialogService;
    private readonly Func<TabViewModel, CancellationToken, Task>? _saveDelegate;

    private TabViewModel? _selectedTab;

    /// <summary>Initializes a new <see cref="TabManager"/> with a single placeholder plus-tab.</summary>
    /// <param name="databaseService">Database service passed to each new <see cref="TabViewModel"/>.</param>
    /// <param name="loggerFactory">Logger factory passed to each new <see cref="TabViewModel"/>.</param>
    /// <param name="dialogService">Optional dialog service for unsaved-changes prompts.</param>
    /// <param name="saveDelegate">Optional delegate invoked when a tab's content should be saved.</param>
    public TabManager(IDatabaseService databaseService, ILoggerFactory loggerFactory, IDialogService? dialogService = null, Func<TabViewModel, CancellationToken, Task>? saveDelegate = null)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _dialogService = dialogService;
        _saveDelegate = saveDelegate;

        Tabs = [new TabViewModel(_databaseService, _loggerFactory, _dialogService) { Title = "+", IsPlus = true }];
    }

    /// <summary>Gets the observable collection of tabs displayed in the UI (includes the plus-tab).</summary>
    public ObservableCollection<TabViewModel> Tabs { get; }

    /// <summary>Gets or sets the currently selected tab. Selecting the plus-tab automatically opens a new tab.</summary>
    public TabViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (!SetProperty(ref _selectedTab, value))
            {
                return;
            }

            if (_selectedTab is { Title: "+" })
            {
                AddNewTab();
            }
        }
    }

    /// <summary>Gets a value indicating whether any non-plus tab has unsaved changes.</summary>
    public bool HasUnsavedTabs => Tabs.Any(t => t is { IsModified: true, IsPlus: false });

    /// <summary>Gets a value indicating whether at least one non-plus tab exists.</summary>
    public bool HasUserTabs => Tabs.Any(t => !t.IsPlus);

    /// <summary>Creates a new query tab, inserts it before the plus-tab, and selects it.</summary>
    public void AddNewTab()
    {
        var newTab = new TabViewModel(_databaseService, _loggerFactory, _dialogService) { Title = $"Query {Tabs.Count}" };
        if (_saveDelegate != null)
        {
            newTab.SaveAction = ct => _saveDelegate(newTab, ct);
        }

        // insert before plus tab
        TabViewModel? plus = Tabs.FirstOrDefault(t => t.Title == "+");
        if (plus != null)
        {
            var idx = Tabs.IndexOf(plus);
            Tabs.Insert(idx, newTab);
        }
        else
        {
            Tabs.Add(newTab);
        }

        SelectedTab = newTab;
    }

    /// <summary>Removes <paramref name="tab"/> from the tab list, creating a replacement tab if none remain.</summary>
    /// <param name="tab">The tab to close. Null or the plus-tab are silently ignored.</param>
    public void CloseTab(TabViewModel? tab)
    {
        if (tab == null || tab.IsPlus) {
            return;
        }

        var idx = Tabs.IndexOf(tab);
        if (idx >= 0) {
            Tabs.RemoveAt(idx);
        }

        // If there are no non-plus (real) tabs, ensure we create one
        if (Tabs.All(t => t.IsPlus))
        {
            AddNewTab();
        }

        // Select a reasonable tab: prefer the item that occupies the previous index, then fallback
        if (Tabs.Count <= 0) { return; }

        var selectIndex = Math.Min(idx, Tabs.Count - 1);
        SelectedTab = Tabs[selectIndex];

        if (SelectedTab.IsPlus)
        {
            TabViewModel? nonPlus = Tabs.FirstOrDefault(t => !t.IsPlus);
            if (nonPlus != null)
            {
                SelectedTab = nonPlus;
            }
        }
    }

    /// <summary>Inserts <paramref name="snippet"/> at the caret position of the selected tab.</summary>
    /// <param name="snippet">Text to insert, or <c>null</c>/<c>""</c> to no-op.</param>
    public void InsertSnippet(string? snippet)
    {
        if (SelectedTab == null || string.IsNullOrEmpty(snippet)) {
            return;
        }

        var text = SelectedTab.EditorText;
        var offset = SelectedTab.CaretOffset;

        // Ensure offset is within bounds
        if (offset < 0) {
            offset = 0;
        }

        if (offset > text.Length) {
            offset = text.Length;
        }

        SelectedTab.EditorText = text.Insert(offset, snippet);
        SelectedTab.IsModified = true;
    }

    /// <summary>
    /// Places <paramref name="sql"/> into the current tab (if empty) or opens a new tab containing it.
    /// </summary>
    /// <param name="sql">SQL text to place. Whitespace-only values are ignored.</param>
    public void AddSqlSnippet(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) {
            return;
        }

        // if there's no selected tab or selected tab is the plus tab, or current content is empty -> set into current
        if (SelectedTab == null || SelectedTab.Title == "+" || string.IsNullOrWhiteSpace(SelectedTab.EditorText))
        {
            // ensure there's a non-plus tab to place content
            if (SelectedTab == null || SelectedTab.Title == "+")
            {
                AddNewTab();
            }

            SelectedTab = SelectedTab ?? throw new InvalidOperationException("SelectedTab is null after AddNewTab");
            SelectedTab.EditorText = sql.Replace("\\n", "\n");
        }
        else
        {
            // insert new tab before plus
            TabViewModel? plus = Tabs.FirstOrDefault(t => t.Title == "+");
            var newTab = new TabViewModel(_databaseService, _loggerFactory, _dialogService)
            {
                Title = $"Query {Tabs.Count}",
                EditorText = sql.Replace("\\n", "\n")
            };
            if (_saveDelegate != null)
            {
                newTab.SaveAction = ct => _saveDelegate(newTab, ct);
            }
            if (plus != null)
            {
                var idx = Tabs.IndexOf(plus);
                Tabs.Insert(idx, newTab);
            }
            else
            {
                Tabs.Add(newTab);
            }

            SelectedTab = newTab;
        }
    }

    /// <summary>Always opens a new tab and sets its content to <paramref name="sql"/>.</summary>
    /// <param name="sql">SQL text to place. Whitespace-only values are ignored.</param>
    public void AddSqlSnippetInNewTab(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) {
            return;
        }

        AddNewTab();

        SelectedTab = SelectedTab ?? throw new InvalidOperationException("SelectedTab is null after AddNewTab");
        SelectedTab.EditorText = sql.Replace("\\n", "\n");
    }
}
