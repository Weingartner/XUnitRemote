using System.Runtime.Serialization;

namespace OutOfProcessTestExecution.Contracts
{
    [DataContract]
    public class TestExecutionPassedResult : ITestExecutionResult
    {
        [DataMember] public decimal ExecutionTime { get; }
        [DataMember] public string Output { get; }

        public TestExecutionPassedResult(decimal executionTime, string output)
        {
            ExecutionTime = executionTime;
            Output = output;
        }
    }
}