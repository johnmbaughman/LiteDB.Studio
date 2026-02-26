using LiteDB.Studio.Wpf.Util;

namespace LiteDB.Studio.Wpf.Services;

/// <summary>Defines the contract for reading and persisting application settings.</summary>
public interface IAppSettingsService
{
    /// <summary>Gets the current application settings instance.</summary>
    ApplicationSettings ApplicationSettings { get; }

    /// <summary>Persists the current settings to disk.</summary>
    void PersistData();

    /// <summary>Returns <c>true</c> if the most-recently-used database file exists on disk.</summary>
    bool IsLastDbExist();

    /// <summary>Returns <c>true</c> if the specified database file path exists on disk.</summary>
    /// <param name="db">Absolute path to the database file.</param>
    bool IsDbExist(string db);

    /// <summary>Prepends <paramref name="connectionString"/> to the recent-connections list and persists.</summary>
    /// <param name="connectionString">The connection string to add, or <c>null</c> to no-op.</param>
    void AddToRecentList(ConnectionString? connectionString);

    /// <summary>Removes non-existent files from the recent-connections list.</summary>
    /// <param name="removeOverflowedItems">When <c>true</c>, also trims the list to <see cref="ApplicationSettings.MaxRecentListItems"/>.</param>
    void ValidateRecentList(bool removeOverflowedItems = true);

    /// <summary>Clears the recent-connections list and persists the change.</summary>
    void ClearRecentList();
}
