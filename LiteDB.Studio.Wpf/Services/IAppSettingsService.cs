using LiteDB.Studio.Wpf.Util;

namespace LiteDB.Studio.Wpf.Services;

public interface IAppSettingsService
{
    ApplicationSettings ApplicationSettings { get; }
    void PersistData();
    bool IsLastDbExist();
    bool IsDbExist(string db);
    void AddToRecentList(ConnectionString? connectionString);
    void ValidateRecentList(bool removeOverflowedItems = true);
    void ClearRecentList();
}
