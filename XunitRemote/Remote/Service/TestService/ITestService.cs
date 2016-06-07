using System;
using System.ServiceModel;
using System.Threading.Tasks;
using XUnitRemote.Remote.Result;
using XUnitRemote.Remote.Service.TestResultNotificationService;

namespace XUnitRemote.Remote.Service.TestService
{
    [ServiceContract(CallbackContract = typeof(ITestResultNotificationService))]
    public interface ITestService
    {
        [OperationContract]
        [FaultContract(typeof(TestExecutionFault))]
        Task RunTest(string assemblyPath, string typeName, string methodName);
    }

    public interface ITestRunner
    {
        Task RunTest(string assemblyPath, string typeName, string methodName);
    }
}
