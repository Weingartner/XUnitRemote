using System;
using System.Runtime.Serialization;

namespace XUnitRemote.Remote.Result
{
    [DataContract]
    [Serializable]
    public class TestPassed : ITestResult
    {
        [DataMember] public string DisplayName { get; private set; }
        [DataMember] public decimal ExecutionTime { get; private set; }
        [DataMember] public string Output { get; private set; }

        public TestPassed(string displayName, decimal executionTime, string output)
        {
            DisplayName = displayName;
            ExecutionTime = executionTime;
            Output = output;
        }
    }
}