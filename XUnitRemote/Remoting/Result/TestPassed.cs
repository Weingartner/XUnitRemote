using System.Runtime.Serialization;

namespace XUnitRemote.Remoting.Result
{
    [DataContract]
    public class TestPassed : ITestResult
    {
        [DataMember] public decimal ExecutionTime { get; private set; }
        [DataMember] public string Output { get; private set; }

        public TestPassed(decimal executionTime, string output)
        {
            ExecutionTime = executionTime;
            Output = output;
        }
    }
}