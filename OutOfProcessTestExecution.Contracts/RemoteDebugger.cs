using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;

namespace XUnitRemote
{
    public class RemoteDebugger
    {

        public static void Launch(int pid)
        {
            var dte = (DTE)Marshal.GetActiveObject("VisualStudio.DTE.14.0");
            MessageFilter.Register();

            IEnumerable<Process> processes = dte.Debugger.LocalProcesses.OfType<Process>();
            var process = processes.SingleOrDefault(x => x.ProcessID == pid);
            if (process != null)
            {
                process.Attach();
            }
        }
    }
}