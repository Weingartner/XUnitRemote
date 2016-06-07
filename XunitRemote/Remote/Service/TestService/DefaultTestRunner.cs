using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;
using XUnitRemote.Remote.Service.TestResultNotificationService;

namespace XUnitRemote.Remote.Service.TestService
{
    public class DefaultTestRunner : ITestRunner
    {
        private readonly Func<ITestResultNotificationService> _CreateNotificationService;

        public DefaultTestRunner(Func<ITestResultNotificationService> createNotificationService)
        {
            _CreateNotificationService = createNotificationService;
        }

        public async Task RunTest(string assemblyPath, string typeName, string methodName)
        {
            var testResultNotificationService = _CreateNotificationService();
            var runner = new IsolatedTestRunner(assemblyPath, typeName, methodName, testResultNotificationService.TestFinished);
            await runner.Run();
        }
    }
}
