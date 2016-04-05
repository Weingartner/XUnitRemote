using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
using TestSkipped = XUnitRemote.Remoting.Result.TestSkipped;

namespace XUnitRemote.Remoting.Service
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class TestService : ITestService
    {
        public TestService()
        {
            Console.WriteLine("woo");
        }

        public string GetCurrentFolder()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public ITestResult RunTest(string assemblyPath, string typeName, string methodName)
        {

            var assembly = Assembly.LoadFrom(assemblyPath);
            var assemblyInfo = new ReflectionAssemblyInfo(assembly);
            var visitor = new MessageSink();
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

            return visitor.TestResult;


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
        private readonly TaskCompletionSource<Unit> _TCO;

        public MessageSink()
        {
            _TCO = new TaskCompletionSource<Unit>();
        }

        public ITestResult TestResult { get; private set; } = null;

        public Task<Unit> Finished => _TCO.Task ;

        protected override bool Visit(ITestFailed info)
        {
            TestResult = new TestFailed(info.ExecutionTime, info.Output, Join(", ", info.ExceptionTypes), Join(", ",info.Messages), Join("\n",info.StackTraces));
            return true;
        }
        protected override bool Visit(ITestSkipped info)
        {
            TestResult = new TestSkipped(info.Reason);
            return true;

        }
        protected override bool Visit(ITestPassed info)
        {
            TestResult = new TestPassed(info.ExecutionTime, info.Output);
            return true;
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            _TCO.SetResult(Unit.Default);
            return true;
        }
    }


}
