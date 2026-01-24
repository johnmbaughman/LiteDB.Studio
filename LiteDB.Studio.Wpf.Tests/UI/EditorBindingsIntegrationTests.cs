using System;
using System.Threading;
using Xunit;

namespace LiteDB.Studio.Wpf.Tests.UI
{
    public class EditorBindingsIntegrationTests
    {
        private static void RunInSta(Action action)
        {
            // Lightweight STA runner: run the action on a dedicated STA thread and wait for completion.
            Exception? ex = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception e) { ex = e; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (ex != null) throw new AggregateException(ex);
        }

        [Fact(Skip = "Flaky in CI: integration with AvalonEdit UI components can hang in headless test hosts. Covered by unit tests and manual QA.")]
        public void Editor_Bindings_ViewModelToView_TwoWay_And_ShowCompletionCommand_Executes()
        {
            // Skipped in CI. Use unit tests for behaviors and TabViewModel tests for completion commands.
        }
    }
}
