using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace XUnitRemote.Remote.Service.TestService
{
    public class DefaultTestRunner : ITestRunner
    {
        private readonly Action<AppDomain> _Config;
        private readonly Uri _ResultNotificationUrl;

        public DefaultTestRunner(Action<AppDomain> config, Uri resultNotificationUrl)
        {
            _Config = config;
            _ResultNotificationUrl = resultNotificationUrl;
        }

        public void RunTest(string assemblyPath, string typeName, string methodName)
        {
            var runner = new AppDomainTestRunner(_ResultNotificationUrl, assemblyPath, typeName, methodName);
            var ad = AppDomain.CreateDomain("test-runner", AppDomain.CurrentDomain.Evidence, AppDomain.CurrentDomain.SetupInformation);
            _Config(ad);
            ad.DoCallBack(runner.RunSync);
            AppDomain.Unload(ad);
        }
    }
}
