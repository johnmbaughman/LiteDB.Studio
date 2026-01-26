using System.IO;

namespace LiteDB.Studio.Wpf.Util;

public static class AppSettingsManager
{
    private static readonly string _settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiteDB.Studio");
    private static readonly string _settingsFile = Path.Combine(_settingsFolder, "appsettings.json");

    public static ApplicationSettings ApplicationSettings { get; set; }

    static AppSettingsManager()
    {
        if (!Directory.Exists(_settingsFolder)) {
            Directory.CreateDirectory(_settingsFolder);
        }

        if (!File.Exists(_settingsFile))
        {
            ApplicationSettings = new ApplicationSettings();
            PersistData();
        }
        else
        {
            var json = File.ReadAllText(_settingsFile);
            try
            {
                ApplicationSettings = System.Text.Json.JsonSerializer.Deserialize<ApplicationSettings>(json) ?? new ApplicationSettings();
            }
            catch
            {
                ApplicationSettings = new ApplicationSettings();
            }
        }
    }

    public static void PersistData()
    {
        var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(ApplicationSettings, opts);
        File.WriteAllText(_settingsFile, json);
    }

    public static bool IsLastDbExist()
    {
        if (ApplicationSettings.LastConnectionStrings == null) {
            return false;
        }

        var ldb = ApplicationSettings.LastConnectionStrings.Filename;
        return !string.IsNullOrEmpty(ldb) && File.Exists(ldb);
    }

    public static bool IsDbExist(string db)
    {
        return !string.IsNullOrEmpty(db) && File.Exists(db);
    }

    public static void AddToRecentList(ConnectionString? connectionString)
    {
        if (connectionString == null) {
            return;
        }

        ConnectionString? connection = ApplicationSettings.RecentConnectionStrings.FirstOrDefault(cs => cs.Filename == connectionString.Filename);
        if (connection != null)
        {
            ApplicationSettings.RecentConnectionStrings.Remove(connection);
        }

        if (ApplicationSettings.RecentConnectionStrings.Count + 1 > ApplicationSettings.MaxRecentListItems)
        {
            ApplicationSettings.RecentConnectionStrings.RemoveAt(ApplicationSettings.RecentConnectionStrings.Count - 1);
        }

        ApplicationSettings.RecentConnectionStrings = new List<ConnectionString>(ApplicationSettings.RecentConnectionStrings.Prepend(connectionString));
        PersistData();
    }

    public static void ValidateRecentList(bool removeOverflowedItems = true)
    {
        var toRemove = ApplicationSettings.RecentConnectionStrings.Where(connectionString => !IsDbExist(connectionString.Filename)).ToList();
        foreach (ConnectionString connectionString in toRemove)
        {
            ApplicationSettings.RecentConnectionStrings.Remove(connectionString);
        }

        var diff = ApplicationSettings.RecentConnectionStrings.Count - ApplicationSettings.MaxRecentListItems;
        if (diff <= 0) {
            return;
        }

        var startIndex = ApplicationSettings.RecentConnectionStrings.Count - diff;
        ApplicationSettings.RecentConnectionStrings.RemoveRange(startIndex, diff);
        PersistData();
    }

    public static void ClearRecentList()
    {
        ApplicationSettings.RecentConnectionStrings.Clear();
        PersistData();
    }
}
