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

        [SampleProcessTheory]
        //[Theory]
        [InlineData(1, 2, 3)]
        [InlineData(5, 6, 7)]
        [InlineData(8, 9, 10)]
        public void TestData(int a, int b, int c)
        {
            Assert.Equal(b-a,1);
            Assert.True(b>6);
            Assert.Equal(c-b,1);
        }

        [SampleProcessFact]
        public void CanGetBar()
        {
            Assert.Equal(Bar, "hello");
        }

        [SampleProcessFact]
        public void OutOfProcess()
        {
            _Output.WriteLine("GetProcess name: " + Process.GetCurrentProcess().ProcessName);
            Assert.Equal(5, 5);
        }

        [Fact]
        public void InProcess()
        {
            _Output.WriteLine("GetProcess name: " + Process.GetCurrentProcess().ProcessName);
            Assert.Equal(5, 3);
        }
    }
}
