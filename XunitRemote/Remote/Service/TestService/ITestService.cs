using System;
using System.ServiceModel;
using System.Threading.Tasks;
using XUnitRemote.Remote.Result;

namespace XUnitRemote.Remote.Service.TestService
{
    [ServiceContract(CallbackContract = typeof(ITestResultNotificationService))]
    public interface ITestService
    {
        [OperationContract]
        [FaultContract(typeof(TestExecutionFault))]
        Task RunTest(string assemblyPath, string typeName, string methodName);
    }

    [ServiceContract]
    [ServiceKnownType(typeof(TestPassed))]
    [ServiceKnownType(typeof(TestFailed))]
    [ServiceKnownType(typeof(TestSkipped))]
    public interface ITestResultNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void TestFinished(ITestResult result);
    }

    public interface ITestRunner
    {
        Task RunTest(string assemblyPath, string typeName, string methodName);
    }
}
