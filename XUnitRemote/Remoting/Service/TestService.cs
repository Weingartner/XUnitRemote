using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;
using Xunit.Sdk;
using XUnitRemote.Remoting.Result;
using static System.String;
using NullMessageSink = Xunit.Sdk.NullMessageSink;
using TestFailed = XUnitRemote.Remoting.Result.TestFailed;
using TestMessageVisitor = Xunit.Sdk.TestMessageVisitor;
using TestPassed = XUnitRemote.Remoting.Result.TestPassed;

namespace XUnitRemote.Remoting.Service
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class TestService : ITestService
    {
        private readonly ITestResultNotificationService _NotificationService;

        public TestService()
        {
            _NotificationService = OperationContext.Current.GetCallbackChannel<ITestResultNotificationService>();
        }

        public void RunTest(string assemblyPath, string typeName, string methodName)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var assemblyInfo = new ReflectionAssemblyInfo(assembly);
            var visitor = new MessageSink(_NotificationService.TestFinished);
            var sourceProvider = new NullSourceInformationProvider();
            var xunit2 = new Xunit2(AppDomainSupport.Denied, sourceProvider, assemblyPath,null, false, null,visitor,true );

            var testAssemblyConfiguration = new TestAssemblyConfiguration {};
            testAssemblyConfiguration.MaxParallelThreads = 1;
            testAssemblyConfiguration.ParallelizeAssembly = false;
            testAssemblyConfiguration.ShadowCopy = false;
            testAssemblyConfiguration.AppDomain = AppDomainSupport.Denied;
            testAssemblyConfiguration.ParallelizeTestCollections = false;

            var discoveryOptions = TestFrameworkOptions.ForDiscovery(testAssemblyConfiguration);
            var executionOptions = TestFrameworkOptions.ForExecution(testAssemblyConfiguration);


            var discoveryVisitor = new TestDiscoveryVisitor(testCase => FilterTestCase(testCase, assemblyPath, typeName, methodName));

            var discover = new XunitTestFrameworkDiscoverer(assemblyInfo,sourceProvider,visitor);

            discover.Find(true, discoveryVisitor, discoveryOptions);
            discoveryVisitor.Finished.WaitOne();

            xunit2.RunTests(discoveryVisitor.TestCases, visitor, executionOptions);

            visitor.Finished.Wait();
        }

        private static bool FilterTestCase(ITestCaseDiscoveryMessage testCase, string assemblyPath, string typeName, string methodName)
        {
            return testCase.TestAssembly.Assembly.AssemblyPath==assemblyPath
                   &&testCase.TestClass.Class.Name == typeName && testCase.TestMethod.Method.Name == methodName;
        }
    }
    class TestDiscoveryVisitor : Xunit.Sdk.TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        private readonly Func<ITestCaseDiscoveryMessage, bool> _Filter;
        public List<ITestCase> TestCases { get; private set; }

        public TestDiscoveryVisitor(Func<ITestCaseDiscoveryMessage,bool> filter )
        {
            _Filter = filter;
            this.TestCases = new List<ITestCase>();
        }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            if(_Filter(testCaseDiscovered))
                this.TestCases.Add(testCaseDiscovered.TestCase);
            return true;
        }
    }

    public class MessageSink : TestMessageVisitor
    {
        private readonly Action<ITestResult> _Callback;
        private readonly TaskCompletionSource<Unit> _TCO;

        public MessageSink(Action<ITestResult> callback)
        {
            _Callback = callback;
            _TCO = new TaskCompletionSource<Unit>();
        }

        public Task<Unit> Finished => _TCO.Task;

        protected override bool Visit(ITestFailed info)
        {
            var r = new TestFailed(info.Test.DisplayName, info.ExecutionTime, info.Output, Join(", ", info.ExceptionTypes), Join(", ",info.Messages), Join("\n",info.StackTraces));
            _Callback(r);
            return true;
        }
        protected override bool Visit(ITestPassed info)
        {
            var r = new TestPassed(info.Test.DisplayName, info.ExecutionTime, info.Output);
            _Callback(r);
            return true;
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            _TCO.SetResult(Unit.Default);
            return true;
        }
    }


}
