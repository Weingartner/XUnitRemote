// Decompiled with JetBrains decompiler
// Type: XUnitRemote.XUnitService
// Assembly: XUnitRemote, Version=1.0.0.7, Culture=neutral, PublicKeyToken=null
// MVID: CAA63DC1-C4FB-455B-A4F1-1665E004B540
// Assembly location: C:\Users\phelan\workspace\WeinCadSW\swcsharpmf\XUnit.Solidworks.Addin\bin\Debug\XUnitRemote.dll

using System;
using System.Collections.Generic;
using System.Reactive;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using XUnitRemote.Remote.Isolation;
using XUnitRemote.Remote.Service;

namespace XUnitRemote.Local
{
    public static class XUnitService
    {
        public static readonly Uri BaseUrl = new Uri("net.pipe://localhost/weingartner/XUnitRemoteTestService/");
        public static Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Start the XUnit WCF service running.
        /// </summary>
        /// <param name="id">The unique id to assign to this service</param>
        /// <param name="isolateInDomain">Isolate each test in it's own AppDomain</param>
        /// <param name="marshaller">A synchronous function that may execute the test on a different dispatcher and wait for the result. Usefull for tests
        ///     where the code must be run on a specific dispatcher</param>
        /// <param name="data">A dictionary of data that will be assigned to XUnitService.Data in the child app domains. All objects
        ///     must be serializable</param>
        /// <param name="timeout">How long the service runs</param>
        /// <returns></returns>
        public static async Task Start(string id, bool isolateInDomain, Action<Action> marshaller = null, Dictionary<string, object> data = null, TimeSpan? timeout = null)
        {
            // Use a default dispatcher if none is provided
            marshaller = marshaller ?? (f => f());

            using (var host = new ServiceHost(new TestDispatcher(marshaller, data, isolateInDomain)))
            {
                var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                var address = Address(id);
                host.AddServiceEndpoint(typeof (ITestService), binding, address);

                host.Open();
                await Task.Delay(timeout ?? Timeout.InfiniteTimeSpan);
            }
        }

        public static Uri Address(string id)
        {
            return new Uri(BaseUrl, id);
        }
    }


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TestDispatcher : ITestService
    {
        public Dictionary<string, object> Data { get; }

        private readonly Action<Action> _Marshaller;
        private readonly bool _IsolateInDomain;

        public TestDispatcher(Action<Action> marshaller, Dictionary<string, object> data, bool isolateInDomain = true)
        {
            Data = data;
            _Marshaller = marshaller;
            _IsolateInDomain = isolateInDomain;
        }

        void ITestService.RunTest(string assemblyPath, string typeName, string methodName) => 
            _Marshaller(() => IsolatedTestRunner.Run(assemblyPath, typeName, methodName, Data, _IsolateInDomain));
    }

    [Serializable]
    public class IsolatedTestRunner : IsolateBase<Unit>
    {
        private readonly string _AssemblyPath;
        private readonly string _TypeName;
        private readonly string _MethodName;
        private readonly Dictionary<string, object> _Data;

        public IsolatedTestRunner(string assemblyPath, string typeName, string methodName, Dictionary<string, object> data):base("xunit.single.test")
        {
            _AssemblyPath = assemblyPath;
            _TypeName = typeName;
            _MethodName = methodName;
            _Data = data;
        }

        protected override Unit RunImpl()
        {
            XUnitService.Data = _Data;
            new TestService().RunTest(_AssemblyPath, _TypeName, _MethodName);
            return Unit.Default;
        }

        public static void Run(string assemblyPath, string typeName, string methodName, Dictionary<string, object> data, bool isolateInDomain)
        {
            if (isolateInDomain)
            {
                new IsolatedTestRunner(assemblyPath, typeName, methodName, data).Run();
            }

            XUnitService.Data = data;
            new TestService().RunTest(assemblyPath, typeName, methodName);
        }
    }
}
