using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OutOfProcessTestExecution.Contracts;
using Polly;
using Xunit.Abstractions;
using Xunit.Runners;
using Xunit.Sdk;
using FactAttribute = Xunit.FactAttribute;

namespace OutOfProcessXunitTest.Xunit
{
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("OutOfProcessXunitTest.Xunit." + nameof(OutOfProcessFactDiscoverer), "OutOfProcessXunitTest")]
    public class OutOfProcessFactAttribute : FactAttribute { }

    public class OutOfProcessFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _DiagnosticMessageSink;
        private readonly FactDiscoverer _FactDiscoverer;

        public OutOfProcessFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _DiagnosticMessageSink = diagnosticMessageSink;
            _FactDiscoverer = new FactDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            if (Process.GetCurrentProcess().ProcessName != "SampleProcess")
            {
                yield return new OutOfProcessTestCase(_DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
            }
            else
            {
                foreach (var testCase in _FactDiscoverer.Discover(discoveryOptions, testMethod, factAttribute))
                {
                    yield return testCase;
                }
            }
        }
    }

    public class OutOfProcessTestCase : XunitTestCase
    {
        public OutOfProcessTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        { }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                  IMessageBus messageBus,
                                                  object[] constructorArguments,
                                                  ExceptionAggregator aggregator,
                                                  CancellationTokenSource cancellationTokenSource)
        {
            return new OutOfProcessTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource).RunAsync();
        }
    }

    public class OutOfProcessTestCaseRunner : XunitTestCaseRunner
    {
        public OutOfProcessTestCaseRunner(IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments, object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        { }

        protected override async Task<RunSummary> RunTestAsync()
        {
            const string testServiceExePath = @"C:\Users\egger\Workspace\OutOfProcessXunitTest\SampleProcess\bin\Debug\SampleProcess.exe";
            try
            {
                return await Task.Run(() =>
                {
                    Func<ITestExecutionService, RunSummary> action = service =>
                    {
                        var testResult = service.RunTest(TestCase.Method.Type.Assembly.AssemblyPath, TestCase.Method.Type.Name, TestCase.Method.Name);
                        var test = new XunitTest(TestCase, DisplayName);
                        var message = GetMessageFromTestResult(test, testResult);
                        MessageBus.QueueMessage(message);
                        return GetRunSummary(testResult);
                    };
                    var result = Policy
                        .Handle<EndpointNotFoundException>()
                        .WaitAndRetry(Enumerable.Repeat(TimeSpan.FromMilliseconds(100), 50))
                        .ExecuteAndCapture(() => ExecuteWithTestService(testServiceExePath, action));
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

        private static T ExecuteWithTestService<T>(string processPath, Func<ITestExecutionService, T> action)
        {
            Func<T> wrapped = () =>
            {
                var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                var address = new Uri("net.pipe://localhost/weingartner/TestExecutionService");
                using (var channel = new ChannelFactory<ITestExecutionService>(binding, address.ToString()))
                {
                    var testExecution = channel.CreateChannel();
                    ExecuteWithChannel()
                    using (UseChannel((IServiceChannel)testExecution))
                    {
                        return action(testExecution);
                    }
                }
            };
            return ExecuteWithProcess(processPath, wrapped);
        }

        private static IDisposable ExecuteWithChannel(IServiceChannel channel)
        {
            return Disposable.Create(() =>
            {

            });
            bool success = false;
            try
            {
                codeBlock((T)proxy);
                proxy.Close();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    proxy.Abort();
                }
            }
        }

        private static T ExecuteWithProcess<T>(string processPath, Func<T> wrapped)
        {
            var isRunning = Process.GetProcessesByName("SampleProcess").Any();

            if (isRunning)
            {
                return wrapped();
            }
            using (var process = new Process())
            {
                process.StartInfo.FileName = processPath;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                return wrapped();
            }
        }

        private static RunSummary GetRunSummary(ITestExecutionResult result)
        {
            if (result is TestExecutionSkippedResult)
            {
                return new RunSummary { Failed = 0, Skipped = 1, Total = 1, Time = 0m};
            }
            var failed = result as TestExecutionFailedResult;
            if (failed != null)
            {
                return new RunSummary { Failed = 1, Skipped = 0, Total = 1, Time = failed.ExecutionTime };
            }
            var passed = result as TestExecutionPassedResult;
            if (passed != null)
            {
                return new RunSummary { Failed = 0, Skipped = 0, Total = 1, Time = passed.ExecutionTime };
            }
            throw new TestExecutionException("Cannot get summary for the following implementation of `ITestExecutionResult`: " + result.GetType().FullName);
        }

        private static IMessageSinkMessage GetMessageFromTestResult(ITest test, ITestExecutionResult result)
        {
            var failed = result as TestExecutionFailedResult;
            if (failed != null)
            {
                return new TestFailed(test, failed.ExecutionTime, failed.Output, failed.ExceptionTypes, failed.ExceptionMessages, failed.ExceptionStackTraces, new [] { -1 });
            }
            var passed = result as TestExecutionPassedResult;
            if (passed != null)
            {
                return new TestPassed(test, passed.ExecutionTime, passed.Output);
            }
            var skipped = result as TestExecutionSkippedResult;
            if (skipped != null)
            {
                return new TestSkipped(test, skipped.SkipReason);
            }
            throw new TestExecutionException("Cannot get message for the following implementation of `ITestExecutionResult`: " + result.GetType().FullName);
        }
    }
}
