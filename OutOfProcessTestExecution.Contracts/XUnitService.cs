using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using XUnitRemote.Remoting.Service;

namespace XUnitRemote
{
    public static class XUnitService
    {
        public static async Task Start(string id,TimeSpan? timeout = null)
        {
            await Task.Run(async () =>
            {
                using (var host = new ServiceHost(typeof (TestService)))
                {
                    var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                    var address = Address(id);
                    host.AddServiceEndpoint(typeof (ITestService), binding, address);
                    host.Open();
                    if (timeout == null)
                        await Task.Delay(-1);
                    else
                        await Task.Delay(timeout??TimeSpan.MaxValue);
                }
            });
        }

        public static string _BaseUrl = "net.pipe://localhost/weingartner/XUnitRemoteTestService/";

        public static Uri Address(string id)
        {
            return new Uri(_BaseUrl + id);
        }
    }
}
