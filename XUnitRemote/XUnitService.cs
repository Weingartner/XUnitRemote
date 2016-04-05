using System;
using System.Reactive;
using System.Reactive.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SolidworksAddinFramework;
using XUnitRemote.Remoting.Service;

namespace XUnitRemote
{
    public static class XUnitService
    {
        public static async Task Start(string id, TimeSpan? timeout = null)
        {

            using (var host = new ServiceHost(typeof (TestService)))
            {
                var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                var address = Address(id);
                host.AddServiceEndpoint(typeof (ITestService), binding, address);
                host.Open();
                await Task.Delay(timeout ?? Timeout.InfiniteTimeSpan);
            }

        }

        public static readonly Uri BaseUrl = new Uri("net.pipe://localhost/weingartner/XUnitRemoteTestService/");

        public static Uri Address(string id)
        {
            return new Uri(BaseUrl, id);
        }
    }
}
