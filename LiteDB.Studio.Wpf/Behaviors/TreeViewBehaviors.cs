using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LiteDB.Studio.Wpf.Behaviors;

public static class TreeViewBehaviors
{
    private sealed class HandlerState
    {
        public MouseButtonEventHandler? Handler { get; set; }
    }

    private static readonly ConditionalWeakTable<TreeView, HandlerState> _handlers = new();

    public static readonly DependencyProperty ItemDoubleClickCommandProperty = DependencyProperty.RegisterAttached(
        "ItemDoubleClickCommand",
        typeof(ICommand),
        typeof(TreeViewBehaviors),
        new PropertyMetadata(null, OnItemDoubleClickCommandChanged));

    public static ICommand? GetItemDoubleClickCommand(DependencyObject obj)
    {
        return (ICommand?)obj.GetValue(ItemDoubleClickCommandProperty);
    }

    public static void SetItemDoubleClickCommand(DependencyObject obj, ICommand? value)
    {
        obj.SetValue(ItemDoubleClickCommandProperty, value);
    }

    private static void OnItemDoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView treeView)
        {
            return;
        }

        if (!_handlers.TryGetValue(treeView, out HandlerState? state))
        {
            state = new HandlerState();
            _handlers.Add(treeView, state);
        }

        if (state.Handler != null)
        {
            treeView.MouseDoubleClick -= state.Handler;
            state.Handler = null;
        }

        if (e.NewValue is not ICommand)
        {
            return;
        }

        state.Handler = (_, _) =>
        {
            if (GetItemDoubleClickCommand(treeView) is not ICommand command)
            {
                return;
            }

            var parameter = treeView.SelectedItem;
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        };

        treeView.MouseDoubleClick += state.Handler;
    }
}
