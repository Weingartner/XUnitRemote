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

namespace XUnitRemote.Remote.Service.TestService
{
    public class DefaultTestRunner : ITestRunner
    {
        private readonly Uri _ResultNotificationUrl;

        public DefaultTestRunner(Uri resultNotificationUrl)
        {
            _ResultNotificationUrl = resultNotificationUrl;
        }

        public async Task RunTest(string assemblyPath, string typeName, string methodName)
        {
            var runner = new IsolatedTestRunner(_ResultNotificationUrl, assemblyPath, typeName, methodName);
            await runner.Run();
        }
    }
}
