using System;
using System.Linq;
using System.Net.Configuration;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using EnvDTE;
using Debugger = System.Diagnostics.Debugger;
using Polly;

namespace XUnitRemote
{
    public static class RemoteDebugger
    {
        public static IDisposable CascadeDebugging(int pid)
        {
            return Debugger.IsAttached ? Attach(pid) : Disposable.Empty;
        }

        public static TR Retry<TR>(Func<TR> fn)
        {
            var result = Policy.Handle<Exception>()
                .WaitAndRetry(50, i => TimeSpan.FromMilliseconds(10 * i))
                .ExecuteAndCapture(fn);

            if (result.Outcome == OutcomeType.Successful)
                return result.Result;

            throw result.FinalException;

        }

        public static void Retry(Action fn)
        {
            Retry(()=>{ fn(); return 0; });
        }


        public static IDisposable Attach(int pid)
        {
            try
            {
                MessageFilter.Register();

                var process = Retry(() => VisualStudio.GetProcess(pid));
                if(process==null)
                    throw new DebuggerException($"Pid {pid} not found. Can't start debugger");

                if (!Retry(()=> process.IsAttached()))
                {
                    Retry(process.Attach);

                    if(!Retry(process.IsAttached))
                        throw new DebuggerException();

                    return Disposable.Create(() =>
                    {
                        if (Retry(process.IsAttached))
                            Retry(() => process.Detach(true));
                    });
                }

                // If the debugger was already connected then don't detach it.
                return Disposable.Empty;

            }
            catch (COMException e)
            {
                throw new DebuggerException(e);
            }
        }
    }

    public class DebuggerException : Exception
    {
        public DebuggerException(COMException comException):base("Can't attach debugger",comException)
        {
        }
        public DebuggerException(string msg = null):base(msg ?? "Can't attach debugger")
        {
        }
    }
}