using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OutOfProcessTestExecution.Contracts;
using Xunit;
using Xunit.Runners;

namespace SampleProcess
{
    public static class Program
    {
        private static void Main()
        {
            try
            {
                Run().Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        private static async Task Run()
        {
            Debugger.Launch();
            using (var host = new ServiceHost(typeof (TestExecutionService)))
            {
                var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                var address = new Uri("net.pipe://localhost/weingartner/TestExecutionService");
                host.AddServiceEndpoint(typeof(ITestExecutionService), binding, address);
                host.Open();
                await Task.Delay(TimeSpan.FromSeconds(60));
            }

        }
    }
}
