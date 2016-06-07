using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using XUnitRemote.Remote.Service.TestResultNotificationService;

namespace XUnitRemote.Remote.Service.TestService
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class DefaultTestService : ITestService
    {
        private readonly ITestRunner _Runner;

        public DefaultTestService(ITestRunner runner)
        {
            _Runner = runner;
        }

        public Task RunTest(string assemblyPath, string typeName, string methodName)
        {
            return _Runner.RunTest(assemblyPath, typeName, methodName);
        }
    }
}
