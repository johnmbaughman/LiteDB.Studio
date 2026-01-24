using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;

namespace LiteDB.Studio.Wpf.Controls
{
    /// <summary>
    /// MVVM-friendly attached behaviors for AvalonEdit.TextEditor.
    /// Exposes EditorText (two-way), CaretOffset (two-way), SelectionStart/Length and IsModified.
    /// Also exposes ShowCompletionCommand (ICommand) which is executed when Ctrl+Space is pressed.
    /// </summary>
    public static class AvalonEditBehaviors
    {
        // Internal per-editor state to avoid update loops
        private class EditorState
        {
            public bool SuppressUpdate { get; set; }
        }

        private static readonly ConditionalWeakTable<TextEditor, EditorState> _states = new();

        #region EditorText
        public static readonly DependencyProperty EditorTextProperty = DependencyProperty.RegisterAttached(
            "EditorText", typeof(string), typeof(AvalonEditBehaviors),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditorTextChanged));

        public static string GetEditorText(DependencyObject obj) => (string)obj.GetValue(EditorTextProperty);
        public static void SetEditorText(DependencyObject obj, string value) => obj.SetValue(EditorTextProperty, value);

        private static void OnEditorTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                EnsureHandlers(editor);

                var state = _states.GetOrCreateValue(editor);
                if (state.SuppressUpdate) return;

