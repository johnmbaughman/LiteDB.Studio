using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Mvvm.Views;

public interface IContentView : IView
{
    IViewModel ViewModel { get; }
}
