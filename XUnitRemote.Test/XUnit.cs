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
    [XunitTestCaseDiscoverer("XUnitRemote.Test.ScheduledSampleProcessFactDiscoverer", "XUnitRemote.Test")]
    public class ScheduledSampleProcessFactAttribute : FactAttribute { }

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
            : base(diagnosticMessageSink, SampleProcess.Program.Id, Common.SampleProcessPath)
        {
        }
    }

    public class ScheduledSampleProcessFactDiscoverer : XUnitRemoteTestCaseDiscoverer
    {
        public ScheduledSampleProcessFactDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink, SampleProcess.Program.Id, Common.SampleProcessPath, new ScheduledFactDiscoverer(diagnosticMessageSink))
        {
        }
    }

    public class ScheduledFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _DiagnosticMessageSink;

        public ScheduledFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _DiagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            yield return new ScheduledTestCase(_DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
        }
    }

    public class ScheduledTestCase : XunitTestCase 
    {
        public ScheduledTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        {
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            var tcs = new TaskCompletionSource<RunSummary>();
            SampleProcess.Program.Scheduler.ScheduleAsync(async (scheduler, ct) =>
            {
                try
                {
                    // Set up the SynchronizationContext so that any awaits
                    // resume on the STA thread as they would in a GUI app.
                    //SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());

                    var runSummary = await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
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
    }

    public class SampleProcessTheoryDiscoverer : XUnitRemoteTheoryDiscoverer
    {
        public SampleProcessTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink, SampleProcess.Program.Id, Common.SampleProcessPath)
        {
        }
    }
}
