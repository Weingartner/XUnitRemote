using System.Runtime.Serialization;

namespace OutOfProcessTestExecution.Contracts
{
    [DataContract]
    public class TestExecutionFailedResult : ITestExecutionResult
    {
        [DataMember] public decimal ExecutionTime { get; }
        [DataMember] public string Output { get; }
        [DataMember] public string[] ExceptionTypes { get; }
        [DataMember] public string[] ExceptionMessages { get; }
        [DataMember] public string[] ExceptionStackTraces { get; }

        public TestExecutionFailedResult(decimal executionTime, string output, string exceptionType, string exceptionMessage, string exceptionStackTrace)
        {
            ExecutionTime = executionTime;
            Output = output;
            ExceptionTypes = new[] { exceptionType };
            ExceptionMessages = new[] { exceptionMessage };
            ExceptionStackTraces = new[] { exceptionStackTrace };
        }
    }
}