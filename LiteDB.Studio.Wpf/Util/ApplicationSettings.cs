using System;
using System.Collections.Generic;
using LiteDB;

namespace LiteDB.Studio.Wpf.Util
{
    [Serializable]
    public class ApplicationSettings
    {
        public ConnectionString? LastConnectionStrings { get; set; }
        public List<ConnectionString> RecentConnectionStrings { get; set; }

        public int MaxRecentListItems { get; set; } = 10;
        public bool LoadLastDbOnStartup { get; set; }

        public ApplicationSettings()
        {
            RecentConnectionStrings = new List<ConnectionString>();
        }
    }
}
