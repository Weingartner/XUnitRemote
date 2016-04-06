using System;

namespace XUnitRemote.Remoting.Result
{
    public interface ITestResult
    {
        string DisplayName { get; }
        decimal ExecutionTime { get; }
        string Output { get; }
    }
}