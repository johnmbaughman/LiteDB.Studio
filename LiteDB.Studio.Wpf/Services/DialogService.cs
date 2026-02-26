using System.Windows;

namespace LiteDB.Studio.Wpf.Services;

/// <summary>Specifies the icon shown in a dialog.</summary>
public enum DialogIcon
{
    /// <summary>No icon.</summary>
    None,
    /// <summary>Informational icon.</summary>
    Information,
    /// <summary>Warning icon.</summary>
    Warning,
    /// <summary>Error icon.</summary>
    Error,
    /// <summary>Question icon.</summary>
    Question
}

/// <summary>Provides a method for showing confirmation dialogs.</summary>
public interface IDialogService
{
    /// <summary>Shows a yes/no confirmation dialog and returns <c>true</c> when the user clicks Yes.</summary>
    /// <param name="message">The message text to display.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="icon">Optional icon to display.</param>
    bool Confirm(string message, string title, DialogIcon icon = DialogIcon.None);
}

/// <summary>WPF <see cref="MessageBox"/>-backed implementation of <see cref="IDialogService"/>.</summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc />
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
