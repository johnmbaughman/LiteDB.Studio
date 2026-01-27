using System;
using System.Windows.Controls;
using LiteDB.Studio.Mvvm.ViewModels;
using LiteDB.Studio.Mvvm.ViewModels.Shell;
using LiteDB.Studio.Mvvm.Views.Shell;

namespace LiteDB.Studio.Mvvm.Views;

public abstract class ViewFor<TViewModel> : UserControl, IViewFor<TViewModel>
    where TViewModel : class, IViewModel
{
    protected ViewFor(TViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        DataContext = viewModel;

        if (viewModel is IShellContentViewModel shellContentViewModel && this is IShellContentView shellContentView)
        {
            shellContentViewModel.View = shellContentView;
        }
    }

    public TViewModel ViewModel { get; }

    IViewModel IView.ViewModel => ViewModel;
}
