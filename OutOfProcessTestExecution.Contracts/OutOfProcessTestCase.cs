using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitRemote
{
    public class OutOfProcessTestCase : XunitTestCase
    {

        public OutOfProcessTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, string id, string exePath)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        {
            Id = id;
            ExePath = exePath;
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            return new OutOfProcessTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource, Id, ExePath).RunAsync();
        }

        public string Id { get; }
        public string ExePath { get; }
    }
}