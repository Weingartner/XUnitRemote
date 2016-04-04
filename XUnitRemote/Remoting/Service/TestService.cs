using System.ServiceModel;
using System.Threading;
using Xunit.Runners;
using XUnitRemote.Remoting.Result;

namespace XUnitRemote.Remoting.Service
{
    public class TestService : ITestService
    {
        public ITestResult RunTest(string assemblyPath, string typeName, string methodName)
        {
            using (var runner = AssemblyRunner.WithAppDomain(assemblyPath))
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
