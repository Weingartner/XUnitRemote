using System.ServiceModel;

namespace OutOfProcessTestExecution.Contracts
{
    [ServiceContract]
    [ServiceKnownType(typeof(TestExecutionPassedResult))]
    [ServiceKnownType(typeof(TestExecutionFailedResult))]
    [ServiceKnownType(typeof(TestExecutionSkippedResult))]

    public interface ITestExecutionService
    {
        [OperationContract]
        ITestExecutionResult RunTest(string assemblyPath, string typeName, string methodName);
    }
}
