using System.ServiceModel;
using System.Threading.Tasks;

namespace XUnitRemote.Remote.Service
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class TestService : ITestService
    {
        private readonly ITestService _Runner;

        public TestService(ITestService runner)
        {
            _Runner = runner;
        }

        public Task RunTest(string assemblyPath, string typeName, string methodName)
        {
            return _Runner.RunTest(assemblyPath, typeName, methodName);
        }
    }
}
