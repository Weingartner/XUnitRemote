using System;
using System.Runtime.Serialization;

namespace XUnitRemote.Remoting.Result
{
    [Serializable]
    [DataContract]
    public class TestFailed : ITestResult
    {
        [DataMember] public string DisplayName { get; private set; }
        [DataMember] public decimal ExecutionTime { get; private set; }
        [DataMember] public string Output { get; private set; }
        [DataMember] public string[] ExceptionTypes { get; private set; }
        [DataMember] public string[] ExceptionMessages { get; private set; }
        [DataMember] public string[] ExceptionStackTraces { get; private set; }

        public TestFailed(string displayName, decimal executionTime, string output, string exceptionType, string exceptionMessage, string exceptionStackTrace)
        {
            DisplayName = displayName;
            ExecutionTime = executionTime;
            Output = output;
            ExceptionTypes = new[] { exceptionType };
            ExceptionMessages = new[] { exceptionMessage };
            ExceptionStackTraces = new[] { exceptionStackTrace };
        }
    }
}