                try
                {
                    state.SuppressUpdate = true;
                    var newText = e.NewValue as string ?? string.Empty;
                    if (editor.Text != newText)
                    {
                        var oldCaret = editor.TextArea.Caret.Offset;
                        editor.Text = newText;
                        // Try to preserve caret position
                        editor.TextArea.Caret.Offset = Math.Min(oldCaret, editor.Text.Length);
                    }
                    // Programmatic set should NOT mark IsModified
                    SetIsModified(editor, false);
                }
                finally
                {
                    state.SuppressUpdate = false;
                }
            }
        }
        #endregion

        #region CaretOffset
        public static readonly DependencyProperty CaretOffsetProperty = DependencyProperty.RegisterAttached(
            "CaretOffset", typeof(int), typeof(AvalonEditBehaviors),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCaretOffsetChanged));

        public static int GetCaretOffset(DependencyObject obj) => (int)obj.GetValue(CaretOffsetProperty);
        public static void SetCaretOffset(DependencyObject obj, int value) => obj.SetValue(CaretOffsetProperty, value);

        private static void OnCaretOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                EnsureHandlers(editor);
                var state = _states.GetOrCreateValue(editor);
                if (state.SuppressUpdate) return;
                try
                {
                    state.SuppressUpdate = true;
                    var desired = (int)e.NewValue;
                    if (editor.TextArea?.Caret != null)
                    {
                        editor.TextArea.Caret.Offset = Math.Max(0, Math.Min(desired, editor.Text.Length));
                    }
                }
                finally { state.SuppressUpdate = false; }
            }
        }
        #endregion

        #region SelectionStart/SelectionLength
        public static readonly DependencyProperty SelectionStartProperty = DependencyProperty.RegisterAttached(
            "SelectionStart", typeof(int), typeof(AvalonEditBehaviors), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));
        public static readonly DependencyProperty SelectionLengthProperty = DependencyProperty.RegisterAttached(
            "SelectionLength", typeof(int), typeof(AvalonEditBehaviors), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));

        public static int GetSelectionStart(DependencyObject obj) => (int)obj.GetValue(SelectionStartProperty);
        public static void SetSelectionStart(DependencyObject obj, int value) => obj.SetValue(SelectionStartProperty, value);
        public static int GetSelectionLength(DependencyObject obj) => (int)obj.GetValue(SelectionLengthProperty);
        public static void SetSelectionLength(DependencyObject obj, int value) => obj.SetValue(SelectionLengthProperty, value);

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                EnsureHandlers(editor);
                var state = _states.GetOrCreateValue(editor);
                if (state.SuppressUpdate) return;
                try
                {
                    state.SuppressUpdate = true;
                    var start = GetSelectionStart(editor);
                    var length = GetSelectionLength(editor);
                    editor.Select(start, length);
                }
                finally { state.SuppressUpdate = false; }
            }
        }
        #endregion

        #region IsModified
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.RegisterAttached(
            "IsModified", typeof(bool), typeof(AvalonEditBehaviors), new FrameworkPropertyMetadata(false));

        public static bool GetIsModified(DependencyObject obj) => (bool)obj.GetValue(IsModifiedProperty);
        public static void SetIsModified(DependencyObject obj, bool value) => obj.SetValue(IsModifiedProperty, value);
        #endregion

        #region ShowCompletionCommand
        public static readonly DependencyProperty ShowCompletionCommandProperty = DependencyProperty.RegisterAttached(
            "ShowCompletionCommand", typeof(ICommand), typeof(AvalonEditBehaviors), new PropertyMetadata(null, OnShowCompletionCommandChanged));

        public static ICommand? GetShowCompletionCommand(DependencyObject obj) => (ICommand?)obj.GetValue(ShowCompletionCommandProperty);
        public static void SetShowCompletionCommand(DependencyObject obj, ICommand? value) => obj.SetValue(ShowCompletionCommandProperty, value);

        private static void OnShowCompletionCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                EnsureHandlers(editor);
                // Add or update a KeyBinding for Ctrl+Space so commands can be invoked via WPF input system
                try
                {
                    var cmd = e.NewValue as ICommand;
                    // Find existing Ctrl+Space keybinding if present
                    var existing = editor.InputBindings.OfType<KeyBinding>().FirstOrDefault(kb => kb.Key == Key.Space && kb.Modifiers == ModifierKeys.Control);
                    if (existing != null)
                    {
                        existing.Command = cmd;
                    }
                    else if (cmd != null)
                    {
                        editor.InputBindings.Add(new KeyBinding(cmd, Key.Space, ModifierKeys.Control));
                    }
                    else if (existing != null && cmd == null)
                    {
                        editor.InputBindings.Remove(existing);
                    }
                }
                catch
                {
                    // Best-effort: if InputBindings are unavailable for some reason, fall back to key handler only.
                }
            }
        }
        #endregion

        private static void EnsureHandlers(TextEditor editor)
        {
            if (!_states.TryGetValue(editor, out var _))
            {
                _states.Add(editor, new EditorState());

                editor.TextChanged += (s, e) =>
                {
                    var state = _states.GetOrCreateValue(editor);
                    if (state.SuppressUpdate) return;
                    try
                    {
                        state.SuppressUpdate = true;
                        SetEditorText(editor, editor.Text);
                        SetIsModified(editor, true);
                        // update selection and caret
                        SetSelectionStart(editor, editor.SelectionStart);
                        SetSelectionLength(editor, editor.SelectionLength);
                        if (editor.TextArea?.Caret != null)
                        {
                            SetCaretOffset(editor, editor.TextArea.Caret.Offset);
                        }
                    }
                    finally { state.SuppressUpdate = false; }
                };

                if (editor.TextArea?.Caret != null)
                {
                    editor.TextArea.Caret.PositionChanged += (s, e) =>
                    {
                        var state = _states.GetOrCreateValue(editor);
                        if (state.SuppressUpdate) return;
                        try
                        {
                            state.SuppressUpdate = true;
                            SetCaretOffset(editor, editor.TextArea.Caret.Offset);
                        }
                        finally { state.SuppressUpdate = false; }
                    };
                }

                // Selection updates when caret moves or text changes; however selections can change without moving the caret (e.g., user selects with mouse),
                // so listen to selection changed events when available.
                try
                {
                    editor.TextArea.SelectionChanged += (s, e) =>
                    {
                        var state = _states.GetOrCreateValue(editor);
                        if (state.SuppressUpdate) return;
                        try
                        {
                            state.SuppressUpdate = true;
                            SetSelectionStart(editor, editor.SelectionStart);
                            SetSelectionLength(editor, editor.SelectionLength);
                        }
                        finally { state.SuppressUpdate = false; }
                    };
                }
                catch
                {
                    // If SelectionChanged is not present for this version of AvalonEdit, it's a best-effort addition.
                }

                // Key handling for completion: Ctrl+Space
                editor.TextArea.PreviewKeyDown += (s, e) =>
                {
                    var pressedKey = e.Key == Key.System ? e.SystemKey : e.Key;
                    var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    if (ctrl && pressedKey == Key.Space)
                    {
                        var cmd = GetShowCompletionCommand(editor);
                        if (cmd != null && cmd.CanExecute(null))
                        {
                            cmd.Execute(null);
                            e.Handled = true;
                        }
                    }
                };

                // Ensure unloading cleans up state
                editor.Unloaded += (s, e) =>
                {
                    _states.Remove(editor);
                };
            }
        }
    }
}
