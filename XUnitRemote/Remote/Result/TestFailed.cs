using System;
using System.Runtime.Serialization;

namespace XUnitRemote.Remote.Result
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

        public TestFailed(string displayName, decimal executionTime, string output, string[] exceptionTypes, string[] exceptionMessages, string[] exceptionStackTraces)
        {
            DisplayName = displayName;
            ExecutionTime = executionTime;
            Output = output;
            ExceptionTypes = exceptionTypes;
            ExceptionMessages = exceptionMessages;
            ExceptionStackTraces = exceptionStackTraces;
        }
    }
}