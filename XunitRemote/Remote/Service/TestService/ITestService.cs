using System;
using System.ServiceModel;
using XUnitRemote.Remote.Result;

namespace XUnitRemote.Remote.Service.TestService
{
    [ServiceContract]
    public interface ITestService
    {
        [OperationContract]
        [FaultContract(typeof(TestExecutionFault))]
        void RunTest(string assemblyPath, string typeName, string methodName);
    }

    public interface ITestRunner
    {
        void RunTest(string assemblyPath, string typeName, string methodName);
    }
}
