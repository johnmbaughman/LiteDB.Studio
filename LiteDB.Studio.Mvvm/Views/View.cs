using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Mvvm.Views;

public abstract class View : IView
{
    public abstract IViewModel ViewModel { get; }
}
