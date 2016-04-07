namespace XUnitRemote.Remote.Result
{
    public interface ITestResult
    {
        string DisplayName { get; }
        decimal ExecutionTime { get; }
        string Output { get; }
    }
}