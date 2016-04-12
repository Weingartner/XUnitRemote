using System.ServiceModel;
using System.Threading.Tasks;
using XUnitRemote.Remote.Result;

namespace XUnitRemote.Remote.Service
{
    [ServiceContract(CallbackContract = typeof(ITestResultNotificationService))]
    public interface ITestService
    {
        [OperationContract]
        [FaultContract(typeof(TestExecutionFault))]
        Task RunTest(string assemblyPath, string typeName, string methodName);
    }

    [ServiceKnownType(typeof(TestPassed))]
    [ServiceKnownType(typeof(TestFailed))]
    public interface ITestResultNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void TestFinished(ITestResult result);
    }
}
