using System;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using Xunit.Runners;
using XUnitRemote.Remoting.Result;

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

            using (var runner = AssemblyRunner.WithoutAppDomain(assemblyPath))
            using (var finished = new ManualResetEvent(false))
            {
                runner.OnExecutionComplete = info => finished.Set();
                runner.TestCaseFilter = testCase => testCase.TestMethod.Method.Name == methodName;

                ITestResult testResult = null;
                runner.OnTestFailed = info => testResult = new TestFailed(info.ExecutionTime, info.Output, info.ExceptionType, info.ExceptionMessage, info.ExceptionStackTrace);
                runner.OnTestSkipped = info => testResult = new TestSkipped(info.SkipReason);
                runner.OnTestPassed = info => testResult = new TestPassed(info.ExecutionTime, info.Output);

                runner.Start(typeName);

                finished.WaitOne();
                if (testResult == null)
                {
                    throw new FaultException<TestExecutionFault>(new TestExecutionFault("Test testResult couldn't be captured."));
                }
                return testResult;
            }
        }
    }
}
