using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using XUnitRemote.Remote;

namespace XUnitRemote.Test.SampleProcess
{
    public static class Program
    {
        public const string Id = @"sampleprocessxx11";

        private static void Main()
        {
            Nito.AsyncEx.AsyncContext.Run(MainAsync);
        }

        public static IScheduler Scheduler { get; private set; }

        private static async Task MainAsync()
        {
            Scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
            SpinWait.SpinUntil(() => Debugger.IsAttached, TimeSpan.FromSeconds(5));
            var data = new Dictionary<string, object> {{"foo", 10}, {"bar", "hello"}};
            using (XUnitService.StartWithDefaultRunner(new TestServiceConfiguration(Id, data)))
            {
                Console.WriteLine("Press <Enter> to exit . . .");
                await Task.Run(() => Console.ReadLine());
            }
        }
    }
}
