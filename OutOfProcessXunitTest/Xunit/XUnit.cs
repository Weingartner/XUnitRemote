using System;
using Xunit.Abstractions;
using Xunit.Sdk;
using FactAttribute = Xunit.FactAttribute;

namespace XUnitRemote.Test.Xunit
{
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("XUnitRemote.Test.Xunit." + nameof(SampleProcessFactDiscoverer), "XUnitRemote.Test")]
    public class SampleProcessFactAttribute : FactAttribute { }

    public class SampleProcessFactDiscoverer : OutOfProcessFactDiscovererBase
    {
        public override string Id { get; } = SampleProcess.Program.Id;
        public override string ExePath { get; } = @"..\..\..\SampleProcess\bin\Debug\SampleProcess.exe";

        public SampleProcessFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
        }
    }
}
