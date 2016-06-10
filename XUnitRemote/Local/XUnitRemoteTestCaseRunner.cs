using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Xunit.Abstractions;
using Xunit.Sdk;
using XUnitRemote.Local.Debugging;
using XUnitRemote.Remote;
using XUnitRemote.Remote.Result;
using XUnitRemote.Remote.Service.TestService;
using TestFailed = XUnitRemote.Remote.Result.TestFailed;
using TestPassed = XUnitRemote.Remote.Result.TestPassed;
using TestSkipped = XUnitRemote.Remote.Result.TestSkipped;

namespace XUnitRemote.Local
{
    public class XUnitRemoteTestCaseRunner : XunitTestCaseRunner
    {
        private readonly string _ExecutablePath;
        private readonly string _Id;

        public XUnitRemoteTestCaseRunner(IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments, object[] testMethodArguments,
            IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, string id, string exePath)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        {
            _Id = id;
            _ExecutablePath =  exePath;
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            try
            {
                using (var process = GetOrStartProcess(_ExecutablePath))
                using (CascadeDebugging(process.Id))
                {
                    var result = await Policy
                        .Handle<DebuggerException>()
                        .Or<EndpointNotFoundException>()
                        .WaitAndRetryAsync(Enumerable.Repeat(TimeSpan.FromMilliseconds(100), 500))
                        .ExecuteAndCaptureAsync(RunTest);
                    if (result.Outcome != OutcomeType.Successful)
                    {
                        throw result.FinalException;
                    }
                    return result.Result;
                }
            }
            catch (Exception e)
            {
                Aggregator.Add(e);
                MessageBus.QueueMessage(new ErrorMessage(new[] { TestCase }, e));
                throw;
            }
        }

        private static readonly ConcurrentDictionary<int, IObservable<Unit>> CascadeDebuggingObservable =
            new ConcurrentDictionary<int, IObservable<Unit>>();
        private static IDisposable CascadeDebugging(int processId)
        {
            return CascadeDebuggingObservable
                .GetOrAdd(processId, pId =>
                    Observable.Defer(() =>
                        Observable.Using(() => RemoteDebugger.CascadeDebugging(processId), _ => Observable.Never<Unit>())
                    )
                    .Publish()
                    .RefCount()
                )
                .Subscribe();
        }

        private async Task<RunSummary> RunTest()
        {
            var runSummary = new RunSummary();
            Action<ITestResult> onTestFinished = result =>
            {
                var test = new XunitTest(TestCase, result.DisplayName);
                MessageBus.QueueMessage(new TestStarting(test));
                var message = GetMessageFromTestResult(test, result);
                MessageBus.QueueMessage(message);
                MessageBus.QueueMessage(new TestFinished(test, result.ExecutionTime, result.Output));
                runSummary.Aggregate(GetRunSummary(result));
            };

            using (var channelFactory = CreateTestServiceChannelFactory(_Id, onTestFinished))
            {
                Func<ITestService, Task> action = async service =>
                {
                    try
                    {
                        await service.RunTest(
                            TestCase.Method.Type.Assembly.AssemblyPath,
                            TestCase.Method.Type.Name,
                            TestCase.Method.Name);
                    }
                    catch (FaultException<TestExecutionFault> fault)
                    {
                        throw new TestExecutionException(fault.Detail.Message, fault);
                    }
                };

                await Common.ExecuteWithChannel(channelFactory, action);
                return runSummary;
            }
        }

        private static ChannelFactory<ITestService> CreateTestServiceChannelFactory(string id, Action<ITestResult> onTestFinished)
        {
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { SendTimeout = TimeSpan.FromMinutes(60) };
            var address = new Uri(XUnitService.BaseUrl, id);
            return new DuplexChannelFactory<ITestService>(new TestServiceNotificationHandler(onTestFinished), binding, address.ToString());
        }

        private static readonly object StartProcessLock = new object();
        private static Process GetOrStartProcess(string processPath)
        {
            lock (StartProcessLock)
            {
                var runningProcess = Process
                    .GetProcessesByName(Path.GetFileNameWithoutExtension(processPath))
                    .FirstOrDefault();

                if (runningProcess != null)
                {
                    return runningProcess;
                }

                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = processPath,
                        CreateNoWindow = true
                    }
                };
                if (!process.Start())
                {
                    throw new TestExecutionException("Process couldn't be started.");
                }
                return process;
            }
        }

        private static RunSummary GetRunSummary(ITestResult result)
        {
            return new RunSummary
            {
                Failed = result is TestFailed ? 1 : 0,
                Skipped = result is TestSkipped ? 1 : 0,
                Total = 1,
                Time = result.ExecutionTime
            };
        }

        private static IMessageSinkMessage GetMessageFromTestResult(ITest test, ITestResult testResult)
        {
            var failed = testResult as TestFailed;
            if (failed != null)
            {
                return new Xunit.Sdk.TestFailed(test, failed.ExecutionTime, failed.Output, failed.ExceptionTypes, failed.ExceptionMessages, failed.ExceptionStackTraces, new [] { -1 });
            }
            var skipped = testResult as TestSkipped;
            if (skipped != null)
            {
                return new Xunit.Sdk.TestSkipped(test, skipped.SkipReason);
            }
            var passed = testResult as TestPassed;
            if (passed != null)
            {
                return new Xunit.Sdk.TestPassed(test, passed.ExecutionTime, passed.Output);
            }
            var resultTypeName = testResult?.GetType().FullName ?? "<unknown>";
            throw new TestExecutionException("Cannot get message for the following implementation of `ITestExecutionResult`: " + resultTypeName);
        }

        internal class TestServiceNotificationHandler : ITestResultNotificationService
        {
            private readonly Action<ITestResult> _OnTestFinished;

            public TestServiceNotificationHandler(Action<ITestResult> onTestFinished)
            {
                _OnTestFinished = onTestFinished;
            }

            public void TestFinished(ITestResult result)
            {
                _OnTestFinished(result);
            }
        }
    }
}