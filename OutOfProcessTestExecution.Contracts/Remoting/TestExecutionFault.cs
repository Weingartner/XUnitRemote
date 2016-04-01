using System.Runtime.Serialization;

namespace XUnitRemote.Remoting
{
    [DataContract]
    public class TestExecutionFault
    {
        [DataMember]
        public string Message { get; }

        public TestExecutionFault(string message)
        {
            Message = message;
        }
    }
}