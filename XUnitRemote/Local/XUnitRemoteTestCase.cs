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
        private string _Id;
        private string _ExePath;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public XUnitRemoteTestCase() { }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            Initialize(data.GetValue<string>("Id"), data.GetValue<string>("ExePath"));
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue("Id", _Id);
            data.AddValue("ExePath", _ExePath);
        }

        protected override string GetUniqueID()
            => $"{base.GetUniqueID()} [{_Id}, {_ExePath}]";

        public XUnitRemoteTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, string id, string exePath)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        {
            Initialize(id, exePath);
        }

        private void Initialize(string id, string exePath)
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