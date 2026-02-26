namespace LiteDB.Studio.Wpf.Util;

/// <summary>Persisted application settings serialised to <c>appsettings.json</c>.</summary>
[Serializable]
public class ApplicationSettings
{
    /// <summary>Gets or sets the connection string for the last opened database.</summary>
    public ConnectionString? LastConnectionStrings { get; set; }
    /// <summary>Gets or sets the ordered list of recently opened database connection strings.</summary>
    public List<ConnectionString> RecentConnectionStrings { get; set; } = [];

    /// <summary>Gets or sets the maximum number of entries kept in the recent-databases list.</summary>
    public int MaxRecentListItems { get; set; } = 10;
    /// <summary>Gets or sets a value indicating whether the last-used database is opened on startup.</summary>
    public bool LoadLastDbOnStartup { get; set; }
}
