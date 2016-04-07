using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using XUnitRemote.Remoting.Result;

namespace XUnitRemote.Remoting.Service
{
    [ServiceContract(CallbackContract = typeof(ITestResultNotificationService))]
    public interface ITestService
    {
        [OperationContract]
        [FaultContract(typeof(TestExecutionFault))]
        void RunTest(string assemblyPath, string typeName, string methodName);
    }

    [ServiceKnownType(typeof(TestPassed))]
    [ServiceKnownType(typeof(TestFailed))]
    public interface ITestResultNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void TestFinished(ITestResult result);
    }
}
