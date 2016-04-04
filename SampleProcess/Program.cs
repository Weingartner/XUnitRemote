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
using Xunit;
using Xunit.Runners;
using XUnitRemote;
using XUnitRemote.Remoting;
using XUnitRemote.Remoting.Service;

namespace SampleProcess
{
    public static class Program
    {
        public const string Id = @"sampleprocessxx11";

        private static void Main()
        {
            XUnitService.Start(Id).Wait();
        }
    }
}
