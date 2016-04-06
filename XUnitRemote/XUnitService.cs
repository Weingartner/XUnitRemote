// Decompiled with JetBrains decompiler
// Type: XUnitRemote.XUnitService
// Assembly: XUnitRemote, Version=1.0.0.7, Culture=neutral, PublicKeyToken=null
// MVID: CAA63DC1-C4FB-455B-A4F1-1665E004B540
// Assembly location: C:\Users\phelan\workspace\WeinCadSW\swcsharpmf\XUnit.Solidworks.Addin\bin\Debug\XUnitRemote.dll

using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using XUnitRemote.Remoting.Result;
using XUnitRemote.Remoting.Service;
using Binding = System.ServiceModel.Channels.Binding;

namespace XUnitRemote
{
    public static class XUnitService
    {
        public static readonly Uri BaseUrl = new Uri("net.pipe://localhost/weingartner/XUnitRemoteTestService/");

        public static async Task Start(string id, Func<Func<ITestResult>,ITestResult> marshaller=null, TimeSpan? timeout = null)
        {
            // Use a default dispatcher if none is provided
            marshaller = marshaller ?? (f => f());

            using (var host = new ServiceHost(new TestDispatcher(marshaller), Array.Empty<Uri>()))
            {
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                Uri address = Address(id);
                host.AddServiceEndpoint(typeof (ITestService), (Binding) binding, address);

                host.Open();
                await Task.Delay(timeout ?? Timeout.InfiniteTimeSpan);
                binding = (NetNamedPipeBinding) null;
                address = (Uri) null;
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

        private readonly Func<Func<ITestResult>,ITestResult> _Marshaller;
        private readonly ITestService _Service = new TestService();

        public TestDispatcher(Func<Func<ITestResult>,ITestResult> marshaller)
        {
            _Marshaller = marshaller;
        }

        ITestResult ITestService.RunTest(string assemblyPath, string typeName, string methodName) => 
            _Marshaller(() => IsolatedTestRunner.Run(assemblyPath, typeName, methodName));

        private static void Callback(string assemblyPath, string typeName, string methodName)
        {
            var service = new TestService();
            var r = service.RunTest(assemblyPath, typeName, methodName);
            AppDomain.CurrentDomain.SetData("Result", r);
        }
    }

    [Serializable]
    public class IsolatedTestRunner : IsolateBase<ITestResult>
    {
        private readonly string _AssemblyPath;
        private readonly string _TypeName;
        private readonly string _MethodName;

        public IsolatedTestRunner(string assemblyPath, string typeName, string methodName):base("xunit.single.test")
        {
            _AssemblyPath = assemblyPath;
            _TypeName = typeName;
            _MethodName = methodName;
        }

        protected override ITestResult RunImpl() => new TestService().RunTest(_AssemblyPath, _TypeName, _MethodName);

        public static ITestResult Run(string assemblyPath, string typeName, string methodName)
        {
            var i = new IsolatedTestRunner(assemblyPath, typeName, methodName);
            return i.Run();
        }
    }
}
