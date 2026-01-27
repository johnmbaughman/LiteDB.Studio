// unset:none

using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Mvvm.Views;

public interface IView
{
    IViewModel ViewModel { get; }
}
