using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitRemote.Local
{
    public class XUnitRemoteTestCase : XunitTestCase
    {
        private readonly string _Id;
        private readonly string _ExePath;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public XUnitRemoteTestCase() { }

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
            return new XUnitRemoteTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource, _Id, _ExePath).RunAsync();
        }
    }
}