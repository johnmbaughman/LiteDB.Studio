using System;
using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels;

namespace LiteDB.Studio.Mvvm.Hosting;

public interface IViewFactory
{
    FrameworkElement CreateView(Type viewType);
    FrameworkElement CreateViewFor(Type viewModelType);
    TView CreateView<TView>() where TView : FrameworkElement;
    FrameworkElement CreateViewFor<TViewModel>() where TViewModel : class, IViewModel;
}
