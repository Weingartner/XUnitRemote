using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using XUnitRemote.Remote;

namespace XUnitRemote.Test.SampleProcess
{
    public static class Program
    {
        public const string Id = @"sampleprocessxx11";

        private static void Main()
        {
            SpinWait.SpinUntil(() => Debugger.IsAttached, TimeSpan.FromSeconds(5));
            var data = new Dictionary<string, object> { { "foo", 10 }, { "bar", "hello" } };
            using (XUnitService.StartWithDefaultRunner(new TestServiceConfiguration(Id, data)))
            {
                Console.WriteLine("Press <Enter> to exit . . .");
                Console.ReadLine();
            }
        }
    }
}
