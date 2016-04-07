using System.Runtime.Serialization;

namespace XUnitRemote.Remote.Result
{
    [DataContract]
    public class TestExecutionFault
    {
        [DataMember] public string Message { get; }

        public TestExecutionFault(string message)
        {
            Message = message;
        }
    }
}