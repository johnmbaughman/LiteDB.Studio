using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace LiteDB.Studio.Wpf.ViewModels
{
    public class TabViewModel : ObservableObject
    {
        private string _title = string.Empty;
        private string _content = string.Empty;
        private bool _isPlus;

        public TabViewModel() { }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public bool IsPlus
        {
            get => _isPlus;
            set => SetProperty(ref _isPlus, value);
        }
    }
}
