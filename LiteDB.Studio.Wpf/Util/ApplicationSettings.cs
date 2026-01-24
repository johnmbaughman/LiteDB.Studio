namespace LiteDB.Studio.Wpf.Util;

[Serializable]
public class ApplicationSettings
{
    public ConnectionString? LastConnectionStrings { get; set; }
    public List<ConnectionString> RecentConnectionStrings { get; set; } = [];

    public int MaxRecentListItems { get; set; } = 10;
    public bool LoadLastDbOnStartup { get; set; }
}