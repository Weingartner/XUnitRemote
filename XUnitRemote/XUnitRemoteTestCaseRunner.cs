using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Xunit.Abstractions;
using Xunit.Sdk;
using XUnitRemote.Remoting;
using XUnitRemote.Remoting.Result;
using XUnitRemote.Remoting.Service;
using TestFailed = XUnitRemote.Remoting.Result.TestFailed;
using TestPassed = XUnitRemote.Remoting.Result.TestPassed;
using TestSkipped = XUnitRemote.Remoting.Result.TestSkipped;

namespace XUnitRemote
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
                using (RemoteDebugger.CascadeDebugging(process.Id))
                    return await Task.Run(() =>
                    {
                        var result = Policy
                            .Handle<Exception>(e => e is DebuggerException || e is EndpointNotFoundException)
                            .WaitAndRetry(Enumerable.Repeat(TimeSpan.FromMilliseconds(100), 500))
                            .ExecuteAndCapture(() => RunTest(_ExecutablePath, _Id));
                        if (result.Outcome != OutcomeType.Successful)
                        {
                            throw result.FinalException;
                        }
                        return result.Result;
                    });
            }
            catch (Exception e)
            {
                Aggregator.Add(e);
                MessageBus.QueueMessage(new ErrorMessage(new[] { TestCase }, e));
                throw;
            }
        }

        private RunSummary RunTest(string processPath, string id)
        {
            using (var channelFactory = CreateTestServiceChannelFactory(id))
            {
                Func<ITestService, RunSummary> action = service =>
                {
                    try
                    {
                        var testResult = service.RunTest(
                            TestCase.Method.Type.Assembly.AssemblyPath,
                            TestCase.Method.Type.Name,
                            TestCase.Method.Name);
                        var test = new XunitTest(TestCase, DisplayName);
                        var message = GetMessageFromTestResult(test, testResult);
                        MessageBus.QueueMessage(message);
                        return GetRunSummary(testResult);
                    }
                    catch (FaultException<TestExecutionFault> fault)
                    {
                        throw new TestExecutionException(fault.Detail.Message, fault);
                    }
                };

                return ExecuteWithChannel(channelFactory, action);
            }
        }

        private static ChannelFactory<ITestService> CreateTestServiceChannelFactory(string id)
        {
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            var address = XUnitService.Address(id);
            var channelFactory = new ChannelFactory<ITestService>(binding, address.ToString());
            return channelFactory;
        }

        private static Process GetOrStartProcess(string processPath)
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

        private static TResult ExecuteWithChannel<TChannel, TResult>(ChannelFactory<TChannel> channelFactory, Func<TChannel, TResult> action)
        {
            var service = channelFactory.CreateChannel();
            var channel = (IServiceChannel) service;
            var success = false;
            try
            {
                var result = action(service);
                channel.Close();
                success = true;
                return result;
            }
            finally
            {
                if (!success)
                {
                    channel.Abort();
                }
            }
        }

        private static RunSummary GetRunSummary(ITestResult testResult)
        {
            if (testResult is TestSkipped)
            {
                return new RunSummary { Failed = 0, Skipped = 1, Total = 1, Time = 0m};
            }
            var failed = testResult as TestFailed;
            if (failed != null)
            {
                return new RunSummary { Failed = 1, Skipped = 0, Total = 1, Time = failed.ExecutionTime };
            }
            var passed = testResult as TestPassed;
            if (passed != null)
            {
                return new RunSummary { Failed = 0, Skipped = 0, Total = 1, Time = passed.ExecutionTime };
            }
            var resultTypeName = testResult?.GetType().FullName ?? "<unknown>";
            throw new TestExecutionException("Cannot get summary for the following implementation of `ITestExecutionResult`: " + resultTypeName);
        }

        private static IMessageSinkMessage GetMessageFromTestResult(ITest test, ITestResult testResult)
        {
            var failed = testResult as TestFailed;
            if (failed != null)
            {
                return new Xunit.Sdk.TestFailed(test, failed.ExecutionTime, failed.Output, failed.ExceptionTypes, failed.ExceptionMessages, failed.ExceptionStackTraces, new [] { -1 });
            }
            var passed = testResult as TestPassed;
            if (passed != null)
            {
                return new Xunit.Sdk.TestPassed(test, passed.ExecutionTime, passed.Output);
            }
            var skipped = testResult as TestSkipped;
            if (skipped != null)
            {
                return new Xunit.Sdk.TestSkipped(test, skipped.SkipReason);
            }
            var resultTypeName = testResult?.GetType().FullName ?? "<unknown>";
            throw new TestExecutionException("Cannot get message for the following implementation of `ITestExecutionResult`: " + resultTypeName);
        }
    }
}