using LiteDB.Studio.Mvvm.ViewModels;
using LiteDB.Studio.Mvvm.Views;
using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views;

// TODO: Implement abstract base class instead of interface.
public partial class DatabaseTreeView : IContentView
{
    private readonly DatabaseTreeViewModel _viewModel;

    public DatabaseTreeView(DatabaseTreeViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        _viewModel = viewModel;

        InitializeComponent();

        DataContext = _viewModel;

        Loaded += async (_, _) => await ViewModel.ViewLoaded();
    }

    public IViewModel ViewModel => _viewModel;
}
