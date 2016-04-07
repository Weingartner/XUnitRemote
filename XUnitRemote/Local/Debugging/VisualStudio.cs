using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;

namespace XUnitRemote.Local.Debugging
{
    public static class VisualStudio
    {
        #region getting the dte
        /// <summary>
        /// Copied from http://stackoverflow.com/a/14205934/158285
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<DTE> GetInstances()
        {
            IRunningObjectTable rot;
            IEnumMoniker enumMoniker;
            int retVal = GetRunningObjectTable(0, out rot);

            if (retVal == 0)
            {
                rot.EnumRunning(out enumMoniker);

                IntPtr fetched = IntPtr.Zero;
                IMoniker[] moniker = new IMoniker[1];
                while (enumMoniker.Next(1, moniker, fetched) == 0)
                {
                    IBindCtx bindCtx;
                    CreateBindCtx(0, out bindCtx);
                    string displayName;
                    moniker[0].GetDisplayName(bindCtx, null, out displayName);
                    Console.WriteLine("Display Name: {0}", displayName);
                    bool isVisualStudio = displayName.StartsWith("!VisualStudio");
                    if (isVisualStudio)
                    {
                        object obj;
                        rot.GetObject(moniker[0], out obj);
                        var dte = obj as DTE;
                        yield return dte;
                    }
                }
            }
        }

        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
        #endregion

        /// <summary>
        /// Is the specific visual studio instance attached to the specific process
        /// </summary>
        /// <param name="dte"></param>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static bool IsDebuggerAttached(DTE dte, int pid) => dte
            .Debugger
            .DebuggedProcesses
            .Cast<Process>()
            .Any(p => p.ProcessID == pid);

        /// <summary>
        /// Get the visual studio instance that is attached to the specified pid. 
        /// </summary>
        /// <param name="pid"></param>
        /// <returns>the visual studio instance or null</returns>
        public static DTE GetDebugger(int pid) =>
            GetInstances().FirstOrDefault(dte => IsDebuggerAttached(dte, pid));

        /// <summary>
        /// Get the visual studio DTE object
        /// </summary>
        public static DTE GetDebuggerOfCurrentProcess() => GetDebugger(System.Diagnostics.Process.GetCurrentProcess().Id);

        /// <summary>
        /// Is a visual studio instance attached to the current process?
        /// </summary>
        /// <returns></returns>
        public static bool IsDebuggingCurrentProcess() => GetDebuggerOfCurrentProcess() != null;
        /// <summary>
        /// Is a visual studio instance attached to the process with the specified pid
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static bool IsDebugging(int pid) => GetDebugger(pid) != null;

        /// <summary>
        /// Look up process with specific id in process list of current debugger.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static Process GetProcess(int pid)
        {
            return GetDebuggerOfCurrentProcess()
                .Debugger
                .LocalProcesses
                .Cast<Process>()
                .SingleOrDefault(x => x.ProcessID == pid);
        }
    }

    public static class VisualStudioProcessExtensions
    {
        public static bool HasDebuggerAttached(this Process p)
        {
            return VisualStudio.IsDebugging(p.ProcessID);
        }
        
    }
}
