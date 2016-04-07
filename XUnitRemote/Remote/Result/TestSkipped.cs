using System;
using System.Runtime.Serialization;

namespace XUnitRemote.Remote.Result
{
    [Serializable]
    [DataContract]
    public class TestSkipped : ITestResult
    {
        [DataMember] public string DisplayName { get; private set; }
        [DataMember] public string SkipReason { get; private set; }
        [DataMember] public decimal ExecutionTime { get; private set; } = 0m;
        [DataMember] public string Output { get; private set; } = null;

        public TestSkipped(string displayName, string skipReason)
        {
            DisplayName = displayName;
            SkipReason = skipReason;
        }
    }
}