using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
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
                        int i = 0;
                        foreach (var result in testResult)
                        {
                            var test = new XunitTest(TestCase, result.DisplayName);
                            MessageBus.QueueMessage(new TestStarting(test));
                            var message = GetMessageFromTestResult(test, result);
                            MessageBus.QueueMessage(message);
                            MessageBus.QueueMessage(new TestFinished(test, result.ExecutionTime, result.Output));
                        }
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
                throw new TestExecutionException("GetProcess couldn't be started.");
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

        private static RunSummary GetRunSummary(ITestResult[] testResults)
        {
            int failed = testResults.OfType<TestFailed>().Count();
            int skipped = testResults.OfType<TestSkipped>().Count();
            int total = testResults.Length;
            var time = testResults.OfType<TestPassed>().Sum(r => r.ExecutionTime) +
                       testResults.OfType<TestFailed>().Sum(r => r.ExecutionTime);

            return new RunSummary() {Failed = failed, Skipped = skipped, Total = total, Time = time};
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
            var resultTypeName = testResult?.GetType().FullName ?? "<unknown>";
            throw new TestExecutionException("Cannot get message for the following implementation of `ITestExecutionResult`: " + resultTypeName);
        }
    }
}