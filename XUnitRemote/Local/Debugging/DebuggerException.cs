using System;

namespace XUnitRemote.Local.Debugging
{
    public class DebuggerException : Exception
    {
        public DebuggerException(Exception innerException)
            : base("Can't attach debugger", innerException)
        {
        }

        public DebuggerException(string msg = null)
            : base(msg ?? "Can't attach debugger")
        {
        }
    }
}