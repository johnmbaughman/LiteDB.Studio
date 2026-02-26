using LiteDB.Studio.Wpf.Util;
using System.IO;

namespace LiteDB.Studio.Wpf.Services;

/// <summary>
/// Reads and persists application settings as a JSON file under the user's app-data folder.
/// Implements <see cref="IAppSettingsService"/>.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsFile;

    /// <inheritdoc />
    public ApplicationSettings ApplicationSettings { get; }

    /// <summary>Initializes a new instance of <see cref="AppSettingsService"/>.</summary>
    /// <param name="settingsFolder">Override folder for the settings file. Defaults to <c>%APPDATA%\LiteDB.Studio</c>.</param>
    public AppSettingsService(string? settingsFolder = null)
    {
        var settingsFolder1 = settingsFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiteDB.Studio");
        _settingsFile = Path.Combine(settingsFolder1, "appsettings.json");

        if (!Directory.Exists(settingsFolder1)) {
            Directory.CreateDirectory(settingsFolder1);
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

    /// <inheritdoc />
    public void PersistData()
    {
        var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(ApplicationSettings, opts);
        File.WriteAllText(_settingsFile, json);
    }

    /// <inheritdoc />
    public bool IsLastDbExist()
    {
        if (ApplicationSettings.LastConnectionStrings == null) {
            return false;
        }

        var ldb = ApplicationSettings.LastConnectionStrings.Filename;
        return !string.IsNullOrEmpty(ldb) && File.Exists(ldb);
    }

    /// <inheritdoc />
    public bool IsDbExist(string db)
    {
        return !string.IsNullOrEmpty(db) && File.Exists(db);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void ClearRecentList()
    {
        ApplicationSettings.RecentConnectionStrings.Clear();
        PersistData();
    }
}
