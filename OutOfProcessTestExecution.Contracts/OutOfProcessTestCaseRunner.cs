using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Xunit.Abstractions;
using Xunit.Sdk;
using XUnitRemote.Remoting;
using XUnitRemote.Remoting.Result;
using XUnitRemote.Remoting.Service;
using TestPassed = XUnitRemote.Remoting.Result.TestPassed;
using TestSkipped = XUnitRemote.Remoting.Result.TestSkipped;

namespace XUnitRemote
{
    public class OutOfProcessTestCaseRunner : XunitTestCaseRunner
    {
        private string ExecutablePath;
        public string Id { get; }

        public OutOfProcessTestCaseRunner(IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments, object[] testMethodArguments,
            IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, string id, string exePath)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        {
            Id = id;
            ExecutablePath =  exePath;
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    Func<ITestService, RunSummary> action = service =>
                    {
                        try
                        {
                            var testResult = service.RunTest(TestCase.Method.Type.Assembly.AssemblyPath,
                                TestCase.Method.Type.Name, TestCase.Method.Name);
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
                    var result = Policy
                        .Handle<EndpointNotFoundException>()
                        .WaitAndRetry(Enumerable.Repeat(TimeSpan.FromMilliseconds(100), 50))
                        .ExecuteAndCapture(() => ExecuteWithTestService(ExecutablePath, action, Id));
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

        private static T ExecuteWithTestService<T>(string processPath, Func<ITestService, T> action, string id)
        {
            Func<T> wrapped = () =>
            {
                var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                var address = XUnitService.Address(id);
                using (var channel = new ChannelFactory<ITestService>(binding, address.ToString()))
                {
                    var testExecution = channel.CreateChannel();
                    return ExecuteWithChannel((IServiceChannel)testExecution, () => action(testExecution));
                }
            };
            return ExecuteWithProcess(processPath, wrapped);
        }

        private static T ExecuteWithChannel<T>(IServiceChannel channel, Func<T> action)
        {
            var success = false;
            try
            {
                var result = action();
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

        private static T ExecuteWithProcess<T>(string processPath, Func<T> wrapped)
        {
            var runningProcess = Process.GetProcessesByName("SampleProcess").FirstOrDefault();

            if (runningProcess != null)
            {
                RemoteDebugger.Launch(runningProcess.Id);
                return wrapped();
            }
            using (var process = new Process())
            {
                process.StartInfo.FileName = processPath;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                RemoteDebugger.Launch(process.Id);

                return wrapped();
            }
        }

        private static RunSummary GetRunSummary(ITestResult testResult)
        {
            if (testResult is TestSkipped)
            {
                return new RunSummary { Failed = 0, Skipped = 1, Total = 1, Time = 0m};
            }
            var failed = testResult as XUnitRemote.Remoting.Result.TestFailed;
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
            var failed = testResult as XUnitRemote.Remoting.Result.TestFailed;
            if (failed != null)
            {
                return new global::Xunit.Sdk.TestFailed(test, failed.ExecutionTime, failed.Output, failed.ExceptionTypes, failed.ExceptionMessages, failed.ExceptionStackTraces, new [] { -1 });
            }
            var passed = testResult as TestPassed;
            if (passed != null)
            {
                return new global::Xunit.Sdk.TestPassed(test, passed.ExecutionTime, passed.Output);
            }
            var skipped = testResult as TestSkipped;
            if (skipped != null)
            {
                return new global::Xunit.Sdk.TestSkipped(test, skipped.SkipReason);
            }
            var resultTypeName = testResult?.GetType().FullName ?? "<unknown>";
            throw new TestExecutionException("Cannot get message for the following implementation of `ITestExecutionResult`: " + resultTypeName);
        }
    }
}