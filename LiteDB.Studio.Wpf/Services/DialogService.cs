using System.Windows;

namespace LiteDB.Studio.Wpf.Services;

public enum DialogIcon
{
    None,
    Information,
    Warning,
    Error,
    Question
}

public interface IDialogService
{
    bool Confirm(string message, string title, DialogIcon icon = DialogIcon.None);
}

public sealed class DialogService : IDialogService
{
    public bool Confirm(string message, string title, DialogIcon icon = DialogIcon.None)
    {
        MessageBoxImage image = icon switch
        {
            DialogIcon.Information => MessageBoxImage.Information,
            DialogIcon.Warning => MessageBoxImage.Warning,
            DialogIcon.Error => MessageBoxImage.Error,
            DialogIcon.Question => MessageBoxImage.Question,
            _ => MessageBoxImage.None
        };

        return MessageBox.Show(message, title, MessageBoxButton.YesNo, image) == MessageBoxResult.Yes;
    }
}
