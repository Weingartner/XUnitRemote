using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitRemote
{
    public class XUnitRemoteTestCase : XunitTestCase
    {
        private readonly string _Id;
        private readonly string _ExePath;

        public XUnitRemoteTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, string id, string exePath)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        {
            _Id = id;
            _ExePath = exePath;
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            return new OutOfProcessTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource, _Id, _ExePath).RunAsync();
        }
    }
}