using System.Collections.Generic;
using System.Diagnostics;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitRemote
{
    public abstract class OutOfProcessFactDiscovererBase : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _DiagnosticMessageSink;
        private readonly FactDiscoverer _FactDiscoverer;

        protected OutOfProcessFactDiscovererBase(IMessageSink diagnosticMessageSink)
        {
            _DiagnosticMessageSink = diagnosticMessageSink;
            _FactDiscoverer = new FactDiscoverer(diagnosticMessageSink);
        }

        protected abstract string Id { get; }
        protected abstract string ExePath { get; }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            if (Process.GetCurrentProcess().ProcessName != "SampleProcess")
            {
                var id = Id;
                var exePath = ExePath;

                yield return new OutOfProcessTestCase(_DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, id, exePath);
            }
            else
            {
                foreach (var testCase in _FactDiscoverer.Discover(discoveryOptions, testMethod, factAttribute))
                {
                    yield return testCase;
                }
            }
        }
    }
}