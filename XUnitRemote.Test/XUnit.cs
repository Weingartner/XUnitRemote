using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using XUnitRemote.Local;
using FactAttribute = Xunit.FactAttribute;
using TestMethodDisplay = Xunit.Sdk.TestMethodDisplay;

namespace XUnitRemote.Test
{

    /// <summary>
    /// This is the custom attribute you add to tests you wish to run under the control of SampleProcess.
    /// For example your unit test can look like
    /// <![CDATA[
    /// [SampleProcessFact]
    /// public void TestShouldWork(){
    ///     Assert.Equal("SampleProcess", GetProcess.GetCurrentProcess().ProcessName)
    /// }
    /// ]]>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("XUnitRemote.Test.SampleProcessFactDiscoverer", "XUnitRemote.Test")]
    public class SampleProcessFactAttribute : FactAttribute { }


    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("XUnitRemote.Test.SampleProcessTheoryDiscoverer", "XUnitRemote.Test")]
    public class SampleProcessTheoryAttribute : TheoryAttribute { }

    internal static class Common
    {
        public const string SampleProcessPath = @"..\..\..\XUnitRemote.Test.SampleProcess\bin\Debug\XUnitRemote.Test.SampleProcess.exe";
    }

    /// <summary>
    /// This is the xunit fact discoverer that xunit uses to replace the standard xunit runner
    /// with our runner. Anything that is tagged with the above attribute will use this discoverer. 
    /// </summary>
    public class SampleProcessFactDiscoverer : XUnitRemoteFactDiscoverer
    {

        public SampleProcessFactDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink, SampleProcess.Program.Id, Common.SampleProcessPath, SampleProcessTheoryDiscoverer.CollectionId )
        {
        }
    }

    public class SampleProcessTheoryDiscoverer : XUnitRemoteTheoryDiscoverer
    {
        /// <summary>
        /// Force SampleProcessTheoryDiscover to be sequential
        /// </summary>
        public static readonly Guid CollectionId = Guid.NewGuid();

        public SampleProcessTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink, SampleProcess.Program.Id, Common.SampleProcessPath, CollectionId  )
        {
        }
    }

    public class ScheduledTestCase : IXunitTestCase
    {
        private readonly IXunitTestCase _TestCase;

        public ScheduledTestCase(IXunitTestCase testCase)
        {
            _TestCase = testCase;
        }

        public Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            var tcs = new TaskCompletionSource<RunSummary>();
            SampleProcess.Program.Scheduler.ScheduleAsync(async (scheduler, ct) =>
            {
                try
                {
                    var runSummary = await _TestCase.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
                    tcs.SetResult(runSummary);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        public IMethodInfo Method => _TestCase.Method;

        public void Deserialize(IXunitSerializationInfo info) => _TestCase.Deserialize(info);

        public void Serialize(IXunitSerializationInfo info) => _TestCase.Serialize(info);

        public string DisplayName => _TestCase.DisplayName;

        public string SkipReason => _TestCase.SkipReason;

        public ISourceInformation SourceInformation
        {
            get { return _TestCase.SourceInformation; }
            set { _TestCase.SourceInformation = value; }
        }

        public ITestMethod TestMethod => _TestCase.TestMethod;

        public object[] TestMethodArguments => _TestCase.TestMethodArguments;

        public Dictionary<string, List<string>> Traits => _TestCase.Traits;

        public string UniqueID => _TestCase.UniqueID;
    }
}
