using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Mvvm.Views;

/// <summary>Base contract for all views that expose a typed <see cref="IViewModel"/>.</summary>
public interface IView
{
    /// <summary>Gets the view model associated with this view.</summary>
    IViewModel ViewModel { get; }
}
