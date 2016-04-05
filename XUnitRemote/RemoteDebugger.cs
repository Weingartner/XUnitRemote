using System;
using System.Linq;
using System.Net.Configuration;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using EnvDTE;
using Debugger = System.Diagnostics.Debugger;

namespace XUnitRemote
{
    public static class RemoteDebugger
    {
        public static IDisposable CascadeDebugging(int pid)
        {
            return IsDebuggerAttached() ? Attach(pid) : Disposable.Empty;
        }


        public static IDisposable Attach(int pid)
        {
            try
            {
                MessageFilter.Register();

                var process = Process(pid);

                if(process==null)
                    throw new DebuggerException($"Pid {pid} not found. Can't start debugger");

                process.Attach();

                if(!IsDebuggerAttached(process.ProcessID))
                    throw new DebuggerException();

                return Disposable.Create(() => Detach(process));
            }
            catch (COMException e)
            {
                throw new DebuggerException(e);
            }
        }

        private static Process Process(int pid)
        {
            return Dte
                .Debugger
                .LocalProcesses
                .Cast<Process>()
                .SingleOrDefault(x => x.ProcessID == pid);
        }

        public static void Detach(int pid)
        {
            Detach(Process(pid));
        }

        private static void Detach(Process p)
        {
            try
            {
                if (IsDebuggerAttached(p.ProcessID))
                    p.Detach();
            }
            catch (Exception e)
            {
            }
        }

        private static bool IsDebuggerAttached()
        {
            return IsDebuggerAttached(System.Diagnostics.Process.GetCurrentProcess().Id);
        }

        public static bool IsDebuggerAttached(int pid) => Dte
            .Debugger
            .DebuggedProcesses
            .Cast<Process>()
            .Any(p => p.ProcessID == pid);


        /// <summary>
        /// Update this when you upgrade visual studio
        /// </summary>
        private const string VisualStudioVersion = "14";
        private static string VisualStudioComId => $"VisualStudio.DTE.{VisualStudioVersion}.0";

        /// <summary>
        /// Get the visual studio DTE object
        /// </summary>
        private static DTE Dte => (DTE) Marshal.GetActiveObject(VisualStudioComId);

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