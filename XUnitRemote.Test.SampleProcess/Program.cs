using System.Collections.Generic;
using XUnitRemote.Local;

namespace XUnitRemote.Test.SampleProcess
{
    public static class Program
    {
        public const string Id = @"sampleprocessxx11";

        private static void Main()
        {
            var data = new Dictionary<string, object> { { "foo", 10 }, { "bar", "hello" } };
            XUnitService.Start(Id, isolateInDomain: false, marshaller: null, data: data).Wait();
        }
    }
}
