using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Polly;
using Debugger = System.Diagnostics.Debugger;

namespace XUnitRemote.Local.Debugging
{
    public static class RemoteDebugger
    {
        public static IDisposable CascadeDebugging(int pid)
        {
            return Debugger.IsAttached ? Attach(pid) : Disposable.Empty;
        }

        private static TR Retry<TR>(Func<TR> fn)
        {
            var result = Policy.Handle<Exception>()
                .WaitAndRetry(50, i => TimeSpan.FromMilliseconds(10 * i))
                .ExecuteAndCapture(fn);

            if (result.Outcome == OutcomeType.Successful)
                return result.Result;

            throw result.FinalException;

        }

        private static void Retry(Action fn)
        {
            Retry(() => { fn(); return 0; });
        }

        public static IDisposable Attach(int pid)
        {
            try
            {
                using (MessageFilter.Register())
                {
                    var process = Retry(() => VisualStudio.GetProcess(pid));
                    if (process == null)
                        throw new DebuggerException($"Pid {pid} not found. Can't start debugger");

                    if (!Retry(() => process.HasDebuggerAttached()))
                    {
                        Retry(process.Attach);

                        if (!Retry(process.HasDebuggerAttached))
                            throw new DebuggerException();

                        // If we attached the debugger we should also detach it
                        return Disposable.Create(() =>
                        {
                            using (MessageFilter.Register())
                            {
                                if (Retry(process.HasDebuggerAttached))
                                {
                                Retry(() => process.Detach(WaitForBreakOrEnd: false));
                                }
                            }
                        });
                    }

                    // If the debugger was already attached then don't detach it
                    return Disposable.Empty;
                }
            }
            catch (COMException e)
            {
                throw new DebuggerException(e);
            }
        }
    }
}