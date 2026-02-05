using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB.Studio.Wpf.Services;

namespace LiteDB.Studio.Wpf.ViewModels;

public sealed partial class TabManager : ObservableObject
{
    private readonly IDatabaseService _databaseService;

    private TabViewModel? _selectedTab;

    public TabManager(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

        Tabs = [new TabViewModel(_databaseService) { Title = "+", IsPlus = true }];
    }

    public ObservableCollection<TabViewModel> Tabs { get; }

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

    public bool HasUnsavedTabs => Tabs.Any(t => t is { IsModified: true, IsPlus: false });

    public bool HasUserTabs => Tabs.Any(t => !t.IsPlus);

    public void AddNewTab()
    {
        var newTab = new TabViewModel(_databaseService) { Title = $"Query {Tabs.Count}" };

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
            var newTab = new TabViewModel(_databaseService) { Title = $"Query {Tabs.Count}", EditorText = sql.Replace("\\n", "\n") };
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
