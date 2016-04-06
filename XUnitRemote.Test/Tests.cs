using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace XUnitRemote.Test
{
    public class Tests
    {
        private readonly ITestOutputHelper _Output;

        public int Foo => (int)XUnitService.Data["foo"]; 
        public string Bar => (string)XUnitService.Data["bar"]; 

        public Tests(ITestOutputHelper output)
        {
            _Output = output;
        }

        [SampleProcessFact]
        public void CanGetFoo()
        {
            Assert.Equal(Foo, 10);
        }

        [SampleProcessFact]
        public void CanGetBar()
        {
            Assert.Equal(Bar, "hello");
        }

        [SampleProcessFact]
        public void OutOfProcess()
        {
            _Output.WriteLine("Process name: " + Process.GetCurrentProcess().ProcessName);
            Assert.Equal(5, 5);
        }

        [Fact]
        public void InProcess()
        {
            _Output.WriteLine("Process name: " + Process.GetCurrentProcess().ProcessName);
            Assert.Equal(5, 3);
        }
    }
}
