using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Runners;
using XUnitRemote.Remote.Result;
using XUnitRemote.Remote.Service.TestResultNotificationService;

namespace XUnitRemote.Remote.Service.TestService
{
    [Serializable]
    public class IsolatedTestRunner
    {
        private readonly string _AssemblyPath;
        private readonly string _TypeName;
        private readonly string _MethodName;
        private readonly Action<ITestResult> _OnTestFinished;

        public IsolatedTestRunner(string assemblyPath, string typeName, string methodName, Action<ITestResult> onTestFinished)
        {
            _AssemblyPath = assemblyPath;
            _TypeName = typeName;
            _MethodName = methodName;
            _OnTestFinished = onTestFinished;
        }

        public async Task Run()
        {
            using (var runner = AssemblyRunner.WithoutAppDomain(_AssemblyPath))
            {
                var tcs = new TaskCompletionSource<object>();
                runner.OnExecutionComplete = info => tcs.SetResult(null);
                runner.TestCaseFilter = testCase => testCase.TestMethod.Method.Name == _MethodName;

                runner.OnTestPassed = info => _OnTestFinished(new TestPassed(info.TestDisplayName, info.ExecutionTime, info.Output));
                runner.OnTestFailed = info => _OnTestFinished(new TestFailed(info.TestDisplayName, info.ExecutionTime, info.Output, new[] { info.ExceptionType }, new[] { info.ExceptionMessage }, new[] { info.ExceptionStackTrace }));
                runner.OnTestSkipped = info => _OnTestFinished(new TestSkipped(info.TestDisplayName, info.SkipReason));

                runner.Start(_TypeName);

                await tcs.Task;
                // We can't dispose AssemblyRunner as long as it's not idle
                // The AssemblyRunner will be idle shortly after we get the `OnExecutionComplete` notification.
                // So we have to wait on another thread for the AssemblyRunner to be idle.
                await Task.Run(() => SpinWait.SpinUntil(() => runner.Status == AssemblyRunnerStatus.Idle));
            }
        }
    }
}