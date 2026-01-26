using System;
using System.Threading;
using Xunit;
using ICSharpCode.AvalonEdit;
using System.Windows.Threading;
using System.Linq;
using LiteDB.Studio.Wpf.Behaviors;

namespace LiteDB.Studio.Wpf.Tests.Controls
{
    public class AvalonEditBehaviorsTests
    {
        private static void RunInSta(Action action)
        {
            var mre = new ManualResetEventSlim(false);
            Exception? ex = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        try { action(); } catch (Exception e) { ex = e; }
                        finally { frame.Continue = false; }
                    });
                    Dispatcher.PushFrame(frame);
                }
                finally
                {
                    // Shutdown dispatcher
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                    mre.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            mre.Wait();
            if (ex != null) throw new AggregateException(ex);
        }

        [Fact]
        public void EditorTextTwoWayBinding_PreservesTextAndIsModifiedState()
        {
            RunInSta(() =>
            {
                var editor = new TextEditor();
                // Programmatic set via attached property
                AvalonEditBehaviors.SetEditorText(editor, "hello");
                // Attach handlers by setting another property
                AvalonEditBehaviors.SetCaretOffset(editor, 0);

                Assert.Equal("hello", editor.Text);
                Assert.False(AvalonEditBehaviors.GetIsModified(editor));

                // Simulate user typing
                editor.AppendText(" world");
                // AvalonEdit raises TextChanged synchronously
                Assert.Equal("hello world", AvalonEditBehaviors.GetEditorText(editor));
                Assert.True(AvalonEditBehaviors.GetIsModified(editor));
            });
        }

        [Fact]
        public void CaretOffset_PropertyReflectsCaretPosition()
        {
            RunInSta(() =>
            {
                var editor = new TextEditor();
                AvalonEditBehaviors.SetEditorText(editor, "0123456789");
                // Ensure handlers attached
                AvalonEditBehaviors.SetSelectionStart(editor, 0);

                editor.TextArea.Caret.Offset = 3;
                // PositionChanged occurs immediately, dispatcher may process it
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

                Assert.Equal(3, AvalonEditBehaviors.GetCaretOffset(editor));

                // Now set caret via attached property
                AvalonEditBehaviors.SetCaretOffset(editor, 7);
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
                Assert.Equal(7, editor.TextArea.Caret.Offset);
            });
        }

        [Fact]
        public void Selection_TwoWayPropagation()
        {
            RunInSta(() =>
            {
                var editor = new TextEditor();
                AvalonEditBehaviors.SetEditorText(editor, "abcdefghij");
                // Programmatic selection via attached properties
                AvalonEditBehaviors.SetSelectionStart(editor, 2);
                AvalonEditBehaviors.SetSelectionLength(editor, 4);
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

                Assert.Equal(2, editor.SelectionStart);
                Assert.Equal(4, editor.SelectionLength);

                // Now make a user selection and ensure attached properties reflect it
                editor.Select(5, 2);
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

                Assert.Equal(5, AvalonEditBehaviors.GetSelectionStart(editor));
                Assert.Equal(2, AvalonEditBehaviors.GetSelectionLength(editor));
            });
        }

        [Fact]
        public void ShowCompletionCommand_IsBoundAndExecutableViaKeyBinding()
        {
            RunInSta(() =>
            {
                var editor = new TextEditor();
                var executed = false;
                var cmd = new TestCommand(() => executed = true);

                AvalonEditBehaviors.SetShowCompletionCommand(editor, cmd);
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

                // Ensure a KeyBinding for Ctrl+Space exists
                var kb = editor.InputBindings.OfType<System.Windows.Input.KeyBinding>().FirstOrDefault(k => k is { Key: System.Windows.Input.Key.Space, Modifiers: System.Windows.Input.ModifierKeys.Control });
                Assert.NotNull(kb);

                // Execute the bound command to simulate the input trigger
                kb.Command.Execute(null);
                Assert.True(executed);
            });
        }

        private class TestCommand(Action action) : System.Windows.Input.ICommand
        {
            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => action();
        }
    }
}
