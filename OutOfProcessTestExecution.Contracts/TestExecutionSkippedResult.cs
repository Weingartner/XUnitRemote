using System.Runtime.Serialization;

namespace OutOfProcessTestExecution.Contracts
{
    [DataContract]
    public class TestExecutionSkippedResult : ITestExecutionResult
    {
        [DataMember] public string SkipReason { get; }

        public TestExecutionSkippedResult(string skipReason)
        {
            SkipReason = skipReason;
        }
    }
}