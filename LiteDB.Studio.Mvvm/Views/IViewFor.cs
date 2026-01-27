using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Mvvm.Views;

public interface IViewFor<out TViewModel> : IView where TViewModel : IViewModel
{
    new TViewModel ViewModel { get; }
}
