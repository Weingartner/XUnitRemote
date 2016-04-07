using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XUnitRemote.Remote.Result;
using TestFailed = XUnitRemote.Remote.Result.TestFailed;
using TestPassed = XUnitRemote.Remote.Result.TestPassed;
using TestSkipped = XUnitRemote.Remote.Result.TestSkipped;

namespace XUnitRemote.Remote.Service
{
    public class DefaultTestRunner : ITestService
    {
        private readonly Func<ITestResultNotificationService> _CreateNotificationService;

        public DefaultTestRunner(Func<ITestResultNotificationService> createNotificationService)
        {
            _CreateNotificationService = createNotificationService;
        }

        public async Task RunTest(string assemblyPath, string typeName, string methodName)
        {
            var testResultNotificationService = _CreateNotificationService();

            var visitor = new MessageSink(testResultNotificationService.TestFinished);
            var sourceProvider = new NullSourceInformationProvider();
            var xunit2 = new Xunit2
                (appDomainSupport: AppDomainSupport.Denied
                , sourceInformationProvider: sourceProvider
                , assemblyFileName: assemblyPath
                , configFileName: null
                , shadowCopy: false // Has no effect if appDomainSupport is 'denied'. 
                , shadowCopyFolder: null
                , diagnosticMessageSink: visitor
                , verifyTestAssemblyExists: true
                );

            var testAssemblyConfiguration = new TestAssemblyConfiguration
            {
                MaxParallelThreads = 1,
                ParallelizeAssembly = false,
                AppDomain = AppDomainSupport.Denied,
                ShadowCopy = false,
                ParallelizeTestCollections = false
            };

            var discoveryOptions = TestFrameworkOptions.ForDiscovery(testAssemblyConfiguration);
            var executionOptions = TestFrameworkOptions.ForExecution(testAssemblyConfiguration);

            var discoveryVisitor = new TestDiscoveryVisitor(testCase => FilterTestCase(testCase, assemblyPath, typeName, methodName));

            var discover = xunit2;

            discover.Find(true, discoveryVisitor, discoveryOptions);
            discoveryVisitor.Finished.WaitOne();

            xunit2.RunTests(discoveryVisitor.TestCases, visitor, executionOptions);


            await visitor.Finished;
        }

        private static bool FilterTestCase(ITestCaseDiscoveryMessage testCase, string assemblyPath, string typeName, string methodName)
        {
            return testCase.TestAssembly.Assembly.AssemblyPath == assemblyPath
                   && testCase.TestClass.Class.Name == typeName && testCase.TestMethod.Method.Name == methodName;
        }
    }

    public class TestDiscoveryVisitor : Xunit.Sdk.TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        private readonly Func<ITestCaseDiscoveryMessage, bool> _Filter;
        public List<ITestCase> TestCases { get; } = new List<ITestCase>();

        public TestDiscoveryVisitor(Func<ITestCaseDiscoveryMessage, bool> filter)
        {
            _Filter = filter;
        }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            if (_Filter(testCaseDiscovered))
                TestCases.Add(testCaseDiscovered.TestCase);
            return true;
        }
    }

    public class MessageSink : Xunit.Sdk.TestMessageVisitor
    {
        private readonly TaskCompletionSource<object> _FinishedTcs = new TaskCompletionSource<object>();
        private readonly Action<ITestResult> _Callback;
        public Task Finished => _FinishedTcs.Task;

        public MessageSink(Action<ITestResult> callback)
        {
            _Callback = callback;
        }

        protected override bool Visit(ITestFailed info)
        {
            _Callback(new TestFailed(info.Test.DisplayName, info.ExecutionTime, info.Output, info.ExceptionTypes, info.Messages, info.StackTraces));
            return true;
        }
        protected override bool Visit(ITestPassed info)
        {
            _Callback(new TestPassed(info.Test.DisplayName, info.ExecutionTime, info.Output));
            return true;
        }

        protected override bool Visit(ITestSkipped info)
        {
            _Callback(new TestSkipped(info.Test.DisplayName, info.Reason));
            return true;
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            _FinishedTcs.SetResult(null);
            return false;
        }
    }
}
