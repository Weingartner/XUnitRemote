using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XUnitRemote.Local;
using XUnitRemote.Remote;

namespace XUnitRemote.Test
{
    public class Tests
    {
        private readonly ITestOutputHelper _Output;

        private static int Foo => (int)XUnitService.Data["foo"];
        private static string Bar => (string)XUnitService.Data["bar"]; 

        public Tests(ITestOutputHelper output)
        {
            _Output = output;
        }

        [SampleProcessFact]
        public void CanGetFoo()
        {
            Assert.Equal(10, Foo);
        }

        [SampleProcessTheory]
        [InlineData(1, 2, 3)]
        [InlineData(5, 6, 7)]
        [InlineData(8, 9, 10)]
        public async Task TestData(int a, int b, int c)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.Equal(1, b-a);
            Assert.Equal(1, c-b);
            //Assert.True(b >= 6);
        }

        [SampleProcessFact]
        public void CanGetBar()
        {
            Assert.Equal("hello", Bar);
        }

        [SampleProcessFact(Skip="test skip")]
        public void SkippedTest()
        {
        }

        private const string ProcessName = "XUnitRemote.Test.SampleProcess";

        [SampleProcessFact]
        public void OutOfProcess()
        {
            _Output.WriteLine("Process name: " + Process.GetCurrentProcess().ProcessName);
            Assert.Equal(ProcessName, Process.GetCurrentProcess().ProcessName);
        }

        [Fact]
        public void InProcess()
        {
            _Output.WriteLine("Process name: " + Process.GetCurrentProcess().ProcessName);
            Assert.NotEqual(ProcessName, Process.GetCurrentProcess().ProcessName);
        }
    }
}
