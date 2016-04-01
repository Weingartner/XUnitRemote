using System.Threading;
using Newtonsoft.Json;
using OutOfProcessTestExecution.Contracts;
using Xunit;
using Xunit.Runners;
using Xunit.Sdk;

namespace SampleProcess
{
    public class TestExecutionService : ITestExecutionService
    {
        public ITestExecutionResult RunTest(string assemblyPath, string typeName, string methodName)
        {
            using (var runner = AssemblyRunner.WithAppDomain(assemblyPath))
            using (var finished = new ManualResetEvent(false))
            {
                runner.OnExecutionComplete = info => finished.Set();
                runner.TestCaseFilter = testCase => testCase.TestMethod.Method.Name == methodName;

                ITestExecutionResult result = null;
                runner.OnTestFailed = info => result = new TestExecutionFailedResult(info.ExecutionTime, info.Output, info.ExceptionType, info.ExceptionMessage, info.ExceptionStackTrace);
                runner.OnTestSkipped = info => result = new TestExecutionSkippedResult(info.SkipReason);
                runner.OnTestPassed = info => result = new TestExecutionPassedResult(info.ExecutionTime, info.Output);

                runner.Start(typeName);

                finished.WaitOne();
                if (result == null)
                {
                    throw new TestExecutionException("Test result couldn't be captured.");
                }
                return result;
            }
        }
    }
}
