using System.Runtime.Serialization;

namespace XUnitRemote.Remoting.Result
{
    [DataContract]
    public class TestSkipped : ITestResult
    {
        [DataMember] public string SkipReason { get; private set; }

        public TestSkipped(string skipReason)
        {
            SkipReason = skipReason;
        }
    }
}