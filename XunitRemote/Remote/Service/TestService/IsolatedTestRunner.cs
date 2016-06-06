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
        private readonly Uri _ResultNotificationUrl;
        private readonly string _AssemblyPath;
        private readonly string _TypeName;
        private readonly string _MethodName;

        public IsolatedTestRunner(Uri resultNotificationUrl, string assemblyPath, string typeName, string methodName)
        {
            _ResultNotificationUrl = resultNotificationUrl;
            _AssemblyPath = assemblyPath;
            _TypeName = typeName;
            _MethodName = methodName;
        }

        public async Task Run()
        {
            using (var runner = AssemblyRunner.WithoutAppDomain(_AssemblyPath))
            using (var channelFactory = CreateNotificationServiceChannelFactory())
            {
                var tcs = new TaskCompletionSource<object>();
                runner.OnExecutionComplete = info => tcs.SetResult(null);
                runner.TestCaseFilter = testCase => testCase.TestMethod.Method.Name == _MethodName;

                runner.OnTestPassed = info => Notify(channelFactory, new TestPassed(info.TestDisplayName, info.ExecutionTime, info.Output));
                runner.OnTestFailed = info => Notify(channelFactory, new TestFailed(info.TestDisplayName, info.ExecutionTime, info.Output, new[] { info.ExceptionType }, new[] { info.ExceptionMessage }, new[] { info.ExceptionStackTrace }));
                runner.OnTestSkipped = info => Notify(channelFactory, new TestSkipped(info.TestDisplayName, info.SkipReason));

                runner.Start(_TypeName);

                await tcs.Task;
                // We can't dispose AssemblyRunner as long as it's not idle
                // The AssemblyRunner will be idle shortly after we get the `OnExecutionComplete` notification.
                // So we have to wait on another thread for the AssemblyRunner to be idle.
                await Task.Run(() => SpinWait.SpinUntil(() => runner.Status == AssemblyRunnerStatus.Idle));
            }
        }

        private static Task Notify(ChannelFactory<ITestResultNotificationService> channelFactory, ITestResult result)
        {
            return Common.ExecuteWithChannel(channelFactory, s =>
            {
                s.TestFinished(result);
                return Task.CompletedTask;
            });
        }

        private ChannelFactory<ITestResultNotificationService> CreateNotificationServiceChannelFactory()
        {
            return new ChannelFactory<ITestResultNotificationService>(Common.CreateBinding(), _ResultNotificationUrl.ToString());
        }
    }
}