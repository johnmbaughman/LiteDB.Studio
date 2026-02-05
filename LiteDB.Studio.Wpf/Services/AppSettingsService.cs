using LiteDB.Studio.Wpf.Util;
using System.IO;

namespace LiteDB.Studio.Wpf.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsFolder;
    private readonly string _settingsFile;

    public ApplicationSettings ApplicationSettings { get; private set; }

    public AppSettingsService(string? settingsFolder = null)
    {
        _settingsFolder = settingsFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiteDB.Studio");
        _settingsFile = Path.Combine(_settingsFolder, "appsettings.json");

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

    public void PersistData()
    {
        var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(ApplicationSettings, opts);
        File.WriteAllText(_settingsFile, json);
    }

    public bool IsLastDbExist()
    {
        if (ApplicationSettings.LastConnectionStrings == null) {
            return false;
        }

        var ldb = ApplicationSettings.LastConnectionStrings.Filename;
        return !string.IsNullOrEmpty(ldb) && File.Exists(ldb);
    }

    public bool IsDbExist(string db)
    {
        return !string.IsNullOrEmpty(db) && File.Exists(db);
    }

    public void AddToRecentList(ConnectionString? connectionString)
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

    public void ValidateRecentList(bool removeOverflowedItems = true)
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

    public void ClearRecentList()
    {
        ApplicationSettings.RecentConnectionStrings.Clear();
        PersistData();
    }
}
