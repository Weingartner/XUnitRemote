using System.ServiceModel;
using XUnitRemote.Remoting.Result;

namespace XUnitRemote.Remoting.Service
{
    [ServiceContract]
    [ServiceKnownType(typeof(TestPassed))]
    [ServiceKnownType(typeof(TestFailed))]
    [ServiceKnownType(typeof(TestSkipped))]

    public interface ITestService
    {
        [OperationContract]
        [FaultContract(typeof(TestExecutionFault))]
        ITestResult RunTest(string assemblyPath, string typeName, string methodName);
    }
}
