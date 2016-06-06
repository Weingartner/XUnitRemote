using System;
using System.ServiceModel;
using System.Threading.Tasks;
using XUnitRemote.Remote.Result;

namespace XUnitRemote.Remote.Service.TestService
{
    [ServiceContract]
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
