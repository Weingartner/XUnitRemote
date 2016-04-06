using System;
using System.Runtime.Serialization;

namespace XUnitRemote.Remoting.Result
{
    [DataContract]
    [Serializable]
    public class TestSkipped : ITestResult
    {
        [DataMember] public string SkipReason { get; private set; }

        public TestSkipped(string skipReason)
        {
            SkipReason = skipReason;
        }
    }
}