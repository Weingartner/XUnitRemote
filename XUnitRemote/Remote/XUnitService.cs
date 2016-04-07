using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.ServiceModel;
using Ninject;
using Ninject.Extensions.Factory;
using Ninject.Extensions.Wcf;
using Ninject.Extensions.Wcf.SelfHost;
using Ninject.Modules;
using Ninject.Web.Common.SelfHost;
using XUnitRemote.Remote.Service;

namespace XUnitRemote.Remote
{
    public static class XUnitService
    {
        public static readonly Uri BaseUrl = new Uri("net.pipe://localhost/weingartner/XUnitRemoteTestService/");

        public static IReadOnlyDictionary<string, object> Data { get; private set; }

        private static IKernel GetKernel()
        {
            var kernel = new StandardKernel();
            EnsureModuleIsLoaded<FuncModule>(kernel);
            return kernel;
        }

        private static void EnsureModuleIsLoaded<T>(IKernel kernel)
            where T : INinjectModule, new()
        {
            if (!kernel.GetModules().OfType<T>().Any())
            {
                kernel.Load<T>();
            }
        }

        /// <summary>
        /// Start the xUnit WCF service and use the default runner that simply executes the tests.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IDisposable StartWithDefaultRunner(TestServiceConfiguration config)
        {
            var kernel = GetKernel();
            kernel.Bind<ITestService>()
                .To<DefaultTestRunner>()
                .WhenInjectedInto<TestService>()
                .InTransientScope();
            return Start(config, kernel);
        }

        /// <summary>
        /// Start the xUnit WCF service and use a custom runner.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="createRunner">Callback to get a custom runner instance.</param>
        /// <returns></returns>
        public static IDisposable StartWithCustomRunner(TestServiceConfiguration config, Func<ITestService> createRunner)
        {
            var kernel = GetKernel();
            kernel.Bind<ITestService>()
                .ToMethod(ctx => createRunner())
                .WhenInjectedInto<TestService>()
                .InTransientScope();
            return Start(config, kernel);
        }

        private static IDisposable Start(TestServiceConfiguration config, IKernel kernel)
        {
            Data = config.Data;

            kernel.Bind<ITestResultNotificationService>()
                .ToMethod(ctx => OperationContext.Current.GetCallbackChannel<ITestResultNotificationService>());

            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { SendTimeout = TimeSpan.FromSeconds(10) };
            var address = new Uri(BaseUrl, config.Id);
            var wcfConfig = NinjectWcfConfiguration.Create<TestService, NinjectServiceSelfHostFactory>(
                h => h.AddServiceEndpoint(typeof(ITestService), binding, address));

            var host = new NinjectSelfHostBootstrapper(() => kernel, wcfConfig);
            host.Start();
            return Disposable.Create(() => host.Dispose());
        }
    }
}