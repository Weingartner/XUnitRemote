using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

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

        public void RunTest(string assemblyPath, string typeName, string methodName)
        {
            _Runner.RunTest(assemblyPath, typeName, methodName);
        }
    }
}
