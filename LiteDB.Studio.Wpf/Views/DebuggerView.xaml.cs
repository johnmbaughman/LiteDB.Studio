using LiteDB.Studio.Wpf.ViewModels;

namespace LiteDB.Studio.Wpf.Views;

public partial class DebuggerView
{
    public DebuggerView(DebuggerViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        DataContext = viewModel;
    }
}
