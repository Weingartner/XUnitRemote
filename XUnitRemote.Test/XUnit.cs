using System;
using Xunit.Abstractions;
using Xunit.Sdk;
using FactAttribute = Xunit.FactAttribute;

namespace XUnitRemote.Test
{
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("XUnitRemote.Test." + nameof(SampleProcessFactDiscoverer), "XUnitRemote.Test")]
    public class SampleProcessFactAttribute : FactAttribute { }

    public class SampleProcessFactDiscoverer : OutOfProcessFactDiscovererBase
    {
        protected override string Id { get; } = SampleProcess.Program.Id;
        protected override string ExePath { get; } = @"..\..\..\SampleProcess\bin\Debug\SampleProcess.exe";

        public SampleProcessFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
        }
    }
}
