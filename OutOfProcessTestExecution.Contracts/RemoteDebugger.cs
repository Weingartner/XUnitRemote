using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;

namespace XUnitRemote
{
    public static class RemoteDebugger
    {

        public static void Launch(int pid)
        {
            var dte = (DTE)Marshal.GetActiveObject("VisualStudio.DTE.14.0");
            MessageFilter.Register();

            var processes = dte.Debugger.LocalProcesses.OfType<Process>();
            var process = processes.SingleOrDefault(x => x.ProcessID == pid);
            process?.Attach();
        }
    }
}